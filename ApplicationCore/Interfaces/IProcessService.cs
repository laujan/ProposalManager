using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApplicationCore.ViewModels;

namespace ApplicationCore.Interfaces
{
    public interface IProcessService
    {
        Task<ProcessTypeListViewModel> GetAllAsync(string requestId = "");
        Task<StatusCodes> CreateItemAsync(ProcessTypeViewModel modelObject, string requestId = "");
        Task<StatusCodes> UpdateItemAsync(ProcessTypeViewModel modelObject, string requestId = "");
        Task<StatusCodes> DeleteItemAsync(string id, string requestId = "");
    }
}
