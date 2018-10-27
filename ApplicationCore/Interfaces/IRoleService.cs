// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using ApplicationCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using ApplicationCore.Models;
using ApplicationCore.ViewModels;

namespace ApplicationCore.Interfaces
{
    public interface IRoleService
    {
        Task<StatusCodes> CreateItemAsync(RoleModel modelObject, string requestId = "");

        Task<StatusCodes> UpdateItemAsync(RoleModel modelObject, string requestId = "");

        Task<StatusCodes> DeleteItemAsync(string id, string requestId = "");

        Task<IList<RoleModel>> GetAllAsync(string requestId = "");
    }
}
