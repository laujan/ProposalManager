using System.Collections.Generic;
using System.Threading.Tasks;
using ApplicationCore.Entities;

namespace ApplicationCore.Interfaces
{
    public interface IProcessRepository
    {
        Task<IList<ProcessesType>> GetAllAsync(string requestId = "");
        Task<StatusCodes> CreateItemAsync(ProcessesType modelObject, string requestId = "");
        Task<StatusCodes> UpdateItemAsync(ProcessesType modelObject, string requestId = "");
        Task<StatusCodes> DeleteItemAsync(string id, string requestId = "");
    }
}