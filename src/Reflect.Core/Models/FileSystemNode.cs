using System;

namespace Reflect.Core.Models
{
    public class FileSystemNode
    {
        public string RelativePath { get; set; } = string.Empty;
        public bool IsDirectory { get; set; }
        public long Size { get; set; }
        public DateTime LastWriteTime { get; set; }
    }
}
