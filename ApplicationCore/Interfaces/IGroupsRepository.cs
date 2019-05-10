// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using ApplicationCore.Entities;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationCore.Interfaces
{
    public interface IGroupsRepository
    {
        Task<StatusCodes> CreateItemAsync(Groups entity, string requestId = "");

        Task<StatusCodes> UpdateItemAsync(Groups entity, string requestId = "");

        Task<StatusCodes> DeleteItemAsync(string id, string requestId = "");

        Task<IList<Groups>> GetAllAsync(string requestId = "");
    }
}
