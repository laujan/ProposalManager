// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using ApplicationCore.Models;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationCore.Interfaces
{
    public interface IGroupsService 
    {
        
        Task<StatusCodes> CreateItemAsync(GroupModel modelObject, string requestId = "");

        Task<StatusCodes> UpdateItemAsync(GroupModel modelObject, string requestId = "");

        Task<StatusCodes> DeleteItemAsync(string id, string requestId = "");

        Task<IList<GroupModel>> GetAllAsync(string requestId = "");
        
    }
}
