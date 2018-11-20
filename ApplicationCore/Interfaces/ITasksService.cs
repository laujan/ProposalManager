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
    public interface ITasksService
    {
        Task<StatusCodes> CreateItemAsync(TasksModel modelObject, string requestId = "");

        Task<StatusCodes> UpdateItemAsync(TasksModel modelObject, string requestId = "");

        Task<StatusCodes> DeleteItemAsync(string id, string requestId = "");

        Task<IList<TasksModel>> GetAllAsync(string requestId = "");
    }
}