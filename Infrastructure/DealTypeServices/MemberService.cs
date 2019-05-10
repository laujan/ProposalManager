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
using ApplicationCore.Services;
using ApplicationCore.Models;
using Infrastructure.Authorization;
using ApplicationCore.Authorization;
using System.Net;
using Newtonsoft.Json.Linq;

namespace Infrastructure.DealTypeServices
{
    public class MemberService : BaseService<MemberService>, IMemberService
    {
        private readonly CardNotificationService _cardNotificationService;
        private readonly IAuthorizationService _authorizationService;
        private readonly IPermissionRepository _permissionRepository;
        protected readonly Infrastructure.Services.GraphUserAppService _graphUserAppService;

        public MemberService(
            ILogger<MemberService> logger,
            IOptionsMonitor<AppOptions> appOptions,
            IAuthorizationService authorizationService,
            IPermissionRepository permissionRepository,
            Infrastructure.Services.GraphUserAppService graphUserAppService,
            CardNotificationService cardNotificationService) : base(logger, appOptions)
        {
            Guard.Against.Null(logger, nameof(logger));
            Guard.Against.Null(appOptions, nameof(appOptions));
            Guard.Against.Null(cardNotificationService, nameof(cardNotificationService));
            Guard.Against.Null(authorizationService, nameof(authorizationService));
            Guard.Against.Null(permissionRepository, nameof(permissionRepository));

            _cardNotificationService = cardNotificationService;
            _authorizationService = authorizationService;
            _permissionRepository = permissionRepository;
            _graphUserAppService = graphUserAppService;
        }

        public async Task<Opportunity> CreateWorkflowAsync(Opportunity opportunity, string requestId = "")
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

            var groupID = jsonDyn.value[0].id.ToString();

            foreach (var teamMember in opportunity.Content.TeamMembers)
            {
                var userId = teamMember.Id;
                if (teamMember.TeamsMembership == TeamsMembership.Owner)
                {
                    try
                    {
                        Guard.Against.NullOrEmpty(teamMember.RoleId, $"UpdateWorkflowAsync_{teamMember.DisplayName} Id NullOrEmpty", requestId);
                        await _graphUserAppService.AddGroupOwnerAsync(userId, groupID, requestId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"RequestId: {requestId} - userId: {userId} - AddGroupOwnerAsync error in CreateWorkflowAsync: {ex}");
                    }
                }
                else { 

                    try
                    {
                        Guard.Against.NullOrEmpty(userId, "CreateWorkflowAsync_LoanOffier_Ups Null or empty", requestId);
                        await _graphUserAppService.AddGroupMemberAsync(userId, groupID);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"RequestId: {requestId} - userId: {userId} - AddGroupMemberAsync error in CreateWorkflowAsync: {ex}");
                    }
                }
            }

            return opportunity;
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

            foreach (var teamMember in opportunity.Content.TeamMembers)
            {
                var userId = teamMember.Id;
                if (teamMember.TeamsMembership == TeamsMembership.Owner)
                {
                    try
                    {
                        Guard.Against.NullOrEmpty(teamMember.RoleId, $"UpdateWorkflowAsync_{teamMember.DisplayName} Id NullOrEmpty", requestId);
                        var responseJson = await _graphUserAppService.AddGroupOwnerAsync(userId, groupID, requestId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"RequestId: {requestId} - userId: {userId} - AddGroupOwnerAsync error in CreateWorkflowAsync: {ex}");
                    }
                }
                else
                {

                    try
                    {
                        Guard.Against.NullOrEmpty(userId, "CreateWorkflowAsync_LoanOffier_Ups Null or empty", requestId);
                        var responseJson = await _graphUserAppService.AddGroupMemberAsync(userId, groupID);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"RequestId: {requestId} - userId: {userId} - AddGroupMemberAsync error in CreateWorkflowAsync: {ex}");
                    }
                }
            }

            return opportunity;
        }
    }
}

