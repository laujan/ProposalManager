using ApplicationCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using ApplicationCore.ViewModels;

namespace ApplicationCore.Interfaces
{
    public interface ITemplateService
    {
        Task<StatusCodes> CreateItemAsync(TemplateViewModel modelObject, string requestId = "");
        Task<StatusCodes> UpdateItemAsync(TemplateViewModel modelObject, string requestId = "");
        Task<TemplateViewModel> GetItemByIdAsync(string id, string requestId = "");
        Task<StatusCodes> DeleteItemAsync(string id, string requestId = "");
        Task<TemplateListViewModel> GetAllAsync(string requestId = "");
        Task<bool> ProcessCheckAsync(IList<ProcessViewModel> processList, string requestId = "");
    }
}
