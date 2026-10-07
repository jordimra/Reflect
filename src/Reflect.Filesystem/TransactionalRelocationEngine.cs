using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Reflect.Core.Interfaces;

namespace Reflect.Filesystem
{
    public class TransactionalRelocationEngine : IRelocationEngine
    {
        private readonly Stack<Func<Task>> _rollbackStack = new Stack<Func<Task>>();
        private readonly INtfsManager _ntfsManager;

        public TransactionalRelocationEngine(INtfsManager ntfsManager)
        {
            _ntfsManager = ntfsManager ?? throw new ArgumentNullException(nameof(ntfsManager));
        }

        public Task RelocateAsync(string sourceDir, string targetDir, CancellationToken cancellationToken = default)
        {
            return Task.Run(() => 
            {
                if (string.IsNullOrWhiteSpace(sourceDir)) throw new ArgumentNullException(nameof(sourceDir));
                if (string.IsNullOrWhiteSpace(targetDir)) throw new ArgumentNullException(nameof(targetDir));

                string backupDir = sourceDir.TrimEnd(Path.DirectorySeparatorChar) + "_Backup_Reflect";

                try
                {
                    // 1. Validar origen
                    if (!Directory.Exists(sourceDir))
                        throw new DirectoryNotFoundException($"El origen {sourceDir} no existe.");

                    // 2. Renombrar origen a _backup (falla si está bloqueado)
                    Directory.Move(sourceDir, backupDir);
                    
                    // Añadir acción de rollback (deshacer renombrado)
                    _rollbackStack.Push(() => 
                    {
                        if (Directory.Exists(sourceDir)) ForceDeleteDirectory(sourceDir);
                        Directory.Move(backupDir, sourceDir);
                        return Task.CompletedTask;
                    });

                    // 3. Crear destino si no existe
                    if (!Directory.Exists(targetDir))
                    {
                        Directory.CreateDirectory(targetDir);
                        _rollbackStack.Push(() =>
                        {
                            ForceDeleteDirectory(targetDir);
                            return Task.CompletedTask;
                        });
                    }

                    // 4. Copiar archivos reales de _backup a target (lanzaría error de acceso aquí)
                    CopyDirectory(backupDir, targetDir, cancellationToken);
                    
                    // 5. Crear el Junction en el origen
                    _ntfsManager.CreateJunction(sourceDir, targetDir);

                    // Operación exitosa, limpiamos el backup. Si falla, el rollback se encarga.
                    ForceDeleteDirectory(backupDir);
                    _rollbackStack.Clear(); // Éxito total
                }
                catch (Exception)
                {
                    // Forzamos el rollback síncrono si hay una excepción
                    RollbackAsync().GetAwaiter().GetResult();
                    throw; // Re-lanzar la excepción original para que la UI se entere
                }

            }, cancellationToken);
        }

        public async Task RollbackAsync()
        {
            while (_rollbackStack.Count > 0)
            {
                var action = _rollbackStack.Pop();
                try
                {
                    await action();
                }
                catch
                {
                    // El Rollback debe ser el mejor esfuerzo (best effort)
                    // No podemos hacer crash dentro de un rollback
                }
            }
        }

        private void CopyDirectory(string sourceDir, string destDir, CancellationToken cancellationToken)
        {
            // Crea todo el árbol de directorios
            foreach (string dirPath in Directory.GetDirectories(sourceDir, "*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                Directory.CreateDirectory(dirPath.Replace(sourceDir, destDir));
            }

            // Copia todos los archivos y sobreescribe
            foreach (string newPath in Directory.GetFiles(sourceDir, "*.*", SearchOption.AllDirectories))
            {
                cancellationToken.ThrowIfCancellationRequested();
                File.Copy(newPath, newPath.Replace(sourceDir, destDir), true);
            }
        }

        private void ForceDeleteDirectory(string targetDir)
        {
            if (!Directory.Exists(targetDir)) return;
            
            // Elimina el atributo de solo lectura para todos los archivos dentro
            foreach (var file in Directory.GetFiles(targetDir, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }
            Directory.Delete(targetDir, true);
        }
    }
}
