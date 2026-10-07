using System;
using System.IO;
using System.Threading.Tasks;
using Reflect.Filesystem;
using Reflect.Ntfs;
using Xunit;

namespace Reflect.Tests
{
    public class TransactionalRelocationEngineTests : IDisposable
    {
        private readonly string _tempBase;
        private readonly string _sourceDir;
        private readonly string _targetDir;
        private readonly NtfsManager _ntfsManager;

        public TransactionalRelocationEngineTests()
        {
            _tempBase = Path.Combine(Path.GetTempPath(), "Reflect_RelocTest_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(_tempBase);

            _sourceDir = Path.Combine(_tempBase, "SourceApp");
            _targetDir = Path.Combine(_tempBase, "TargetDrive");

            Directory.CreateDirectory(_sourceDir);
            File.WriteAllText(Path.Combine(_sourceDir, "app.dll"), "binary");
            
            _ntfsManager = new NtfsManager();
        }

        [Fact]
        public async Task RelocateAsync_ShouldMoveFilesAndLeaveJunction_OnSuccess()
        {
            // Arrange
            var engine = new TransactionalRelocationEngine(_ntfsManager);

            // Act
            await engine.RelocateAsync(_sourceDir, _targetDir);

            // Assert
            Assert.True(Directory.Exists(_sourceDir));
            Assert.True(_ntfsManager.IsJunction(_sourceDir));
            Assert.True(File.Exists(Path.Combine(_sourceDir, "app.dll"))); // Leído transparente del junction
            Assert.True(File.Exists(Path.Combine(_targetDir, "app.dll"))); // Físicamente en destino
        }

        [Fact]
        public async Task RelocateAsync_ShouldRollbackAndRestoreSource_OnFailure()
        {
            // Arrange
            var engine = new TransactionalRelocationEngine(_ntfsManager);
            
            // Act & Assert
            await Assert.ThrowsAnyAsync<Exception>(async () => 
            {
                // Forzar un fallo pasando un destino no mapeable o inválido
                await engine.RelocateAsync(_sourceDir, "?::invalid_path");
            });

            // Verificar Rollback: El origen debe seguir estando allí como directorio normal y con sus archivos intactos
            Assert.True(Directory.Exists(_sourceDir));
            Assert.False(_ntfsManager.IsJunction(_sourceDir));
            Assert.True(File.Exists(Path.Combine(_sourceDir, "app.dll")));
            
            // Validar que no se generó basura en la carpeta backup temporal
            string backupDir = _sourceDir.TrimEnd(Path.DirectorySeparatorChar) + "_Backup_Reflect";
            Assert.False(Directory.Exists(backupDir));
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempBase))
            {
                try { Directory.Delete(_tempBase, true); } catch { }
            }
        }
    }
}
