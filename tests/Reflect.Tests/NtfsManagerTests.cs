using System;
using System.IO;
using Reflect.Ntfs;
using Xunit;

namespace Reflect.Tests
{
    public class NtfsManagerTests : IDisposable
    {
        private readonly string _tempBase;
        private readonly string _sourceJunction;
        private readonly string _targetDir;

        public NtfsManagerTests()
        {
            _tempBase = Path.Combine(Path.GetTempPath(), "Reflect_NtfsTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempBase);

            _sourceJunction = Path.Combine(_tempBase, "JunctionPoint");
            _targetDir = Path.Combine(_tempBase, "RealTarget");
            Directory.CreateDirectory(_targetDir);
        }

        [Fact]
        public void CreateJunction_ShouldCreateReparsePointCorrectly()
        {
            // Arrange
            var manager = new NtfsManager();
            File.WriteAllText(Path.Combine(_targetDir, "test.txt"), "some data");

            // Act
            manager.CreateJunction(_sourceJunction, _targetDir);

            // Assert
            Assert.True(Directory.Exists(_sourceJunction));
            Assert.True(manager.IsJunction(_sourceJunction));
            
            // Verificar lectura transparente a través del Junction
            var resolvedFile = Path.Combine(_sourceJunction, "test.txt");
            Assert.True(File.Exists(resolvedFile));
            Assert.Equal("some data", File.ReadAllText(resolvedFile));
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempBase))
            {
                try
                {
                    Directory.Delete(_tempBase, true);
                }
                catch { }
            }
        }
    }
}
