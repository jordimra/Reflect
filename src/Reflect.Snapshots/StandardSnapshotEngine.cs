using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Reflect.Core.Interfaces;
using Reflect.Core.Models;

namespace Reflect.Snapshots
{
    public class StandardSnapshotEngine : ISnapshotEngine
    {
        public Task<Snapshot> TakeSnapshotAsync(string directoryPath, CancellationToken cancellationToken = default)
        {
            return Task.Run(() => TakeSnapshot(directoryPath, cancellationToken), cancellationToken);
        }

        private Snapshot TakeSnapshot(string directoryPath, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(directoryPath))
                throw new ArgumentException("El directorio no puede estar vacío.", nameof(directoryPath));

            if (!Directory.Exists(directoryPath))
                throw new DirectoryNotFoundException($"No se encontró el directorio: {directoryPath}");

            var snapshot = new Snapshot
            {
                CapturedAt = DateTime.UtcNow,
                RootDirectory = directoryPath
            };

            var nodes = new List<FileSystemNode>();

            // Utilizamos EnumerationOptions con IgnoreInaccessible para no chocar con carpetas
            // de permisos restringidos (System, ocultos profundos)
            var options = new EnumerationOptions
            {
                IgnoreInaccessible = true,
                RecurseSubdirectories = true,
                ReturnSpecialDirectories = false
            };

            var rootUri = new Uri(directoryPath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar);

            foreach (var entry in new DirectoryInfo(directoryPath).EnumerateFileSystemInfos("*", options))
            {
                cancellationToken.ThrowIfCancellationRequested();

                var entryUri = new Uri(entry.FullName);
                string relativePath = Uri.UnescapeDataString(rootUri.MakeRelativeUri(entryUri).ToString()).Replace('/', Path.DirectorySeparatorChar);

                bool isDir = (entry.Attributes & FileAttributes.Directory) == FileAttributes.Directory;
                long size = 0;

                if (!isDir && entry is FileInfo fileInfo)
                {
                    size = fileInfo.Length;
                }

                nodes.Add(new FileSystemNode
                {
                    RelativePath = relativePath,
                    IsDirectory = isDir,
                    LastWriteTime = entry.LastWriteTimeUtc,
                    Size = size
                });
            }

            snapshot.Nodes = nodes;
            return snapshot;
        }
    }
}
