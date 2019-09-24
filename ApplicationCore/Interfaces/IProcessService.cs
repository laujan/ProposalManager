// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApplicationCore.ViewModels;
using Newtonsoft.Json.Linq;

namespace ApplicationCore.Interfaces
{
    public interface IProcessService
    {
        Task<ProcessTypeListViewModel> GetAllAsync(string requestId = "");
        Task<JObject> CreateItemAsync(ProcessTypeViewModel modelObject, string requestId = "");
        Task<JObject> UpdateItemAsync(ProcessTypeViewModel modelObject, string requestId = "");
        Task<StatusCodes> DeleteItemAsync(ProcessTypeViewModel modelObject, string requestId = "");
    }
}
