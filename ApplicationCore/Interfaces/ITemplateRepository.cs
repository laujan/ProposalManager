using System.Collections.Generic;
using System.Threading.Tasks;
using ApplicationCore.Entities;

namespace ApplicationCore.Interfaces
{
    public interface ITemplateRepository
    {
        Task<StatusCodes> CreateItemAsync(Template modelObject, string requestId = "");
        Task<StatusCodes> UpdateItemAsync(Template modelObject, string requestId = "");
        Task<Template> GetItemByIdAsync(string id, string requestId = "");
        Task<StatusCodes> DeleteItemAsync(string id, string requestId = "");
        Task<IList<Template>> GetAllAsync(string requestId = "");
    }
}
