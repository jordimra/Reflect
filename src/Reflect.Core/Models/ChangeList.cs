using System.Collections.Generic;

namespace Reflect.Core.Models
{
    public class ChangeList
    {
        public IReadOnlyCollection<FileSystemNode> Added { get; set; } = new List<FileSystemNode>();
        public IReadOnlyCollection<FileSystemNode> Modified { get; set; } = new List<FileSystemNode>();
        public IReadOnlyCollection<FileSystemNode> Deleted { get; set; } = new List<FileSystemNode>();
    }
}
