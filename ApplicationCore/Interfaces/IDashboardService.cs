
using ApplicationCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using ApplicationCore.Models;
using ApplicationCore.ViewModels;

namespace ApplicationCore.Interfaces
{
    public interface IDashboardService
    {
        Task<StatusCodes> CreateOpportunityAsync(DashboardModel entity, string requestId = "");
        Task<StatusCodes> UpdateOpportunityAsync(DashboardModel entity, string requestId = "");
        Task<StatusCodes> DeleteOpportunityAsync(string id, string requestId = "");
        Task<IList<DashboardModel>> GetAllAsync(string requestId = "");
        Task<DashboardModel> GetAsync(string Id,string requestId = "");
    }
}
