using System.Threading;
using System.Threading.Tasks;

namespace Reflect.Core.Interfaces
{
    public interface IRelocationEngine
    {
        Task RelocateAsync(string sourceDir, string targetDir, CancellationToken cancellationToken = default);
        Task RollbackAsync();
    }
}
