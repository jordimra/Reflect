using System;
using System.Collections.Generic;
using Reflect.Core.Interfaces;
using Reflect.Core.Models;

namespace Reflect.Analysis
{
    public class BasicChangeAnalyzer : IChangeAnalyzer
    {
        public ChangeList Compare(Snapshot preInstall, Snapshot postInstall)
        {
            if (preInstall == null) throw new ArgumentNullException(nameof(preInstall));
            if (postInstall == null) throw new ArgumentNullException(nameof(postInstall));

            var changeList = new ChangeList
            {
                Added = new List<FileSystemNode>(),
                Modified = new List<FileSystemNode>(),
                Deleted = new List<FileSystemNode>()
            };

            var preDict = new Dictionary<string, FileSystemNode>(StringComparer.OrdinalIgnoreCase);
            
            foreach (var node in preInstall.Nodes)
            {
                // En Windows, las rutas son case-insensitive.
                preDict[node.RelativePath] = node;
            }

            foreach (var postNode in postInstall.Nodes)
            {
                if (preDict.TryGetValue(postNode.RelativePath, out var preNode))
                {
                    // Si existe, lo sacamos del diccionario para que al final sólo queden los borrados
                    preDict.Remove(postNode.RelativePath);

                    // Si es un directorio, ignoramos los cambios de fecha (a menudo Windows actualiza
                    // LastWriteTime de un directorio si se añaden cosas dentro). Nos interesa el contenido real.
                    if (postNode.IsDirectory)
                        continue;

                    // Para archivos, si el tamaño o el momento de escritura han cambiado, es una modificación.
                    if (preNode.Size != postNode.Size || preNode.LastWriteTime != postNode.LastWriteTime)
                    {
                        ((List<FileSystemNode>)changeList.Modified).Add(postNode);
                    }
                }
                else
                {
                    // No estaba en preInstall, así que ha sido añadido
                    ((List<FileSystemNode>)changeList.Added).Add(postNode);
                }
            }

            // Todo lo que sobró en el diccionario de preInstall significa que no apareció en postInstall
            foreach (var deletedNode in preDict.Values)
            {
                ((List<FileSystemNode>)changeList.Deleted).Add(deletedNode);
            }

            return changeList;
        }
    }
}
