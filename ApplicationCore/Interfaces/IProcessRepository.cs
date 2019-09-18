// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using System.Collections.Generic;
using System.Threading.Tasks;
using ApplicationCore.Entities;
using Newtonsoft.Json.Linq;

namespace ApplicationCore.Interfaces
{
    public interface IProcessRepository
    {
        Task<IList<ProcessesType>> GetAllAsync(string requestId = "");
        Task<JObject> CreateItemAsync(ProcessesType modelObject, string requestId = "");
        Task<StatusCodes> UpdateItemAsync(ProcessesType modelObject, string requestId = "");
        Task<StatusCodes> DeleteItemAsync(string id, string requestId = "");
    }
}