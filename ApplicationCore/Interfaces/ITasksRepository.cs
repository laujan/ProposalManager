﻿// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using System.Collections.Generic;
using System.Threading.Tasks;
using ApplicationCore.Entities;
using Newtonsoft.Json.Linq;

namespace ApplicationCore.Interfaces
{
    public interface ITasksRepository
    {
        Task<JObject> CreateItemAsync(Tasks entity, string requestId = "");

        Task<StatusCodes> UpdateItemAsync(Tasks entity, string requestId = "");

        Task<StatusCodes> DeleteItemAsync(string id, string requestId = "");

        Task<IList<Tasks>> GetAllAsync(string requestId = "");
    }
}