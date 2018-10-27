using ApplicationCore.Entities;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace ApplicationCore.Interfaces
{
    public interface IDashboardRepository
    {
        Task<StatusCodes> CreateOpportunityAsync(Dashboard entity, string requestId = "");
        Task<StatusCodes> UpdateOpportunityAsync(Dashboard entity, string requestId = "");
        Task<StatusCodes> DeleteOpportunityAsync(string id, string requestId = "");
        Task<IList<Dashboard>> GetAllAsync(string requestId = "");
        Task<Dashboard> GetAsync(string Id, string requestId = "");
    }
}
