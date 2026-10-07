using System;
using System.Threading;
using System.Threading.Tasks;
using Reflect.Core.Models;

namespace Reflect.Core.Interfaces
{
    public interface ISnapshotEngine
    {
        Task<Snapshot> TakeSnapshotAsync(string directoryPath, CancellationToken cancellationToken = default);
    }
}
