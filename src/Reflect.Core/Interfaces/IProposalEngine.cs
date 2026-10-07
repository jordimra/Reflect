using Reflect.Core.Models;

namespace Reflect.Core.Interfaces
{
    public interface IProposalEngine
    {
        ApplicationProposal GenerateProposal(ChangeList changes);
    }
}
