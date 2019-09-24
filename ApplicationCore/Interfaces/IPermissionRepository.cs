// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using ApplicationCore.Entities;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationCore.Interfaces
{
    public interface IPermissionRepository
    {
        Task<JObject> CreateItemAsync(Permission entity, string requestId = "");

        Task<StatusCodes> DeleteItemAsync(string id, string requestId = "");

        Task<IList<Permission>> GetAllAsync(string requestId = "");
        void CleanCache();
    }
}
