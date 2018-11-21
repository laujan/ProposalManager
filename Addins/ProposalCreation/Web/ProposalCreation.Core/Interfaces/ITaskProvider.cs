using ProposalCreation.Core.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProposalCreation.Core.Interfaces
{
    public interface ITaskProvider
    {
        Task<IEnumerable<string>> GetTasksAsync();
    }
}
