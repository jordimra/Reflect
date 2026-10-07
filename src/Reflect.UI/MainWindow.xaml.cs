using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using Reflect.Analysis;
using Reflect.Core.Models;
using Reflect.Filesystem;
using Reflect.Installation;
using Reflect.Ntfs;
using Reflect.Snapshots;

namespace Reflect.UI
{
    public partial class MainWindow : Window
    {
        private readonly StandardSnapshotEngine _snapshotEngine;
        private readonly BasicChangeAnalyzer _analyzer;
        private readonly ProcessTrackingRunner _runner;
        private readonly TransactionalRelocationEngine _relocator;

        private Snapshot _preSnapshot;
        private Snapshot _postSnapshot;
        private ChangeList _changes;
        
        // Para la prueba principal vigilaremos Archivos de Programa
        private readonly string _monitoredDir;

        public MainWindow()
        {
            InitializeComponent();

            _snapshotEngine = new StandardSnapshotEngine();
            _analyzer = new BasicChangeAnalyzer();
            _runner = new ProcessTrackingRunner();
            _relocator = new TransactionalRelocationEngine(new NtfsManager());

            _monitoredDir = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
        }

        private void BtnSelectInstaller_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog { Filter = "Instaladores (*.exe;*.msi)|*.exe;*.msi|Todos los archivos (*.*)|*.*" };
            if (dlg.ShowDialog() == true)
            {
                TxtInstallerPath.Text = dlg.FileName;
            }
        }

        private void BtnSelectTarget_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Pega o escribe la ruta destino directamente en el recuadro para esta versión inicial.\nEjemplo: D:\\Applications", "Aviso");
        }

        private async void BtnRunInstaller_Click(object sender, RoutedEventArgs e)
        {
            if (!File.Exists(TxtInstallerPath.Text))
            {
                MessageBox.Show("Selecciona un archivo instalador válido.", "Error");
                return;
            }

            try
            {
                BtnRunInstaller.IsEnabled = false;

                TxtStatus.Text = $"Tomando snapshot de {_monitoredDir} (puede tardar un momento)...";
                _preSnapshot = await _snapshotEngine.TakeSnapshotAsync(_monitoredDir);

                TxtStatus.Text = "Ejecutando instalador y esperando...";
                await _runner.RunAndWaitAsync(TxtInstallerPath.Text);

                TxtStatus.Text = $"Tomando snapshot posterior de {_monitoredDir}...";
                _postSnapshot = await _snapshotEngine.TakeSnapshotAsync(_monitoredDir);

                TxtStatus.Text = "Analizando cambios (Diff)...";
                // Este paso ocurrirá en el hilo principal pero al ser O(N) será instantáneo
                _changes = _analyzer.Compare(_preSnapshot, _postSnapshot);

                // Agrupamos simplemente por la carpeta de primer nivel (ej: "Mozilla Firefox")
                var topLevelDirs = _changes.Added
                    .Select(n => n.RelativePath.Split(Path.DirectorySeparatorChar).First())
                    .Distinct()
                    .ToList();

                ListProposals.ItemsSource = topLevelDirs.Select(d => new { Path = d, Status = "Nuevo" });

                if (topLevelDirs.Any())
                {
                    BtnRelocate.IsEnabled = true;
                    TxtStatus.Text = "Análisis completado. Por favor, proceda a reubicar.";
                }
                else
                {
                    TxtStatus.Text = "No se detectaron carpetas nuevas.";
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocurrió un error en el flujo de la instalación: {ex.Message}");
                TxtStatus.Text = "Error en el proceso.";
            }
            finally
            {
                BtnRunInstaller.IsEnabled = true;
            }
        }

        private async void BtnRelocate_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtTargetDir.Text))
            {
                MessageBox.Show("Especifica la ruta destino (ej. D:\\Applications).", "Aviso");
                return;
            }

            // Usamos un objeto anónimo, por lo que usamos dynamic
            var items = ListProposals.ItemsSource as IEnumerable<dynamic>;
            if (items == null || !items.Any()) return;

            BtnRelocate.IsEnabled = false;

            try
            {
                TxtStatus.Text = "Moviendo archivos y creando Junctions... ¡No apague el equipo!";

                foreach (var item in items)
                {
                    string topDir = item.Path;
                    string sourcePath = Path.Combine(_monitoredDir, topDir);
                    string targetPath = Path.Combine(TxtTargetDir.Text, topDir);

                    // Reubica la carpeta recién creada
                    await _relocator.RelocateAsync(sourcePath, targetPath);
                }

                TxtStatus.Text = "¡Operación completada con éxito!";
                MessageBox.Show("Las carpetas seleccionadas fueron reubicadas de forma física y reemplazadas por un Junction lógico de manera exitosa.", "Éxito");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ocurrió un error crítico durante el movimiento. El sistema intentó recuperar el estado previo.\nError: {ex.Message}");
                TxtStatus.Text = "Error durante la reubicación. Reversión ejecutada.";
            }
        }
    }
}