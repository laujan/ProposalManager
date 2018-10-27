// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ApplicationCore.Artifacts;
using ApplicationCore.ViewModels;

namespace ApplicationCore.Interfaces
{
    public interface IDealTypeService
    {
        Task<Opportunity> CreateWorkflowAsync(Opportunity opportunity, string requestId = "");

        Task<Opportunity> UpdateWorkflowAsync(Opportunity opportunity, string requestId = "");

        Task<Opportunity> MapToEntityAsync(Opportunity entity, OpportunityViewModel viewModel, string requestId = "");

        Task<OpportunityViewModel> MapToModelAsync(Opportunity entity, OpportunityViewModel viewModel, string requestId = "");
    }
}
