// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ApplicationCore.ViewModels;
using ApplicationCore.Interfaces;
using ApplicationCore;
using ApplicationCore.Artifacts;
using ApplicationCore.Helpers;
using ApplicationCore.Entities;
using ApplicationCore.Helpers.Exceptions;
using ApplicationCore.Models;
using System.Net;
using Infrastructure.Services;
using Newtonsoft.Json.Linq;

namespace Infrastructure.DealTypeServices
{
    public class NewOpportunityProcessService : BaseService<NewOpportunityProcessService>, INewOpportunityProcessService
    {
        protected readonly GraphUserAppService _graphUserAppService;

        public NewOpportunityProcessService(
            ILogger<NewOpportunityProcessService> logger,
            IOptionsMonitor<AppOptions> appOptions,
            GraphUserAppService graphUserAppService) : base(logger, appOptions)
        {
            Guard.Against.Null(logger, nameof(logger));
            Guard.Against.Null(appOptions, nameof(appOptions));

            _graphUserAppService = graphUserAppService;
        }

        public async Task<Opportunity> CreateWorkflowAsync(Opportunity opportunity, string requestId = "")
        {
            return await UpdateDealTypeStatus(opportunity, requestId);
        }

        public Task<Opportunity> MapToEntityAsync(Opportunity entity, OpportunityViewModel viewModel, string requestId = "")
        {
            throw new NotImplementedException();
        }

        public Task<OpportunityViewModel> MapToModelAsync(Opportunity entity, OpportunityViewModel viewModel, string requestId = "")
        {
            throw new NotImplementedException();
        }

        public async Task<Opportunity> UpdateWorkflowAsync(Opportunity opportunity, string requestId = "")
        {
            return await UpdateDealTypeStatus(opportunity, requestId);
        }
        private async Task<Opportunity> UpdateDealTypeStatus(Opportunity opportunity, string requestId = "")
        {
            var groupID = string.Empty;
            var opportunityName = WebUtility.UrlEncode(opportunity.DisplayName);
            var options = new List<QueryParam> { new QueryParam("filter", $"startswith(displayName,'{opportunityName}')") };

            while (true)
            {
                dynamic jsonDyn = await _graphUserAppService.GetGroupAsync(options, "", requestId);
                JArray jsonArray = JArray.Parse(jsonDyn["value"].ToString());
                if (jsonArray.Count() > 0)
                {
                    if (!string.IsNullOrEmpty(jsonDyn.value[0].id.ToString()))
                    {
                        groupID = jsonDyn.value[0].id.ToString();
                        break;
                    }
                }
            }

            var processStatus = ActionStatus.NotStarted;

            foreach (var item in opportunity.Content.TeamMembers)
            {
                // QUESTION: adding LO as owner always fails because he's added in the create team process
                if (item.AssignedRole.DisplayName.Equals("RelationshipManager", StringComparison.OrdinalIgnoreCase))
                {
                    processStatus = ActionStatus.InProgress;

                    try
                    {
                        Guard.Against.NullOrEmpty(item.Id, $"UpdateWorkflowAsync_{item.AssignedRole.DisplayName} Id NullOrEmpty", requestId);
                        await _graphUserAppService.AddGroupOwnerAsync(item.Id, groupID, requestId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"RequestId: {requestId} - userId: {item.Id} - UpdateWorkflowAsync_AddGroupOwnerAsync_{item.AssignedRole.DisplayName} error in CreateWorkflowAsync: {ex}");
                    }
                }

                if (item.AssignedRole.DisplayName.Equals("LoanOfficer", StringComparison.OrdinalIgnoreCase))
                {
                    processStatus = ActionStatus.Completed;
                }

                try
                {
                    Guard.Against.NullOrEmpty(item.Id, $"UpdateStartProcessStatus_{item.AssignedRole.DisplayName} Id NullOrEmpty", requestId);
                    await _graphUserAppService.AddGroupMemberAsync(item.Id, groupID, requestId);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"RequestId: {requestId} - userId: {item.Id} - UpdateStartProcessStatus_AddGroupMemberAsync_{item.AssignedRole.DisplayName} error in CreateWorkflowAsync: {ex}");
                }
            }

            // Update process status
            var process = opportunity.Content.DealType.ProcessList.FirstOrDefault(x => x.ProcessStep.Equals("new opportunity", StringComparison.OrdinalIgnoreCase));

            if (process != null)
            {
                process.Status = processStatus;
            }

            return opportunity;
        }
    }
}
