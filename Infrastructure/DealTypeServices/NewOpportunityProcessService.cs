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
    public class NewOpportunityProcessService : BaseService<NewOpportunityProcessService>, IDealTypeService
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

            bool check = true;
            dynamic jsonDyn = null;
            var opportunityName = WebUtility.UrlEncode(opportunity.DisplayName);
            var options = new List<QueryParam>();
            options.Add(new QueryParam("filter", $"startswith(displayName,'{opportunityName}')"));
            while (check)
            {
                var groupIdJson = await _graphUserAppService.GetGroupAsync(options, "", requestId);
                jsonDyn = groupIdJson;
                JArray jsonArray = JArray.Parse(jsonDyn["value"].ToString());
                if (jsonArray.Count() > 0)
                {
                    if (!String.IsNullOrEmpty(jsonDyn.value[0].id.ToString()))
                        check = false;
                }

            }

            var groupID = String.Empty;
            groupID = jsonDyn.value[0].id.ToString();
            

            bool isLoanOfficerSelected = false;

            foreach (var item in opportunity.Content.TeamMembers)
            {
                //var groupID = group;
                var userId = item.Id;
                var oItem = item;

                if (item.AssignedRole.DisplayName == "RelationshipManager")
                {
                    foreach (var process in opportunity.Content.DealType.ProcessList)
                        if (process.ProcessStep.ToLower() == "new opportunity")
                            process.Status = ActionStatus.InProgress;
                    try
                    {
                        Guard.Against.NullOrEmpty(item.Id, $"UpdateWorkflowAsync_{item.AssignedRole.DisplayName} Id NullOrEmpty", requestId);
                        var responseJson = await _graphUserAppService.AddGroupOwnerAsync(userId, groupID, requestId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"RequestId: {requestId} - userId: {userId} - UpdateWorkflowAsync_AddGroupOwnerAsync_{item.AssignedRole.DisplayName} error in CreateWorkflowAsync: {ex}");
                    }
                }
                else if (item.AssignedRole.DisplayName == "LoanOfficer")
                {
                    if (!String.IsNullOrEmpty(item.Id))
                    {
                        isLoanOfficerSelected = true;
                    }

                    try
                    {
                        Guard.Against.NullOrEmpty(userId, "CreateWorkflowAsync_LoanOffier_Ups Null or empty", requestId);
                        var responseJson = await _graphUserAppService.AddGroupOwnerAsync(userId, groupID);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"RequestId: {requestId} - userId: {userId} - AddGroupOwnerAsync error in CreateWorkflowAsync: {ex}");
                    }
                }
                //else
                //{
                    //adding of the team member
                    //if (!String.IsNullOrEmpty(item.Fields.UserPrincipalName))
                    //{
                        try
                        {
                            Guard.Against.NullOrEmpty(item.Id, $"UpdateStartProcessStatus_{item.AssignedRole.DisplayName} Id NullOrEmpty", requestId);
                            var responseJson = await _graphUserAppService.AddGroupMemberAsync(item.Id, groupID, requestId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"RequestId: {requestId} - userId: {item.Id} - UpdateStartProcessStatus_AddGroupMemberAsync_{item.AssignedRole.DisplayName} error in CreateWorkflowAsync: {ex}");
                        }
                    //}
                //}
            }
            
            if(isLoanOfficerSelected)
            {
                foreach (var process in opportunity.Content.DealType.ProcessList)
                    if (process.ProcessStep.ToLower() == "new opportunity" && process.Status!=ActionStatus.Completed)
                        process.Status = ActionStatus.Completed;
            }
            else
            {
                foreach (var process in opportunity.Content.DealType.ProcessList)
                    if (process.ProcessStep.ToLower() == "new opportunity" && process.Status != ActionStatus.InProgress)
                        process.Status = ActionStatus.InProgress;
            }

            return opportunity;
        }
    }
}
