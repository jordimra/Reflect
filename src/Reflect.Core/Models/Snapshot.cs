using System;
using System.Collections.Generic;

namespace Reflect.Core.Models
{
    public class Snapshot
    {
        public DateTime CapturedAt { get; set; }
        public string RootDirectory { get; set; } = string.Empty;
        public IReadOnlyCollection<FileSystemNode> Nodes { get; set; } = Array.Empty<FileSystemNode>();
    }
}
