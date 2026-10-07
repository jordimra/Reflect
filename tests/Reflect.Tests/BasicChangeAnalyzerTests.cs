using System;
using System.Collections.Generic;
using System.Linq;
using Reflect.Analysis;
using Reflect.Core.Models;
using Xunit;

namespace Reflect.Tests
{
    public class BasicChangeAnalyzerTests
    {
        [Fact]
        public void Compare_ShouldDetectNewFolders_WhenAddedInPostSnapshot()
        {
            // Arrange
            var pre = new Snapshot { RootDirectory = "C:\\Target", Nodes = new List<FileSystemNode>
            {
                new FileSystemNode { RelativePath = "App1", IsDirectory = true }
            }};

            var post = new Snapshot { RootDirectory = "C:\\Target", Nodes = new List<FileSystemNode>
            {
                new FileSystemNode { RelativePath = "App1", IsDirectory = true },
                new FileSystemNode { RelativePath = "App2", IsDirectory = true }
            }};

            var analyzer = new BasicChangeAnalyzer();

            // Act
            var changes = analyzer.Compare(pre, post);

            // Assert
            Assert.Single(changes.Added);
            Assert.Equal("App2", changes.Added.First().RelativePath);
            Assert.Empty(changes.Deleted);
        }

        [Fact]
        public void Compare_ShouldBeCaseInsensitive_WhenPathsOnlyDifferInCase()
        {
            // Arrange
            var pre = new Snapshot { RootDirectory = "C:\\Target", Nodes = new List<FileSystemNode>
            {
                new FileSystemNode { RelativePath = "mozilla firefox", IsDirectory = true }
            }};

            var post = new Snapshot { RootDirectory = "C:\\Target", Nodes = new List<FileSystemNode>
            {
                new FileSystemNode { RelativePath = "Mozilla Firefox", IsDirectory = true }
            }};

            var analyzer = new BasicChangeAnalyzer();

            // Act
            var changes = analyzer.Compare(pre, post);

            // Assert
            // Dado que en Windows las rutas no distinguen mayúsculas/minúsculas,
            // no debería detectarse como añadido ni eliminado.
            Assert.Empty(changes.Added);
            Assert.Empty(changes.Deleted);
        }
    }
}
