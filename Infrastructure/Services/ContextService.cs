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
using Infrastructure.Services;
using ApplicationCore.Helpers;
using ApplicationCore.Models;
using ApplicationCore.Entities;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Infrastructure.Authorization;
using ApplicationCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services
{
	public class ContextService : BaseService<ContextService>, IContextService
	{
		private readonly GraphSharePointAppService _graphSharePointAppService;
        private readonly DocumentIdActivatorConfiguration documentIdActivatorConfiguration;
        private readonly IAzureKeyVaultService _azureKeyVaultService;

        public ContextService(
			ILogger<ContextService> logger,
            IOptionsMonitor<AppOptions> appOptions,
            IConfiguration configuration,
            GraphSharePointAppService graphSharePointAppService,
            IAzureKeyVaultService azureKeyVaultService) : base(logger, appOptions)
		{
			Guard.Against.Null(graphSharePointAppService, nameof(graphSharePointAppService));

            _graphSharePointAppService = graphSharePointAppService;
            _azureKeyVaultService = azureKeyVaultService;

            documentIdActivatorConfiguration = new DocumentIdActivatorConfiguration();
            configuration.Bind(DocumentIdActivatorConfiguration.ConfigurationName, documentIdActivatorConfiguration);
        }

        public async Task<ClientSettingsModel> GetClientSetingsAsync()
        {
            var clientSettings = new ClientSettingsModel();
            clientSettings.SharePointHostName = _appOptions.SharePointHostName;
            clientSettings.ProposalManagementRootSiteId = _appOptions.ProposalManagementRootSiteId;
            clientSettings.CategoriesListId = _appOptions.CategoriesListId;
            clientSettings.TemplateListId = _appOptions.TemplateListId;
            clientSettings.RoleListId = _appOptions.RoleListId;
            clientSettings.Permissions = _appOptions.Permissions;
            clientSettings.ProcessListId = _appOptions.ProcessListId;
            clientSettings.WorkSpaceId = _appOptions.PBIWorkSpaceId;
            clientSettings.IndustryListId = _appOptions.IndustryListId;
            clientSettings.RegionsListId = _appOptions.RegionsListId;
            clientSettings.DashboardListId = _appOptions.DashboardListId;
            clientSettings.RoleMappingsListId = _appOptions.RoleMappingsListId;
            clientSettings.OpportunitiesListId = _appOptions.OpportunitiesListId;
            clientSettings.SharePointListsPrefix = _appOptions.SharePointListsPrefix;

            clientSettings.AllowedTenants = _appOptions.AllowedTenants;
            clientSettings.BotServiceUrl = _appOptions.BotServiceUrl;
            clientSettings.BotName = _appOptions.BotName;
            clientSettings.BotId = _appOptions.BotId;

            clientSettings.PBIApplicationId = _appOptions.PBIApplicationId;
            clientSettings.PBIWorkSpaceId = _appOptions.PBIWorkSpaceId;
            clientSettings.PBIReportId = _appOptions.PBIReportId;
            clientSettings.PBITenantId = _appOptions.PBITenantId;

            try
            {
                clientSettings.PBIUserName = await _azureKeyVaultService.GetValueFromVaultAsync(_appOptions.PBIUserName);
                clientSettings.PBIUserPassword = await _azureKeyVaultService.GetValueFromVaultAsync(_appOptions.PBIUserPassword);
            }
            catch(Exception ex)
            {
                _logger.LogError("Get PowerBI user credentials error: " + ex);
                clientSettings.PBIUserName = "";
                clientSettings.PBIUserPassword = "";
            }


            clientSettings.GeneralProposalManagementTeam = _appOptions.GeneralProposalManagementTeam;
            clientSettings.ProposalManagerAddInName = _appOptions.ProposalManagerAddInName;
            clientSettings.ProposalManagerGroupID = _appOptions.ProposalManagerGroupID;
            clientSettings.TeamsAppInstanceId = _appOptions.TeamsAppInstanceId;
            clientSettings.UserProfileCacheExpiration = _appOptions.UserProfileCacheExpiration;
            clientSettings.SetupPage = _appOptions.SetupPage;
            clientSettings.GraphRequestUrl = _appOptions.GraphRequestUrl;
            clientSettings.GraphBetaRequestUrl = _appOptions.GraphBetaRequestUrl;
            clientSettings.SharePointSiteRelativeName = _appOptions.SharePointSiteRelativeName;
            clientSettings.VaultBaseUrl = _appOptions.VaultBaseUrl;

            clientSettings.MicrosoftAppId = _appOptions.MicrosoftAppId;
            clientSettings.MicrosoftAppPassword = _appOptions.MicrosoftAppPassword;

            clientSettings.WebhookAddress = documentIdActivatorConfiguration.WebhookAddress;
            clientSettings.WebhookUsername = documentIdActivatorConfiguration.WebhookUsername;
            clientSettings.WebhookPassword = documentIdActivatorConfiguration.WebhookPassword;

            return clientSettings;
        }

		public async Task<JObject> GetTeamGroupDriveAsync(string teamGroupName)
		{
			_logger.LogInformation("GetTeamGroupDriveAsync called.");

			try
			{
				Guard.Against.NullOrEmpty(teamGroupName, nameof(teamGroupName));
				string result = string.Concat(teamGroupName.Where(c => !char.IsWhiteSpace(c)));

				// TODO: Implement,, the below code is part of boilerplate
				var siteIdResponse = await _graphSharePointAppService.GetSiteIdAsync(_appOptions.SharePointHostName, result);
				dynamic responseDyn = siteIdResponse;
				var siteId = responseDyn.id.ToString();

				var driveResponse = await _graphSharePointAppService.GetSiteDriveAsync(siteId);

				return driveResponse;
			}
			catch (Exception ex)
			{
				_logger.LogError("GetTeamGroupDriveAsync error: " + ex);
				throw;
			}

		}

		public async Task<JObject> GetSiteDriveAsync(string siteName)
		{
			_logger.LogInformation("GetChannelDriveAsync called.");

			Guard.Against.NullOrEmpty(siteName, nameof(siteName));
			string result = string.Concat(siteName.Where(c => !char.IsWhiteSpace(c)));

			var siteIdResponse = await _graphSharePointAppService.GetSiteIdAsync(_appOptions.SharePointHostName, result);

			// Response field id is composed as follows: {hostname},{spsite.id},{spweb.id}
			var siteId = siteIdResponse["id"].ToString();

			var driveResponse = await _graphSharePointAppService.GetSiteDriveAsync(siteId);

			return driveResponse;
		}

		public async Task<JObject> GetSiteIdAsync(string siteName)
		{
			_logger.LogInformation("GetSiteIdAsync called.");

			Guard.Against.NullOrEmpty(siteName, nameof(siteName));
			string result = string.Concat(siteName.Where(c => !char.IsWhiteSpace(c)));

			var siteIdResponse = await _graphSharePointAppService.GetSiteIdAsync(_appOptions.SharePointHostName, result);

			return siteIdResponse;
		}

		public async Task<JArray> GetOpportunityStatusAllAsync()
		{
			_logger.LogInformation("GetOpportunityStatusAllAsync called.");

			var response = JsonConvert.SerializeObject(OpportunityState.List.ToArray());

			JArray oppStatusArray = JArray.Parse(response);

			return oppStatusArray;

		}

		public async Task<JArray> GetActionStatusAllAsync()
		{
			_logger.LogInformation("GetActionStatusAllAsync called.");

			var response = JsonConvert.SerializeObject(ActionStatus.List.ToArray());

			JArray actionStatusArray = JArray.Parse(response);

			return actionStatusArray;
		}
	}
}
