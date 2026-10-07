using System.Collections.Generic;

namespace Reflect.Core.Models
{
    public class ApplicationProposal
    {
        public string DetectedApplicationName { get; set; } = string.Empty;
        public List<string> RecommendedFoldersToMove { get; set; } = new List<string>();
        public List<string> IgnoredFolders { get; set; } = new List<string>();
    }
}
