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
using Infrastructure.GraphApi;
using ApplicationCore.Authorization;
using System.Net;
using Newtonsoft.Json.Linq;
using Infrastructure;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.DealTypeServices
{
    public class TeamChannelService  : BaseService<TeamChannelService>, ITeamChannelService
    {
        private readonly CardNotificationService _cardNotificationService;
        private readonly IAuthorizationService _authorizationService;
        private readonly IPermissionRepository _permissionRepository;
        private readonly Infrastructure.Services.GraphUserAppService _graphUserAppService;
        protected readonly Infrastructure.Services.GraphTeamsAppService _graphTeamsAppService;
        protected readonly Infrastructure.Services.GraphTeamsOnBehalfService _graphTeamsOnBehalfService;
        private readonly IAddInHelper _addInHelper;
        private readonly string _baseUrl;

        public TeamChannelService(
            ILogger<TeamChannelService> logger,
            IOptionsMonitor<AppOptions> appOptions,
            IAuthorizationService authorizationService,
            IPermissionRepository permissionRepository,
            Infrastructure.Services.GraphUserAppService graphUserAppService,
            Infrastructure.Services.GraphTeamsAppService graphTeamsAppService,
            Infrastructure.Services.GraphTeamsOnBehalfService graphTeamsOnBehalfService,
            IAddInHelper addInHelper,
            IConfiguration configuration,
            CardNotificationService cardNotificationService) : base(logger, appOptions)
        {
            Guard.Against.Null(logger, nameof(logger));
            Guard.Against.Null(appOptions, nameof(appOptions));
            Guard.Against.Null(cardNotificationService, nameof(cardNotificationService));
            Guard.Against.Null(authorizationService, nameof(authorizationService));
            Guard.Against.Null(permissionRepository, nameof(permissionRepository));

            Guard.Against.Null(graphUserAppService, nameof(graphUserAppService));
            Guard.Against.Null(graphTeamsAppService, nameof(graphTeamsAppService));
            Guard.Against.Null(graphTeamsOnBehalfService, nameof(graphTeamsOnBehalfService));
            Guard.Against.Null(addInHelper, nameof(addInHelper));


            var azureOptions = new AzureAdOptions();
            configuration.Bind("AzureAd", azureOptions);

            _graphUserAppService = graphUserAppService;
            _cardNotificationService = cardNotificationService;
            _authorizationService = authorizationService;
            _permissionRepository = permissionRepository;
            _graphTeamsAppService = graphTeamsAppService;
            _graphTeamsOnBehalfService = graphTeamsOnBehalfService;
            _addInHelper = addInHelper;
            _baseUrl = azureOptions.BaseUrl;
        }

        public async Task<Opportunity> CreateWorkflowAsync(Opportunity opportunity, string requestId = "")
        {

            _logger.LogError($"RequestId: {requestId} - createTeamAndChannels Started");

            try
            {
                string memberId = String.Empty;
                if (opportunity.Content.TeamMembers.Any())
                {
                    memberId = opportunity.Content.TeamMembers.FirstOrDefault()?.Id;
                }

                var groupID = String.Empty;
                try
                {                    
                    var response = await _graphTeamsAppService.CreateTeamAsync(opportunity.DisplayName, memberId, requestId);
                    groupID = response["id"].ToString();
                }
                catch(Exception ex)
                {
                    throw new ResponseException($"RequestId: {requestId} - CreateWorkflowAsync Service Exception: {ex}");
                }

                var generalChannel = await _graphTeamsAppService.ListChannelAsync(groupID);
                dynamic generalChannelObj = generalChannel;
                string generalChannelId = generalChannelObj.value[0].id.ToString();

                if (!String.IsNullOrEmpty(generalChannelId))
                {
                    opportunity.Metadata.OpportunityChannelId = generalChannelId;
                }
                else
                    throw new ResponseException($"RequestId: {requestId} - CreateWorkflowAsync Service Exception: Opportunity Channel is not created");



                var channelInfo = new List<Tuple<string, string>>();
                channelInfo.Add(new Tuple<string, string>("General", generalChannelId));

                foreach (var process in opportunity.Content.Template.ProcessList)
                {
                    if (process.Channel.ToLower() != "none" && process.ProcessType.ToLower() !="none")
                    {
                        var response = await _graphTeamsAppService.CreateChannelAsync(groupID, process.Channel, process.Channel + " Channel");
                        channelInfo.Add(new Tuple<string, string>(process.Channel, response["id"].ToString()));
                    }
                }

                try
                {
                    await _graphTeamsOnBehalfService.AddAppToTeamAsync(groupID);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"RequestId: {requestId} - CreateTeamAndChannels_AddAppToTeamAsync Service Exception: {ex}");
                }

                foreach (var channel in channelInfo)
                {
                    try
                    {
                        await _graphTeamsAppService.AddTab(channel.Item1, groupID, channel.Item2, opportunity.DisplayName, _baseUrl);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"RequestId: {requestId} - Adding General Tab Service Exception: {ex}");
                    }
                }

                if (!string.IsNullOrWhiteSpace(opportunity.Reference))
                {
                    try
                    {
                        // Call to AddIn helper once team has been created
                        await _addInHelper.CallAddInWebhookAsync(opportunity, requestId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"RequestId: {requestId} -_addInHelper.CallAddInWebhookAsync(opportunity, requestId): {ex}");
                    }
                }

                // Activate Document Id Service
                await _addInHelper.ActivateDocumentId(opportunity);

                return opportunity;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - CreateTeamAndChannels Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - CreateTeamAndChannels Service Exception: {ex}");
            }
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

            _logger.LogError($"RequestId: {requestId} - UpdateTeamAndChannels Started");

            try
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


                var generalChannel = await _graphTeamsAppService.ListChannelAsync(groupID);
                dynamic generalChannelObj = generalChannel;
                string generalChannelId = generalChannelObj.value[0].id.ToString();

                if (!String.IsNullOrEmpty(generalChannelId))
                {
                    opportunity.Metadata.OpportunityChannelId = generalChannelId;
                }
                else
                    throw new ResponseException($"RequestId: {requestId} - CreateWorkflowAsync Service Exception: Opportunity Channel is not created");



                var channelInfo = new List<Tuple<string, string>>();
                foreach (var process in opportunity.Content.Template.ProcessList)
                {
                    if (process.Channel.ToLower() != "none" && process.ProcessType.ToLower() !="none")
                    {
                        try
                        {
                            var response = await _graphTeamsAppService.CreateChannelAsync(groupID, process.Channel, process.Channel + " Channel");
                            channelInfo.Add(new Tuple<string, string>(process.Channel, response["id"].ToString()));
                        }
                        catch(Exception ex)
                        {
                            _logger.LogError($"RequestId: {requestId} - Adding new channel Exception: {ex}");
                        }
                     
                    }
                }

                foreach (var channel in channelInfo)
                {
                    try
                    {
                        await _graphTeamsAppService.AddTab(channel.Item1, groupID, channel.Item2, opportunity.DisplayName, _baseUrl);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"RequestId: {requestId} - Adding General Tab Service Exception: {ex}");
                    }
                }

                return opportunity;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - CreateTeamAndChannels Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - CreateTeamAndChannels Service Exception: {ex}");
            }
        }
    }
}
