using Reflect.Core.Models;

namespace Reflect.Core.Interfaces
{
    public interface IChangeAnalyzer
    {
        ChangeList Compare(Snapshot preInstall, Snapshot postInstall);
    }
}
