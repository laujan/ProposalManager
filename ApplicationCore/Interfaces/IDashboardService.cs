// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using ApplicationCore;
using System.Collections.Generic;
using System.Threading.Tasks;
using ApplicationCore.Models;
using ApplicationCore.ViewModels;
using ApplicationCore.Artifacts;

namespace ApplicationCore.Interfaces
{
    public interface IDashboardService
    {
        Task<Opportunity> CreateWorkflowAsync(Opportunity opportunity, string requestId = "");

        Task<Opportunity> UpdateWorkflowAsync(Opportunity opportunity, string requestId = "");
        Task<StatusCodes> DeleteOpportunityAsync(string id, string requestId = "");
    }
}
