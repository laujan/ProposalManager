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
using ApplicationCore.Interfaces;
using ApplicationCore;
using ApplicationCore.Helpers;
using ApplicationCore.Models;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Microsoft.Extensions.Configuration;

namespace Infrastructure.Services
{
    public class ContextService : BaseService<ContextService>, IContextService
	{
		private readonly GraphSharePointAppService _graphSharePointAppService;
        private readonly DocumentIdActivatorConfiguration documentIdActivatorConfiguration;
        private readonly IAzureKeyVaultService _azureKeyVaultService;
        public readonly IRoleService _roleService;

        public ContextService(
			ILogger<ContextService> logger,
            IOptionsMonitor<AppOptions> appOptions,
            IConfiguration configuration,
            GraphSharePointAppService graphSharePointAppService,
            IRoleService roleService,
            IAzureKeyVaultService azureKeyVaultService) : base(logger, appOptions)
		{
			Guard.Against.Null(graphSharePointAppService, nameof(graphSharePointAppService));

            _graphSharePointAppService = graphSharePointAppService;
            _azureKeyVaultService = azureKeyVaultService;
            _roleService = roleService;

            documentIdActivatorConfiguration = new DocumentIdActivatorConfiguration();
            configuration.Bind(DocumentIdActivatorConfiguration.ConfigurationName, documentIdActivatorConfiguration);
        }

        public async Task<ClientSettingsModel> GetClientSetingsAsync()
        {
            var clientSettings = new ClientSettingsModel();
            clientSettings.SharePointHostName = _appOptions.SharePointHostName;
            clientSettings.ProposalManagementRootSiteId = _appOptions.ProposalManagementRootSiteId;
            clientSettings.TemplateListId = _appOptions.TemplateListId;
            clientSettings.RoleListId = _appOptions.RoleListId;
            clientSettings.Permissions = _appOptions.Permissions;
            clientSettings.ProcessListId = _appOptions.ProcessListId;
            clientSettings.WorkSpaceId = _appOptions.PBIWorkSpaceId;
            clientSettings.DashboardListId = _appOptions.DashboardListId;
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
            clientSettings.AuditWorkspaceId = _appOptions.AuditWorkspaceId;
            clientSettings.AuditReportId = _appOptions.AuditReportId;
            clientSettings.AuditEnabled = _appOptions.AuditEnabled;

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

                return await _graphSharePointAppService.GetSiteDriveAsync(siteId);

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

            return await _graphSharePointAppService.GetSiteDriveAsync(siteIdResponse["id"].ToString());
		}

		public async Task<JObject> GetSiteIdAsync(string siteName)
		{
			_logger.LogInformation("GetSiteIdAsync called.");

			Guard.Against.NullOrEmpty(siteName, nameof(siteName));
			string result = string.Concat(siteName.Where(c => !char.IsWhiteSpace(c)));

	        return await _graphSharePointAppService.GetSiteIdAsync(_appOptions.SharePointHostName, result);
		}

		public JArray GetOpportunityStatusAllAsync()
		{
			_logger.LogInformation("GetOpportunityStatusAllAsync called.");

			return JArray.Parse(JsonConvert.SerializeObject(OpportunityState.List.ToArray()));

        }

		public JArray GetActionStatusAllAsync()
		{
			_logger.LogInformation("GetActionStatusAllAsync called.");

            return JArray.Parse(JsonConvert.SerializeObject(ActionStatus.List.ToArray()));

		}

        public async Task<List<ProcessRoleModel>> GetProcessRolesList(string requestId="")
        {
            _logger.LogInformation("GetProcessRolesList called.");
            var processRolesList = (await _roleService.GetAllAsync(requestId)).Select(item => new ProcessRoleModel { Key = item.Id, RoleName = item.DisplayName }).ToList();
            return processRolesList;
        }
        
    }
}
