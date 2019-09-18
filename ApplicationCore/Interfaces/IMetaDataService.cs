// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using ApplicationCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using ApplicationCore.Models;
using ApplicationCore.ViewModels;
using Newtonsoft.Json.Linq;

namespace ApplicationCore.Interfaces
{
    public interface IMetaDataService
    {
        Task<JObject> CreateItemAsync(MetaDataModel modelObject, string requestId = "");

        Task<StatusCodes> UpdateItemAsync(MetaDataModel modelObject, string requestId = "");

        Task<StatusCodes> DeleteItemAsync(string id, string requestId = "");

        Task<IList<MetaDataModel>> GetAllAsync(string requestId = "");
    }
}
