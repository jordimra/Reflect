using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Reflect.Snapshots;
using Xunit;

namespace Reflect.Tests
{
    public class StandardSnapshotEngineTests : IDisposable
    {
        private readonly string _tempPath;

        public StandardSnapshotEngineTests()
        {
            _tempPath = Path.Combine(Path.GetTempPath(), "Reflect_SnapshotTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempPath);
        }

        [Fact]
        public async Task TakeSnapshotAsync_ShouldCaptureFilesAndFoldersCorrectly()
        {
            // Arrange
            Directory.CreateDirectory(Path.Combine(_tempPath, "Folder1"));
            File.WriteAllText(Path.Combine(_tempPath, "file1.txt"), "Hello World");
            File.WriteAllText(Path.Combine(_tempPath, "Folder1", "file2.txt"), "Nested");

            var engine = new StandardSnapshotEngine();

            // Act
            var snapshot = await engine.TakeSnapshotAsync(_tempPath);

            // Assert
            Assert.Equal(_tempPath, snapshot.RootDirectory);
            Assert.Equal(3, snapshot.Nodes.Count); // Folder1, file1.txt, Folder1/file2.txt
            
            var file1Node = snapshot.Nodes.FirstOrDefault(n => n.RelativePath == "file1.txt");
            Assert.NotNull(file1Node);
            Assert.False(file1Node.IsDirectory);
            Assert.Equal(11, file1Node.Size);

            var folderNode = snapshot.Nodes.FirstOrDefault(n => n.RelativePath == "Folder1");
            Assert.NotNull(folderNode);
            Assert.True(folderNode.IsDirectory);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempPath))
            {
                try { Directory.Delete(_tempPath, true); } catch { }
            }
        }
    }
}
