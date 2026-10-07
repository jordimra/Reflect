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
    public class ProposalItem
    {
        public bool IsSelected { get; set; } = true;
        public string Path { get; set; }
        public string Status { get; set; }
        public string SourceBase { get; set; }
        public string SourcePath { get; set; }
        public string TargetPath { get; set; }
    }

    public partial class MainWindow : Window
    {
        private readonly StandardSnapshotEngine _snapshotEngine;
        private readonly BasicChangeAnalyzer _analyzer;
        private readonly ProcessTrackingRunner _runner;
        private readonly TransactionalRelocationEngine _relocator;

        private List<Snapshot> _preSnapshots = new List<Snapshot>();
        private List<Snapshot> _postSnapshots = new List<Snapshot>();
        
        private readonly string[] _monitoredDirs;

        public MainWindow()
        {
            InitializeComponent();

            _snapshotEngine = new StandardSnapshotEngine();
            _analyzer = new BasicChangeAnalyzer();
            _runner = new ProcessTrackingRunner();
            _relocator = new TransactionalRelocationEngine(new NtfsManager());

            _monitoredDirs = new[]
            {
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles),
                Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86),
                Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "LocalLow")
            }.Where(d => !string.IsNullOrWhiteSpace(d) && Directory.Exists(d))
             .Distinct(StringComparer.OrdinalIgnoreCase)
             .ToArray();
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
            var dlg = new OpenFolderDialog { Title = "Seleccione la carpeta destino" };
            if (dlg.ShowDialog() == true)
            {
                TxtTargetDir.Text = dlg.FolderName;
            }
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

                TxtStatus.Text = "Tomando snapshots previos (puede tardar un momento)...";
                _preSnapshots.Clear();
                foreach(var dir in _monitoredDirs)
                {
                    _preSnapshots.Add(await _snapshotEngine.TakeSnapshotAsync(dir));
                }

                TxtStatus.Text = "Ejecutando instalador y esperando...";
                await _runner.RunAndWaitAsync(TxtInstallerPath.Text);

                TxtStatus.Text = "Tomando snapshots posteriores...";
                _postSnapshots.Clear();
                foreach(var dir in _monitoredDirs)
                {
                    _postSnapshots.Add(await _snapshotEngine.TakeSnapshotAsync(dir));
                }

                TxtStatus.Text = "Analizando cambios (Diff)...";
                var allProposals = new List<ProposalItem>();

                for (int i = 0; i < _monitoredDirs.Length; i++)
                {
                    var changes = _analyzer.Compare(_preSnapshots[i], _postSnapshots[i]);
                    var topDirs = changes.Added
                        .Select(n => n.RelativePath.Split(Path.DirectorySeparatorChar).First())
                        .Distinct();

                    foreach (var d in topDirs)
                    {
                        allProposals.Add(new ProposalItem { 
                            Path = d, 
                            Status = "Nuevo", 
                            SourceBase = _monitoredDirs[i],
                            SourcePath = Path.Combine(_monitoredDirs[i], d),
                            TargetPath = Path.Combine(TxtTargetDir.Text ?? "", d)
                        });
                    }
                }

                ListProposals.ItemsSource = allProposals;

                if (allProposals.Any())
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

            var items = ListProposals.ItemsSource as IEnumerable<ProposalItem>;
            if (items == null) return;

            var selectedItems = items.Where(i => i.IsSelected).ToList();
            if (!selectedItems.Any()) return;

            BtnRelocate.IsEnabled = false;

            try
            {
                TxtStatus.Text = "Moviendo archivos y creando Junctions... ¡No apague el equipo!";

                foreach (var item in selectedItems)
                {
                    string topDir = item.Path;
                    string sourceBase = item.SourceBase;
                    string sourcePath = Path.Combine(sourceBase, topDir);
                    
                    // Reubica la carpeta recién creada
                    string targetPath = Path.Combine(TxtTargetDir.Text, topDir);

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