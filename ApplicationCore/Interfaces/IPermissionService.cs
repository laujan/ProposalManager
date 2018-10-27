using ApplicationCore.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationCore.Interfaces
{
    public interface IPermissionService
    {
        Task<StatusCodes> CreateItemAsync(PermissionModel modelObject, string requestId = "");

        Task<StatusCodes> DeleteItemAsync(string id, string requestId = "");

        Task<IList<PermissionModel>> GetAllAsync(string requestId = "");
    }
}
