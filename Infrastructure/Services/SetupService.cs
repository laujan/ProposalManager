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
using Infrastructure.Helpers;
using ApplicationCore.Entities.GraphServices;
using System.Net;
using System.Xml;
using System.Xml.Linq;

namespace Infrastructure.Services
{
    public class SetupService : BaseService<SetupService>, ISetupService
    {
        private readonly GraphSharePointAppService _graphSharePointAppService;
        private readonly IWritableOptions<AppOptions> _writableOptions;
        private readonly IWritableOptions<DocumentIdActivatorConfiguration> documentIdActivatorConfigurationWritableOptions;
        //protected readonly SharePointListsSchemaHelper _sharePointListsSchemaHelper;
        private readonly GraphTeamsAppService _graphTeamsAppService;
        private readonly GraphUserAppService _graphUserAppService;
        private readonly IUserContext _userContext;
        private readonly IAzureKeyVaultService _azureKeyVaultService;

        public SetupService(
            ILogger<SetupService> logger,
            IOptionsMonitor<AppOptions> appOptions,
            GraphSharePointAppService graphSharePointAppService,
            IWritableOptions<AppOptions> writableOptions,
            IWritableOptions<DocumentIdActivatorConfiguration> documentIdActivatorConfigurationWritableOptions,
            GraphTeamsAppService graphTeamsAppService,
            GraphUserAppService graphUserAppService,
            IUserContext userContext,
            IAzureKeyVaultService azureKeyVaultService) : base(logger, appOptions)
        {
            Guard.Against.Null(graphSharePointAppService, nameof(graphSharePointAppService));
            Guard.Against.Null(writableOptions, nameof(writableOptions));
            Guard.Against.Null(graphTeamsAppService, nameof(graphTeamsAppService));
            Guard.Against.Null(graphUserAppService, nameof(graphUserAppService));
            Guard.Against.Null(userContext, nameof(userContext));
            Guard.Against.Null(azureKeyVaultService, nameof(azureKeyVaultService));

            _graphSharePointAppService = graphSharePointAppService;
            _writableOptions = writableOptions;
            this.documentIdActivatorConfigurationWritableOptions = documentIdActivatorConfigurationWritableOptions;
            //_sharePointListsSchemaHelper = sharePointListsSchemaHelper;
            _graphTeamsAppService = graphTeamsAppService;
            _graphUserAppService = graphUserAppService;
            _userContext = userContext;
            _azureKeyVaultService = azureKeyVaultService;

        }

        public async Task<StatusCodes> UpdateAppOpptionsAsync(string key, string value, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - SetupService_UpdateAppOpptionsAsync called.");

            //Save powerbi username and password to to Key Vault.
            if (key == "PBIUserName" || key == "PBIUserPassword")
            {
                await _azureKeyVaultService.SetValueInVaultAsync(key, value, requestId);
                return StatusCodes.Status200OK;
            }
            //save values to appsettings.
            else
            {
                await _writableOptions.UpdateAsync(key, value, requestId);
                return StatusCodes.Status200OK;
            }
        }

        public Task<StatusCodes> UpdateDocumentIdActivatorOptionsAsync(string key, string value, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - SetupService_UpdateAppOpptionsAsync called.");

            if (!key.StartsWith("SharePoint"))
                documentIdActivatorConfigurationWritableOptions.UpdateAsync(key, value, requestId);
            else
                SaveSharePointAppSetting(key, value);

            return Task.FromResult(StatusCodes.Status200OK);
        }

        private const string OpportunitySiteProvisionerConfigurationFileName = @"app_data\jobs\triggered\OpportunitySiteProvisioner\OpportunitySiteProvisioner.exe.config";

        private void SaveSharePointAppSetting(string key, string value)
        {
            var actualKey = key.Replace("SharePoint", string.Empty);
            var document = new XmlDocument();
            document.Load(OpportunitySiteProvisionerConfigurationFileName);
            document["configuration"]["appSettings"].ChildNodes.OfType<XmlNode>().First(n => n.Attributes["key"].Value == actualKey).Attributes["value"].Value = value;
            document.Save(OpportunitySiteProvisionerConfigurationFileName);
        }

        public async Task CreateSiteProcessesAsync(string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - SetupService_CreateSiteProcessesAsync called.");
            try
            {
                var siteList = new SiteList
                {
                    SiteId = _appOptions.ProposalManagementRootSiteId,
                    ListId = _appOptions.ProcessListId
                };
                var processes = getProcesses();
                foreach (var process in processes)
                {
                    try
                    {
                        // Create Json object for SharePoint create list item
                        dynamic itemFieldsJson = new JObject();
                        itemFieldsJson.ProcessType = process.ProcessType;
                        itemFieldsJson.Channel = process.Channel;
                        itemFieldsJson.ProcessStep = process.ProcessStep;

                        dynamic itemJson = new JObject();
                        itemJson.fields = itemFieldsJson;


                        _logger.LogDebug($"RequestId: {requestId} - SetupService_CreateSiteProcessesAsync debug result: {itemJson}");

                        var result = await _graphSharePointAppService.CreateListItemAsync(siteList, itemJson.ToString());

                        _logger.LogDebug($"RequestId: {requestId} - SetupService_CreateSiteProcessesAsync debug result: {result}");
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"RequestId: {requestId} - SetupService_CreateSiteProcessesAsync warning: {ex}");
                    }

                }

            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - SetupService_CreateSiteProcessesAsync Service Exception: {ex}");
                throw;
            }
        }

        public async Task CreateSitePermissionsAsync(string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - SetupService_CreateSitePermissionsAsync called.");
            try
            {
                var siteList = new SiteList
                {
                    SiteId = _appOptions.ProposalManagementRootSiteId,
                    ListId = _appOptions.Permissions
                };
                var permissions = getPermissions();

                foreach (string permission in permissions)
                {
                    try
                    {
                        // Create Json object for SharePoint create list item
                        dynamic itemFieldsJson = new JObject();
                        itemFieldsJson.Name = permission;

                        dynamic itemJson = new JObject();
                        itemJson.fields = itemFieldsJson;

                        var result = await _graphSharePointAppService.CreateListItemAsync(siteList, itemJson.ToString());
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"RequestId: {requestId} - SetupService_CreateSitePermissionsAsync warning: {ex}");
                    }
                }

            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - SetupService_CreateSitePermissionsAsync Service Exception: {ex}");
                throw;
            }
        }

        public async Task CreateSiteRolesAsync(string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - SetupService_CreateSiteRolesAsync called.");

            try
            {
                var siteList = new SiteList
                {
                    SiteId = _appOptions.ProposalManagementRootSiteId,
                    ListId = _appOptions.RoleListId
                };
                var roles = getRoles();

                foreach (string role in roles)
                {
                    try
                    {
                        // Create Json object for SharePoint create list item
                        dynamic itemFieldsJson = new JObject();
                        itemFieldsJson.Name = role;

                        dynamic itemJson = new JObject();
                        itemJson.fields = itemFieldsJson;

                        var result = await _graphSharePointAppService.CreateListItemAsync(siteList, itemJson.ToString());
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"RequestId: {requestId} - SetupService_CreateSiteRolesAsync warning: {ex}");
                    }
                }

            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - SetupService_CreateSiteRolesAsync Service Exception: {ex}");
                throw;
            }
        }

        public async Task CreateSiteTasksAsync(string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - SetupService_CreateSiteTasksAsync called.");

            try
            {
                var siteList = new SiteList
                {
                    SiteId = _appOptions.ProposalManagementRootSiteId,
                    ListId = _appOptions.TasksListId
                };
                var tasks = getTasks();

                foreach (string task in tasks)
                {
                    try
                    {
                        // Create Json object for SharePoint create list item
                        dynamic itemFieldsJson = new JObject();
                        itemFieldsJson.Name = task;

                        dynamic itemJson = new JObject();
                        itemJson.fields = itemFieldsJson;

                        var result = await _graphSharePointAppService.CreateListItemAsync(siteList, itemJson.ToString());
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning($"RequestId: {requestId} - SetupService_CreateSiteTasksAsync warning: {ex}");
                    }
                }

            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - SetupService_CreateSiteTasksAsync Service Exception: {ex}");
                throw;
            }
        }

        public async Task CreateSiteAdminPermissionsAsync(string adGroupName, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - SetupService_CreateSiteAdminPermissionsAsync called.");
            try
            {
                var siteList = new SiteList
                {
                    SiteId = _appOptions.ProposalManagementRootSiteId,
                    ListId = _appOptions.RoleMappingsListId
                };
                // Create Json object for SharePoint create list item
                dynamic itemFieldsJson = new JObject();
                itemFieldsJson.ADGroupName = adGroupName;
                itemFieldsJson.Role = "Administrator";
                itemFieldsJson.Permissions = @"[
                      {
                        'typeName': 'Permission',
                        'id': '',
                        'name': 'Administrator'
                      }
                    ]";

                dynamic itemJson = new JObject();
                itemJson.fields = itemFieldsJson;

                var result = await _graphSharePointAppService.CreateListItemAsync(siteList, itemJson.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - SetupService_CreateSiteAdminPermissionsAsync Service Exception: {ex}");
                throw;
            }
        }

        public async Task CreateAllListsAsync(string siteRootId, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - SetupService_CreateAllListsAsync called.");

            var sharepointLists = GetSharePointLists();
            var siteList = new SiteList();

            siteList.SiteId = _appOptions.ProposalManagementRootSiteId;
            if (string.IsNullOrEmpty(siteList.SiteId))
                siteList.SiteId = siteRootId;
            foreach (var list in sharepointLists)
            {
                try
                {
                    string htmlBody = string.Empty;
                    switch (list)
                    {
                        case ListSchema.CategoriesListId:
                            htmlBody = SharePointListsSchemaHelper.CategoriesJsonSchema(_appOptions.CategoriesListId);
                            await _graphSharePointAppService.CreateSiteListAsync(htmlBody);
                            break;
                        case ListSchema.IndustryListId:
                            htmlBody = SharePointListsSchemaHelper.IndustryJsonSchema(_appOptions.IndustryListId);
                            await _graphSharePointAppService.CreateSiteListAsync(htmlBody);
                            break;
                        case ListSchema.OpportunitiesListId:
                            htmlBody = SharePointListsSchemaHelper.OpportunitiesJsonSchema(_appOptions.OpportunitiesListId);
                            await _graphSharePointAppService.CreateSiteListAsync(htmlBody);
                            break;
                        case ListSchema.Permissions:
                            htmlBody = SharePointListsSchemaHelper.PermissionJsonSchema(_appOptions.Permissions);
                            await _graphSharePointAppService.CreateSiteListAsync(htmlBody);
                            break;
                        case ListSchema.ProcessListId:
                            htmlBody = SharePointListsSchemaHelper.WorkFlowItemsJsonSchema(_appOptions.ProcessListId);
                            await _graphSharePointAppService.CreateSiteListAsync(htmlBody);
                            break;
                        case ListSchema.RegionsListId:
                            htmlBody = SharePointListsSchemaHelper.RegionsJsonSchema(_appOptions.RegionsListId);
                            await _graphSharePointAppService.CreateSiteListAsync(htmlBody);
                            break;
                        case ListSchema.RoleListId:
                            htmlBody = SharePointListsSchemaHelper.RoleJsonSchema(_appOptions.RoleListId);
                            await _graphSharePointAppService.CreateSiteListAsync(htmlBody);
                            break;
                        case ListSchema.RoleMappingsListId:
                            htmlBody = SharePointListsSchemaHelper.RoleMappingsJsonSchema(_appOptions.RoleMappingsListId);
                            await _graphSharePointAppService.CreateSiteListAsync(htmlBody);
                            break;
                        case ListSchema.TemplateListId:
                            htmlBody = SharePointListsSchemaHelper.TemplatesJsonSchema(_appOptions.TemplateListId);
                            await _graphSharePointAppService.CreateSiteListAsync(htmlBody);
                            break;
                        case ListSchema.DashboardListId:
                            htmlBody = SharePointListsSchemaHelper.DashboardJsonSchema(_appOptions.DashboardListId);
                            await _graphSharePointAppService.CreateSiteListAsync(htmlBody);
                            break;
                        case ListSchema.TasksListId:
                            htmlBody = SharePointListsSchemaHelper.TasksJsonSchema(_appOptions.TasksListId);
                            await _graphSharePointAppService.CreateSiteListAsync(htmlBody);
                            break;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError($"RequestId: {requestId} - SetupService_CreateAllListsAsync error: {ex}");
                }
            }
        }

        public async Task CreateProposalManagerTeamAsync(string name, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - SetupService_CreateProposalManagerTeamAsync called.");

            try
            {
                await _graphTeamsAppService.CreateTeamAsync(name, name + "team");
                //get groupID
                bool check = true;
                dynamic jsonDyn = null;
                var opportunityName = WebUtility.UrlEncode(name);
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

                //get user Id
                string objectId = _userContext.User.FindFirst(AzureAdConstants.ObjectIdClaimType).Value;
                await _graphUserAppService.AddGroupMemberAsync(objectId, groupID, requestId);

                //Create channels
                await _graphTeamsAppService.CreateChannelAsync(groupID, "Configuration", "Configuration Channel");
                await _graphTeamsAppService.CreateChannelAsync(groupID, "Administration", "Administration Channel");

            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - SetupService_CreateProposalManagerTeamAsync error: {ex}");
                throw;
            }
        }

        public async Task CreateAdminGroupAsync(string name, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - SetupService_CreateAdminGroupAsync called.");

            try
            {
                await _graphTeamsAppService.CreateGroupAsync(name, name + " Group");
                //get groupID
                bool check = true;
                dynamic jsonDyn = null;
                var groupName = WebUtility.UrlEncode(name);
                var options = new List<QueryParam>();
                options.Add(new QueryParam("filter", $"startswith(displayName,'{groupName}')"));
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

                //get user Id
                string objectId = _userContext.User.FindFirst(AzureAdConstants.ObjectIdClaimType).Value;
                await _graphUserAppService.AddGroupMemberAsync(objectId, groupID, requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - SetupService_CreateAdminGroupAsync error: {ex}");
                throw;
            }
        }

        public async Task<String> GetAppId(string name, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - SetupService_GetAppId called.");

            //get groupID
            bool check = true;
            dynamic jsonDyn = null;
            var groupName = WebUtility.UrlEncode(name);
            var options = new List<QueryParam>();
            options.Add(new QueryParam("filter", $"startswith(displayName,'{groupName}')"));
            while (check)
            {
                var groupIdJson = await _graphUserAppService.GetGroupAsync(options, "");
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
            var respose = await _graphTeamsAppService.GetAppIdAsync(groupID);
            return respose;
        }

        private List<ListSchema> GetSharePointLists(string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - SetupService_GetSharePointLists called.");

            List<ListSchema> sharepointLists = new List<ListSchema>();
            sharepointLists.Add(ListSchema.CategoriesListId);
            sharepointLists.Add(ListSchema.IndustryListId);
            sharepointLists.Add(ListSchema.OpportunitiesListId);
            sharepointLists.Add(ListSchema.ProcessListId);
            sharepointLists.Add(ListSchema.RegionsListId);
            sharepointLists.Add(ListSchema.RoleListId);
            sharepointLists.Add(ListSchema.RoleMappingsListId);
            sharepointLists.Add(ListSchema.TemplateListId);
            sharepointLists.Add(ListSchema.Permissions);
            sharepointLists.Add(ListSchema.DashboardListId);
            sharepointLists.Add(ListSchema.TasksListId);
            return sharepointLists;
        }

        private List<string> getPermissions(string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - SetupService_getPermissions called.");

            List<string> permissions = new List<string>();
            permissions.Add("Opportunity_Create");
            permissions.Add("Opportunity_Read_All");
            permissions.Add("Opportunity_ReadWrite_All");
            permissions.Add("Opportunity_Read_Partial");
            permissions.Add("Opportunity_ReadWrite_Partial");
            permissions.Add("Opportunities_Read_All");
            permissions.Add("Opportunities_ReadWrite_All");
            permissions.Add("Opportunity_ReadWrite_Team");
            permissions.Add("Opportunity_ReadWrite_Dealtype");
            permissions.Add("Administrator");
            permissions.Add("CustomerDecision_Read");
            permissions.Add("CustomerDecision_ReadWrite");
            permissions.Add("CreditCheck_Read");
            permissions.Add("CreditCheck_ReadWrite");
            permissions.Add("Compliance_Read");
            permissions.Add("Compliance_ReadWrite");
            permissions.Add("ProposalDocument_Read");
            permissions.Add("ProposalDocument_ReadWrite");
            permissions.Add("RiskAssessment_Read");
            permissions.Add("RiskAssessment_ReadWrite");
            permissions.Add("Underwriting_Read");
            permissions.Add("Underwriting_ReadWrite");
            return permissions;
        }

        private List<string> getRoles(string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - SetupService_getRoles called.");

            List<string> roles = new List<string>();
            roles.Add("LoanOfficer");
            roles.Add("RelationshipManager");
            roles.Add("Administrator");
            roles.Add("CreditCheck");
            roles.Add("LegalCounsel");
            roles.Add("RiskAssessment");
            roles.Add("HumanResources");
            roles.Add("Compliance");
            roles.Add("Underwriting");
            return roles;
        }
        private List<string> getTasks(string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - SetupService_getTasks called.");

            List<string> tasks = new List<string>();
            tasks.Add("Approval");
            tasks.Add("Content");
            tasks.Add("Unassigned");
            return tasks;
        }

        private List<ProcessesType> getProcesses(string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - SetupService_getProcesses called.");

            List<ProcessesType> processesTypes = new List<ProcessesType>();
            //Start Process 
            ProcessesType startProcess = new ProcessesType();
            startProcess.ProcessType = "Base";
            startProcess.Channel = "None";
            startProcess.ProcessStep = "Start Process";
            processesTypes.Add(startProcess);
            //New Opportunity 
            ProcessesType newOpportunity = new ProcessesType();
            newOpportunity.ProcessType = "Base";
            newOpportunity.Channel = "None";
            newOpportunity.ProcessStep = "New Opportunity";
            processesTypes.Add(newOpportunity);
            //Customer Descision
            ProcessesType customerDecision = new ProcessesType();
            customerDecision.Channel = "CustomerDecision";
            customerDecision.ProcessType = "customerDecisionTab";
            customerDecision.ProcessStep = "Draft Proposal";
            processesTypes.Add(customerDecision);
            //Formal Proposal
            ProcessesType formalProposal = new ProcessesType();
            formalProposal.Channel = "Formal Proposal";
            formalProposal.ProcessType = "ProposalStatusTab";
            formalProposal.ProcessStep = "None";
            processesTypes.Add(formalProposal);
            //Credit Check
            ProcessesType creditCheck = new ProcessesType();
            creditCheck.Channel = "Credit Check";
            creditCheck.ProcessType = "CheckListTab";
            creditCheck.ProcessStep = "CreditCheck";
            processesTypes.Add(creditCheck);
            //Compliance
            ProcessesType compliance = new ProcessesType();
            compliance.Channel = "Compliance";
            compliance.ProcessType = "CheckListTab";
            compliance.ProcessStep = "Compliance";
            processesTypes.Add(compliance);
            //UnderWriting
            ProcessesType underWriting = new ProcessesType();
            underWriting.Channel = "Underwriting";
            underWriting.ProcessType = "CheckListTab";
            underWriting.ProcessStep = "Underwriting";
            processesTypes.Add(underWriting);
            //Risk Assessment
            ProcessesType riskAssessment = new ProcessesType();
            riskAssessment.Channel = "Risk Assessment";
            riskAssessment.ProcessType = "CheckListTab";
            riskAssessment.ProcessStep = "RiskAssessment";
            processesTypes.Add(riskAssessment);

            return processesTypes;
        }
    }
}
