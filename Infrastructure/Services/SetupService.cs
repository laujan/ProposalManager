// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using ApplicationCore;
using ApplicationCore.Entities;
using ApplicationCore.Entities.GraphServices;
using ApplicationCore.Helpers;
using ApplicationCore.Interfaces;
using Infrastructure.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using System.Xml;

namespace Infrastructure.Services
{
    public class SetupService : BaseService<SetupService>, ISetupService
    {
        private readonly GraphSharePointAppService _graphSharePointAppService;
        private readonly IWritableOptions<AppOptions> _writableOptions;
        private readonly IWritableOptions<DocumentIdActivatorConfiguration> documentIdActivatorConfigurationWritableOptions;
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
            _graphTeamsAppService = graphTeamsAppService;
            _graphUserAppService = graphUserAppService;
            _userContext = userContext;
            _azureKeyVaultService = azureKeyVaultService;
        }

        public async Task<StatusCodes> UpdateAppOptionsAsync(string key, string value, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - SetupService_UpdateAppOpptionsAsync called.");
            // Update ProposalManagementRootSiteId if empty
            if (string.IsNullOrWhiteSpace(_writableOptions.Value.ProposalManagementRootSiteId))
            {
                _writableOptions.UpdateAsync(nameof(_writableOptions.Value.ProposalManagementRootSiteId), await _graphSharePointAppService.GetSharePointRootId(), requestId);
            }

            _writableOptions.UpdateAsync(key, value, requestId);
            return StatusCodes.Status200OK;
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
                        itemFieldsJson.RoleName = process.RoleName;
                        itemFieldsJson.RoleId = process.RoleId;
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

        public async Task CreateSiteAdminPermissionsAsync(string adGroupName,string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - SetupService_CreateSiteAdminPermissionsAsync called.");
            try
            {
                var siteList = new SiteList
                {
                    SiteId = _appOptions.ProposalManagementRootSiteId,
                    ListId = _appOptions.RoleListId
                };
                // Create Json object for SharePoint create list item
                dynamic itemFieldsJson = new JObject();
                itemFieldsJson.AdGroupName = adGroupName;
                itemFieldsJson.Role = "Administrator";
                itemFieldsJson.Permissions = @"[
                      {
                        'typeName': 'Permission',
                        'id': '',
                        'name': 'Administrator'
                      }
                    ]";
                itemFieldsJson.TeamsMembership = "Owner";
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

        public async Task CreateAllListsAsync(string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - SetupService_CreateAllListsAsync called.");

            var sharepointLists = GetSharePointLists();

            var siteRootId = await _graphSharePointAppService.GetSharePointRootId();

            foreach (var list in sharepointLists)
            {
                try
                {
                    string htmlBody = string.Empty;
                    switch (list)
                    {
                        case ListSchema.OpportunitiesListId:
                            htmlBody = SharePointListsSchemaHelper.OpportunitiesJsonSchema(_appOptions.OpportunitiesListId);
                            break;
                        case ListSchema.Permissions:
                            htmlBody = SharePointListsSchemaHelper.PermissionJsonSchema(_appOptions.Permissions);
                            break;
                        case ListSchema.ProcessListId:
                            htmlBody = SharePointListsSchemaHelper.WorkFlowItemsJsonSchema(_appOptions.ProcessListId);
                            break;
                        case ListSchema.OpportunityMetaDataId:
                            htmlBody = SharePointListsSchemaHelper.OpportunityMetaDataJsonSchema(_appOptions.OpportunityMetaDataId);
                            break;
                        case ListSchema.RoleListId:
                            htmlBody = SharePointListsSchemaHelper.RoleJsonSchema(_appOptions.RoleListId);
                            break;
                        case ListSchema.GroupsListId:
                            htmlBody = SharePointListsSchemaHelper.GroupJsonSchema(_appOptions.GroupsListId);
                            break;
                        case ListSchema.TemplateListId:
                            htmlBody = SharePointListsSchemaHelper.TemplatesJsonSchema(_appOptions.TemplateListId);
                            break;
                        case ListSchema.DashboardListId:
                            htmlBody = SharePointListsSchemaHelper.DashboardJsonSchema(_appOptions.DashboardListId);
                            break;
                        case ListSchema.TasksListId:
                            htmlBody = SharePointListsSchemaHelper.TasksJsonSchema(_appOptions.TasksListId);
                            break;
                        case ListSchema.AuditListId:
                            htmlBody = SharePointListsSchemaHelper.AuditJsonSchema("Audit");
                            break;
                    }
                    await _graphSharePointAppService.CreateSiteListAsync(htmlBody, siteRootId);
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
                _logger.LogInformation($"RequestId: {requestId} - SetupService_CreateProposalManagerTeamAsync called.");

                try
                {
                    var response = await _graphTeamsAppService.GetGroupsIdAsync($"startswith(displayName,'{_appOptions.GeneralProposalManagementTeam.ToString()}')");
                    var groupID = JObject.Parse(response.ToString()).SelectToken("value")[0].SelectToken("id").ToString();

                    //Create channels
                    await _graphTeamsAppService.CreateChannelAsync(groupID, "Configuration", "Configuration Channel");
                    await _graphTeamsAppService.CreateChannelAsync(groupID, "Administration", "Administration Channel");
                    await _graphTeamsAppService.CreateChannelAsync(groupID, "Help", "Help Channel");
                }
                catch (Exception ex)
                {
                    _logger.LogError($"RequestId: {requestId} - SetupService_CreateProposalManagerTeamAsync error: {ex}");
                    throw;
                }

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
            sharepointLists.Add(ListSchema.OpportunityMetaDataId);
            sharepointLists.Add(ListSchema.GroupsListId);
            sharepointLists.Add(ListSchema.OpportunitiesListId);
            sharepointLists.Add(ListSchema.ProcessListId);
            sharepointLists.Add(ListSchema.RoleListId);
            sharepointLists.Add(ListSchema.TemplateListId);
            sharepointLists.Add(ListSchema.Permissions);
            sharepointLists.Add(ListSchema.DashboardListId);
            sharepointLists.Add(ListSchema.TasksListId);
            sharepointLists.Add(ListSchema.AuditListId);

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
            permissions.Add("ProposalDocument_Read");
            permissions.Add("ProposalDocument_ReadWrite");
            return permissions;
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
            startProcess.RoleId = "";
            startProcess.RoleName = "";
            processesTypes.Add(startProcess);

            //Customer Descision
            ProcessesType customerDecision = new ProcessesType();
            customerDecision.Channel = "Customer Decision";
            customerDecision.ProcessType = "customerDecisionTab";
            customerDecision.ProcessStep = "Customer Decision";
            customerDecision.RoleId = "";
            customerDecision.RoleName = "";
            processesTypes.Add(customerDecision);

            //Formal Proposal
            ProcessesType formalProposal = new ProcessesType();
            formalProposal.Channel = "Formal Proposal";
            formalProposal.ProcessType = "proposalStatusTab";
            formalProposal.ProcessStep = "Formal Proposal";
            formalProposal.RoleId = "";
            formalProposal.RoleName = "";
            processesTypes.Add(formalProposal);

            return processesTypes;
        }

        public async Task CreateMetaDataList(string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - SetupService_CreateMetaDataList called.");
            try
            {
                var siteList = new SiteList
                {
                    SiteId = _appOptions.ProposalManagementRootSiteId,
                    ListId = _appOptions.OpportunityMetaDataId
                };
                var metaDataList = getMetaData();
                foreach (var entity in metaDataList)
                {
                    try
                    {
                        // Create Json object for SharePoint create list item
                        dynamic itemFieldsJson = new JObject();
                        itemFieldsJson.FieldName = entity.DisplayName;
                        itemFieldsJson.FieldType = entity.FieldType.Name.ToString();
                        itemFieldsJson.FieldValue = entity.Values;
                        itemFieldsJson.FieldScreen = entity.Screen;
                        itemFieldsJson.FieldRequired = entity.Required.ToString();
                        itemFieldsJson.FieldUniqueId = entity.UniqueId;

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

        private List<MetaData> getMetaData(string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - SetupService_getProcesses called.");

            List<MetaData> metaDataList = new List<MetaData>();
            metaDataList.Add(new MetaData
            {
                DisplayName = "Customer",
                FieldType = FieldType.String,
                Values = "",
                Screen = "Screen1",
                Required = true,
                UniqueId = "customer"
            });

            metaDataList.Add(new MetaData
            {
                DisplayName = "Opportunity",
                FieldType = FieldType.String,
                Values = "",
                Screen = "Screen1",
                Required = true,
                UniqueId = "opportunity"
            });

            metaDataList.Add(new MetaData
            {
                DisplayName = "Opened Date",
                FieldType = FieldType.Date,
                Values = "",
                Screen = "Screen1",
                Required = true,
                UniqueId = "openeddate"
            });

            metaDataList.Add(new MetaData
            {
                DisplayName = "Target Date",
                FieldType = FieldType.Date,
                Values = "",
                Screen = "Screen1",
                Required = true,
                UniqueId = "targetdate"
            });

            metaDataList.Add(new MetaData
            {
                DisplayName = "Deal Size",
                FieldType = FieldType.Double,
                Values = "0",
                Screen = "Screen1",
                Required = false,
                UniqueId = "dealsize"
            });

            metaDataList.Add(new MetaData
            {
                DisplayName = "Annual Revenue",
                FieldType = FieldType.Double,
                Values = "0",
                Screen = "Screen1",
                Required = false,
                UniqueId = "annualrevenue"
            });

            metaDataList.Add(new MetaData
            {
                DisplayName = "Industry",
                FieldType = FieldType.DropDown,
                Values = @"[
                            {
                            'typeName': 'DropDownMetaDataValue',
                            'id': '1',
                            'name': 'Industry 1'
                            },{
                            'typeName': 'DropDownMetaDataValue',
                            'id': '2',
                            'name': 'Industry 2'
                            }]",
                Screen = "Screen1",
                Required = false,
                UniqueId = "industry"
            });

            metaDataList.Add(new MetaData
            {
                DisplayName = "Region",
                FieldType = FieldType.DropDown,
                Values = @"[
                            {
                            'typeName': 'DropDownMetaDataValue',
                            'id': '1',
                            'name': 'Region 1'
                            },{
                            'typeName': 'DropDownMetaDataValue',
                            'id': '2',
                            'name': 'Region 2'
                            }]",
                Screen = "Screen1",
                Required = false,
                UniqueId = "region"
            });

            metaDataList.Add(new MetaData
            {
                DisplayName = "Notes",
                FieldType = FieldType.String,
                Values = "",
                Screen = "Screen1",
                Required = false,
                UniqueId = "notes"
            });

            metaDataList.Add(new MetaData
            {
                DisplayName = "Category",
                FieldType = FieldType.DropDown,
                Values = @"[
                            {
                            'typeName': 'DropDownMetaDataValue',
                            'id': '1',
                            'name': 'Category 1'
                            },{
                            'typeName': 'DropDownMetaDataValue',
                            'id': '2',
                            'name': 'Category 2'
                            }]",
                Screen = "Screen2",
                Required = false,
                UniqueId = "category"
            });

            //Screen3

            metaDataList.Add(new MetaData
            {
                DisplayName = "Margin",
                FieldType = FieldType.Double,
                Values = "0",
                Screen = "Screen3",
                Required = false,
                UniqueId = "margin"
            });

            metaDataList.Add(new MetaData
            {
                DisplayName = "Debt Ratio",
                FieldType = FieldType.Double,
                Values = "0",
                Screen = "Screen3",
                Required = false,
                UniqueId = "debtratio"
            });

            metaDataList.Add(new MetaData
            {
                DisplayName = "Rate",
                FieldType = FieldType.Double,
                Values = "0",
                Screen = "Screen3",
                Required = false,
                UniqueId = "rate"
            });

            metaDataList.Add(new MetaData
            {
                DisplayName = "Purpose",
                FieldType = FieldType.String,
                Values = "",
                Screen = "Screen3",
                Required = false,
                UniqueId = "purpose"
            });

            metaDataList.Add(new MetaData
            {
                DisplayName = "Disbursement Schedule",
                FieldType = FieldType.String,
                Values = "",
                Screen = "Screen3",
                Required = false,
                UniqueId = "disbursementschedule"
            });

            metaDataList.Add(new MetaData
            {
                DisplayName = "Collateral Amount",
                FieldType = FieldType.Double,
                Values = "0",
                Screen = "Screen3",
                Required = false,
                UniqueId = "collateralamount"
            });

            metaDataList.Add(new MetaData
            {
                DisplayName = "Guarantees",
                FieldType = FieldType.Double,
                Values = "0",
                Screen = "Screen3",
                Required = false,
                UniqueId = "guarantees"
            });

            return metaDataList;
        }

        public async Task CreateDefaultBusinessProcess(string requestId)
        {

            _logger.LogInformation($"RequestId: {requestId} - SetupService_CreateDefaultBusinessProcess called.");
            try
            {
                var siteList = new SiteList
                {
                    SiteId = _appOptions.ProposalManagementRootSiteId,
                    ListId = _appOptions.TemplateListId
                };

                try
                {
                    // Create Json object for SharePoint create list item
                    dynamic templateFieldsJson = new JObject();
                    templateFieldsJson.TemplateName = "Default Business Process";
                    templateFieldsJson.Description = "Default Business Process";
                    templateFieldsJson.LastUsed = DateTimeOffset.Now.Date;
                    templateFieldsJson.CreatedBy = "";
                    templateFieldsJson.ProcessList = @"[{
                                                        'typeName': 'ProcessesType',
                                                        'id': '',
                                                        'processStep': 'Start Process',
                                                        'channel': 'None',
                                                        'processType': 'Base',
                                                        'order': '1.1',
                                                        'roleName': '',
                                                        'daysEstimate': '0',
                                                        'roleId': '',
                                                        'status': 0,
                                                        'processnumber': 0,
                                                        'groupnumber': 0
                                                      }]";
                    templateFieldsJson.DefaultTemplate = "True";

                    dynamic templateJson = new JObject();
                    templateJson.fields = templateFieldsJson;

                    _logger.LogDebug($"RequestId: {requestId} - SetupService_CreateSiteProcessesAsync debug result: {templateJson}");

                    var result = await _graphSharePointAppService.CreateListItemAsync(siteList, templateJson.ToString(), requestId);

                    _logger.LogDebug($"RequestId: {requestId} - SetupService_CreateSiteProcessesAsync debug result: {result}");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning($"RequestId: {requestId} - SetupService_CreateSiteProcessesAsync warning: {ex}");
                }

            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - SetupService_CreateSiteProcessesAsync Service Exception: {ex}");
                throw;
            }
        }
    }
}
