using System.Threading;
using System.Threading.Tasks;

namespace Reflect.Core.Interfaces
{
    public interface IInstallationRunner
    {
        Task<int> RunAndWaitAsync(string installerPath, string arguments = "", CancellationToken cancellationToken = default);
    }
}
