using ApplicationCore.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationCore.Interfaces
{
    public interface IPermissionRepository
    {
        Task<StatusCodes> CreateItemAsync(Permission entity, string requestId = "");

        Task<StatusCodes> DeleteItemAsync(string id, string requestId = "");

        Task<IList<Permission>> GetAllAsync(string requestId = "");
    }
}
