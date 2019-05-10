// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ApplicationCore.Artifacts;
using ApplicationCore.Interfaces;
using ApplicationCore.Entities;
using ApplicationCore.Services;
using ApplicationCore;
using ApplicationCore.Helpers;
using ApplicationCore.Authorization;
using ApplicationCore.Entities.GraphServices;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using ApplicationCore.Helpers.Exceptions;
using System.Linq;

namespace Infrastructure.Services
{
    public class OpportunityRepository : BaseArtifactFactory<Opportunity>, IOpportunityRepository
    {
        private readonly IOpportunityFactory _opportunityFactory;
        private readonly GraphSharePointAppService _graphSharePointAppService;
        private readonly GraphUserAppService _graphUserAppService;
        private readonly IUserProfileRepository _userProfileRepository;
        private readonly IUserContext _userContext;
        private readonly IDashboardService _dashboardService;
        private readonly IAuthorizationService _authorizationService;
        private readonly IPermissionRepository _permissionRepository;
        private readonly ITemplateRepository _templateRepository;
        public OpportunityRepository(
            ILogger<OpportunityRepository> logger,
            IOptionsMonitor<AppOptions> appOptions,
            GraphSharePointAppService graphSharePointAppService,
            GraphUserAppService graphUserAppService,
            IUserProfileRepository userProfileRepository,
            IUserContext userContext,
            IOpportunityFactory opportunityFactory,
            IAuthorizationService authorizationService,
            IPermissionRepository permissionRepository,
            ITemplateRepository templateRepository,
            IDashboardService dashboardService) : base(logger, appOptions)
        {
            Guard.Against.Null(graphSharePointAppService, nameof(graphSharePointAppService));
            Guard.Against.Null(graphUserAppService, nameof(graphUserAppService));
            Guard.Against.Null(userProfileRepository, nameof(userProfileRepository));
            Guard.Against.Null(userContext, nameof(userContext));
            Guard.Against.Null(opportunityFactory, nameof(opportunityFactory));
            Guard.Against.Null(dashboardService, nameof(dashboardService));
            Guard.Against.Null(authorizationService, nameof(authorizationService));
            Guard.Against.Null(permissionRepository, nameof(permissionRepository));

            _graphSharePointAppService = graphSharePointAppService;
            _graphUserAppService = graphUserAppService;
            _userProfileRepository = userProfileRepository;
            _userContext = userContext;
            _opportunityFactory = opportunityFactory;
            _dashboardService = dashboardService;
            _authorizationService = authorizationService;
            _permissionRepository = permissionRepository;
            _templateRepository = templateRepository;
        }

        public async Task<StatusCodes> CreateItemAsync(Opportunity opportunity, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - OpportunityRepository_CreateItemAsync called.");

            try
            {
                Guard.Against.Null(opportunity, nameof(opportunity), requestId);
                Guard.Against.NullOrEmpty(opportunity.DisplayName, nameof(opportunity.DisplayName), requestId);

                // Set initial opportunity state
                opportunity.Metadata.OpportunityState = OpportunityState.Creating;

                //add default business process, since this is coming from API (outside app)
                if (opportunity.Content.Template == null)
                {
                    opportunity.Content.Template = (await _templateRepository.GetAllAsync(requestId)).ToList().Find(x => x.DefaultTemplate);
                }

                //Granular Access : Start
                if (StatusCodes.Status401Unauthorized == await _authorizationService.CheckAccessFactoryAsync(PermissionNeededTo.Create, requestId)) return StatusCodes.Status401Unauthorized;
                //Granular Access : End
                // Ensure id is blank since it will be set by SharePoint
                opportunity.Id = String.Empty;

                opportunity = await _opportunityFactory.CreateWorkflowAsync(opportunity, requestId);

                _logger.LogInformation($"RequestId: {requestId} - OpportunityRepository_CreateItemAsync creating SharePoint List for opportunity.");

                // Create Json object for SharePoint create list item
                dynamic opportunityFieldsJson = new JObject();
                opportunityFieldsJson.Name = opportunity.DisplayName;
                opportunityFieldsJson.OpportunityState = opportunity.Metadata.OpportunityState.Name;
                try
                {
                    opportunityFieldsJson.OpportunityObject = JsonConvert.SerializeObject(opportunity, Formatting.Indented);
                    //TODO
                    opportunityFieldsJson.TemplateLoaded = opportunity.TemplateLoaded.ToString();
                }
                catch(Exception ex)
                {
                    _logger.LogError($"RequestId: {requestId} - OpportunityRepository_CreateItemAsync create dashboard entry Exception: {ex}");
                }
                opportunityFieldsJson.Reference = opportunity.Reference ?? String.Empty;

                dynamic opportunityJson = new JObject();
                opportunityJson.fields = opportunityFieldsJson;

                var opportunitySiteList = new SiteList
                {
                    SiteId = _appOptions.ProposalManagementRootSiteId,
                    ListId = _appOptions.OpportunitiesListId
                };

                await _graphSharePointAppService.CreateListItemAsync(opportunitySiteList, opportunityJson.ToString(), requestId);

                //DashBoard Create call End.
                _logger.LogInformation($"RequestId: {requestId} - OpportunityRepository_CreateItemAsync finished creating SharePoint List for opportunity.");

                return StatusCodes.Status201Created;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - OpportunityRepository_CreateItemAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - OpportunityRepository_CreateItemAsync Service Exception: {ex}");
            }
        }

        public async Task<StatusCodes> UpdateItemAsync(Opportunity opportunity, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - OpportunityRepository_UpdateItemAsync called.");
            Guard.Against.Null(opportunity, nameof(opportunity), requestId);
            Guard.Against.NullOrEmpty(opportunity.Id, nameof(opportunity.Id), requestId);

            try
            {
                // TODO: This section will be replaced with a workflow
                _logger.LogInformation($"RequestId: {requestId} - OpportunityRepository_UpdateItemAsync SharePoint List for opportunity.");

                //Granular Access : Start
                var access = await CheckAccessAsync(PermissionNeededTo.WritePartial, PermissionNeededTo.Write, PermissionNeededTo.WriteAll, requestId);
                var currentUser = (_userContext.User.Claims).ToList().Find(x => x.Type == "preferred_username")?.Value;
                if (!access.haveSuperAcess && !access.haveAccess && !access.havePartial)
                {
                    // This user is not having any write permissions, so he won't be able to update
                    _logger.LogError($"RequestId: {requestId} - OpportunityRepository_GetItemByIdAsync current user: {currentUser} AccessDeniedException");
                    throw new AccessDeniedException($"RequestId: {requestId} - OpportunityRepository_GetItemByIdAsync current user: {currentUser} AccessDeniedException");
                }
                else if (!access.haveSuperAcess)
                {
                    if (!(opportunity.Content.TeamMembers).ToList().Any
                            (teamMember => teamMember.Fields.UserPrincipalName == currentUser))
                    {
                        // This user is not having any write permissions, so he won't be able to update
                        _logger.LogError($"RequestId: {requestId} - OpportunityRepository_GetItemByIdAsync current user: {currentUser} AccessDeniedException");
                        throw new AccessDeniedException($"RequestId: {requestId} - OpportunityRepository_GetItemByIdAsync current user: {currentUser} AccessDeniedException");
                    }
                }
                //Granular Access : End

                // Workflow processor
                opportunity = await _opportunityFactory.UpdateWorkflowAsync(opportunity, requestId);


                var opportunityJObject = JObject.FromObject(opportunity);

                // Create Json object for SharePoint create list item
                dynamic opportunityJson = new JObject();
                opportunityJson.OpportunityId = opportunity.Id;
                opportunityJson.OpportunityState = opportunity.Metadata.OpportunityState.Name;
                opportunityJson.OpportunityObject = JsonConvert.SerializeObject(opportunity, Formatting.Indented);
                opportunityJson.Reference = opportunity.Reference ?? String.Empty;

                //TODO...
                try
                {
                    if (opportunity.Content.Template.ProcessList.Count > 1 && !opportunity.TemplateLoaded)
                    {
                        opportunityJson.TemplateLoaded = "True";
                    }else opportunityJson.TemplateLoaded = opportunity.TemplateLoaded.ToString();

                }
                catch
                {
                    opportunityJson.TemplateLoaded = opportunity.TemplateLoaded.ToString();
                }

                var opportunitySiteList = new SiteList
                {
                    SiteId = _appOptions.ProposalManagementRootSiteId,
                    ListId = _appOptions.OpportunitiesListId
                };

                await _graphSharePointAppService.UpdateListItemAsync(opportunitySiteList, opportunity.Id, opportunityJson.ToString(), requestId);

                _logger.LogInformation($"RequestId: {requestId} - OpportunityRepository_UpdateItemAsync finished SharePoint List for opportunity.");
                //For DashBoard---
                return StatusCodes.Status200OK;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - OpportunityRepository_UpdateItemAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - OpportunityRepository_UpdateItemAsync Service Exception: {ex}");
            }
        }

        public async Task<Opportunity> GetItemByIdAsync(string id, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - OpportunityRepository_GetItemByIdAsync called.");

            try
            {
                Guard.Against.NullOrEmpty(id, nameof(id), requestId);

                var opportunitySiteList = new SiteList
                {
                    SiteId = _appOptions.ProposalManagementRootSiteId,
                    ListId = _appOptions.OpportunitiesListId
                };

                //Granular Access : Start
                var access = await CheckAccessAsync(PermissionNeededTo.ReadPartial, PermissionNeededTo.Read, PermissionNeededTo.ReadAll, requestId);
                var currentUser = (_userContext.User.Claims).ToList().Find(x => x.Type == "preferred_username")?.Value;
                if (!access.haveSuperAcess && !access.haveAccess && !access.havePartial)
                {
                    // This user is not having any write permissions, so he won't be able to update
                    _logger.LogError($"RequestId: {requestId} - OpportunityRepository_GetItemByIdAsync current user: {currentUser} AccessDeniedException");
                    throw new AccessDeniedException($"RequestId: {requestId} - OpportunityRepository_GetItemByIdAsync current user: {currentUser} AccessDeniedException");
                }
                //Granular Access : End

                var json = await _graphSharePointAppService.GetListItemByIdAsync(opportunitySiteList, id, "all", requestId);
                Guard.Against.Null(json, nameof(json), requestId);

                var obj = JObject.Parse(json.ToString()).SelectToken("fields");

                var oppArtifact = JsonConvert.DeserializeObject<Opportunity>(obj.SelectToken("OpportunityObject").ToString(), new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                });

                //Granular Access : Start
                if (!access.haveSuperAcess)
                {
                    if (!(oppArtifact.Content.TeamMembers).ToList().Any
                            (teamMember => teamMember.Fields.UserPrincipalName == currentUser))
                    {
                        // This user is not having any write permissions, so he won't be able to update
                        _logger.LogError($"RequestId: {requestId} - OpportunityRepository_GetItemByIdAsync current user: {currentUser} AccessDeniedException");
                        throw new AccessDeniedException($"RequestId: {requestId} - OpportunityRepository_GetItemByIdAsync current user: {currentUser} AccessDeniedException");
                    }
                }
    
                oppArtifact.Id = obj.SelectToken("id")?.ToString();
                oppArtifact.TemplateLoaded = obj.SelectToken("TemplateLoaded") != null ? obj.SelectToken("TemplateLoaded").ToString() == "True" : false;
                return oppArtifact;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - OpportunityRepository_GetItemByIdAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - OpportunityRepository_GetItemByIdAsync Service Exception: {ex}");
            }
        }

        public async Task<Opportunity> GetItemByNameAsync(string name, bool isCheckName, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - OpportunityRepository_GetItemByNameAsync called.");

            try
            {
                Guard.Against.NullOrEmpty(name, nameof(name), requestId);

                //Granular Access : Start
                var access = await CheckAccessAsync(PermissionNeededTo.ReadPartial,PermissionNeededTo.Read, PermissionNeededTo.ReadAll, requestId);
                var currentUser = (_userContext.User.Claims).ToList().Find(x => x.Type == "preferred_username")?.Value;
                if (!access.haveSuperAcess && !access.haveAccess && !access.havePartial)
                {
                    // This user is not having any write permissions, so he won't be able to update
                    _logger.LogError($"RequestId: {requestId} - OpportunityRepository_GetItemByIdAsync current user: {currentUser} AccessDeniedException");
                    throw new AccessDeniedException($"RequestId: {requestId} - OpportunityRepository_GetItemByIdAsync current user: {currentUser} AccessDeniedException");
                }
                //Granular Access : End

                var opportunitySiteList = new SiteList
                {
                    SiteId = _appOptions.ProposalManagementRootSiteId,
                    ListId = _appOptions.OpportunitiesListId
                };

                name = name.Replace("'", "");
                var nameEncoded = WebUtility.UrlEncode(name);
                var options = new List<QueryParam>();
                options.Add(new QueryParam("filter", $"startswith(fields/Name,'{nameEncoded}')"));

                dynamic jsonDyn = await _graphSharePointAppService.GetListItemAsync(opportunitySiteList, options, "all", requestId);

                if (jsonDyn.value.HasValues)
                {
                    foreach (var item in jsonDyn.value)
                    {
                        var obj = JObject.Parse(item.ToString()).SelectToken("fields");

                        if (obj.SelectToken("Name").ToString() == name)
                        {
                            if (isCheckName)
                            {
                                // If just checking for name, rtunr empty opportunity and skip access check
                                var emptyOpportunity = Opportunity.Empty;
                                emptyOpportunity.DisplayName = name;
                                return emptyOpportunity;
                            }

                            var oppArtifact = JsonConvert.DeserializeObject<Opportunity>(obj.SelectToken("OpportunityObject").ToString(), new JsonSerializerSettings
                            {
                                MissingMemberHandling = MissingMemberHandling.Ignore,
                                NullValueHandling = NullValueHandling.Ignore
                            });

                            oppArtifact.Id = obj.SelectToken("id")?.ToString();
                            oppArtifact.TemplateLoaded = obj.SelectToken("TemplateLoaded") != null ? obj.SelectToken("TemplateLoaded").ToString() == "True" : false;

                            //Granular Access : Start
                            if (!access.haveSuperAcess)
                               {
                                   if (!CheckTeamMember(oppArtifact,currentUser))
                                   {
                                       // This user is not having any write permissions, so he won't be able to update
                                       _logger.LogError($"RequestId: {requestId} - OpportunityRepository_GetItemByIdAsync current user: {currentUser} AccessDeniedException");
                                       throw new AccessDeniedException($"RequestId: {requestId} - OpportunityRepository_GetItemByIdAsync current user: {currentUser} AccessDeniedException");
                                   }
                               }
                            //Granular Access : End
                            return oppArtifact;
                        }
                    }

                }

                // Not found
                _logger.LogError($"RequestId: {requestId} - OpportunityRepository_GetItemByNameAsync opportunity: {name} - Not found.");

                return Opportunity.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - OpportunityRepository_GetItemByNameAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - OpportunityRepository_GetItemByNameAsync Service Exception: {ex}");
            }
        }

        public async Task<Opportunity> GetItemByRefAsync(string reference, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - OpportunityRepository_GetItemByRefAsync called.");

            try
            {
                Guard.Against.NullOrEmpty(reference, nameof(reference), requestId);

                //Granular Access : Start
                var access = await CheckAccessAsync(PermissionNeededTo.ReadPartial,PermissionNeededTo.Read, PermissionNeededTo.ReadAll, requestId);
                var currentUser = (_userContext.User.Claims).ToList().Find(x => x.Type == "preferred_username")?.Value;
                if (!access.haveSuperAcess && !access.haveAccess && !access.havePartial)
                {
                    // This user is not having any write permissions, so he won't be able to update
                    _logger.LogError($"RequestId: {requestId} - OpportunityRepository_GetItemByIdAsync current user: {currentUser} AccessDeniedException");
                    throw new AccessDeniedException($"RequestId: {requestId} - OpportunityRepository_GetItemByIdAsync current user: {currentUser} AccessDeniedException");
                }
                //Granular Access : End

                var opportunitySiteList = new SiteList
                {
                    SiteId = _appOptions.ProposalManagementRootSiteId,
                    ListId = _appOptions.OpportunitiesListId
                };

                reference = reference.Replace("'", "");
                var nameEncoded = WebUtility.UrlEncode(reference);
                var options = new List<QueryParam>();
                options.Add(new QueryParam("filter", $"startswith(fields/Reference,'{nameEncoded}')"));

                dynamic json = await _graphSharePointAppService.GetListItemAsync(opportunitySiteList, options, "all", requestId);

                if (json.value.HasValues)
                {
                    foreach (var item in json.value)
                    {
                        var obj = JObject.Parse(item.ToString()).SelectToken("fields");

                        if (obj.SelectToken("Reference").ToString() == reference)
                        {

                            var oppArtifact = JsonConvert.DeserializeObject<Opportunity>(obj.SelectToken("OpportunityObject").ToString(), new JsonSerializerSettings
                            {
                                MissingMemberHandling = MissingMemberHandling.Ignore,
                                NullValueHandling = NullValueHandling.Ignore
                            });

                            oppArtifact.Id = obj.SelectToken("id")?.ToString();
                            oppArtifact.TemplateLoaded = obj.SelectToken("TemplateLoaded") != null ? obj.SelectToken("TemplateLoaded").ToString() == "True" : false;

                            //Granular Access : Start
                            if (!access.haveSuperAcess)
                            {
                                if (!CheckTeamMember(oppArtifact,currentUser))
                                {
                                    // This user is not having any write permissions, so he won't be able to update
                                    _logger.LogError($"RequestId: {requestId} - OpportunityRepository_GetItemByIdAsync current user: {currentUser} AccessDeniedException");
                                    throw new AccessDeniedException($"RequestId: {requestId} - OpportunityRepository_GetItemByIdAsync current user: {currentUser} AccessDeniedException");
                                }
                            }
                            //Granular Access : End

                            return oppArtifact;
                        }
                    }

                }

                // Not found
                _logger.LogError($"RequestId: {requestId} - OpportunityRepository_GetItemByRefAsync opportunity: {reference} - Not found.");

                return Opportunity.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - OpportunityRepository_GetItemByRefAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - OpportunityRepository_GetItemByRefAsync Service Exception: {ex}");
            }
        }

        public async Task<IList<Opportunity>> GetAllAsync(string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - OpportunityRepository_GetAllAsync called.");

            try
            {
                var siteList = new SiteList
                {
                    SiteId = _appOptions.ProposalManagementRootSiteId,
                    ListId = _appOptions.OpportunitiesListId
                };

                //Granular Access : Start
                var access = await CheckAccessAsync(PermissionNeededTo.ReadPartial,PermissionNeededTo.Read, PermissionNeededTo.ReadAll, requestId);
                var currentUser = (_userContext.User.Claims).ToList().Find(x => x.Type == "preferred_username")?.Value;
                //Granular Access : End
                var currentUserScope = (_userContext.User.Claims).ToList().Find(x => x.Type == "http://schemas.microsoft.com/identity/claims/scope")?.Value;
                Guard.Against.NullOrEmpty(currentUser, "OpportunityRepository_GetAllAsync CurrentUser null-empty", requestId);

                var callerUser = await _userProfileRepository.GetItemByUpnAsync(currentUser, requestId);
                Guard.Against.Null(callerUser, "_userProfileRepository.GetItemByUpnAsync Null", requestId);
                if (currentUser != callerUser.Fields.UserPrincipalName)
                {
                    _logger.LogError($"RequestId: {requestId} - OpportunityRepository_GetItemByIdAsync current user: {currentUser} AccessDeniedException");
                    throw new AccessDeniedException($"RequestId: {requestId} - OpportunityRepository_GetItemByIdAsync current user: {currentUser} AccessDeniedException");
                }

                //Granular Access : Start
                if (access.haveAccess == false && access.haveSuperAcess == false && access.havePartial == false)
                {
                    // This user is not having any read permissions, so he won't be able to list of opportunities
                    _logger.LogError($"RequestId: {requestId} - OpportunityRepository_GetItemByIdAsync current user: {currentUser} AccessDeniedException");
                    throw new AccessDeniedException($"RequestId: {requestId} - OpportunityRepository_GetItemByIdAsync current user: {currentUser} AccessDeniedException");
                }
                //Granular Access : End

                var isMember = false;
                var isOwner = false;


                if (callerUser.Fields.UserRoles.Find(x => x.TeamsMembership == TeamsMembership.Owner) != null)
                {
                    isOwner = true;
                }else if(callerUser.Fields.UserRoles.Find(x => x.TeamsMembership == TeamsMembership.Member) != null)
                {
                    isMember = true;
                }


                var itemsList = new List<Opportunity>();

                if (isOwner || isMember)
                {
                    dynamic json = await _graphSharePointAppService.GetListItemsAsync(siteList, "all", requestId);

                    if (json.value.HasValues)
                    {
                        foreach (var item in (JArray)json["value"])
                        {
                            var obj = JObject.Parse(item.ToString()).SelectToken("fields");

                            var oppArtifact = JsonConvert.DeserializeObject<Opportunity>(obj.SelectToken("OpportunityObject").ToString(), new JsonSerializerSettings
                            {
                                MissingMemberHandling = MissingMemberHandling.Ignore,
                                NullValueHandling = NullValueHandling.Ignore
                            });

                            oppArtifact.Id = obj.SelectToken("id")?.ToString();
                            oppArtifact.TemplateLoaded = obj.SelectToken("TemplateLoaded") != null ? obj.SelectToken("TemplateLoaded").ToString() == "True" : false;


                            //Granular Access : Start
                            if (access.haveSuperAcess || isOwner)
                                itemsList.Add(oppArtifact);
                            else
                            {
                                if ((oppArtifact.Content.TeamMembers).ToList().Any
                                    (teamMember => teamMember.Fields.UserPrincipalName == currentUser))
                                    itemsList.Add(oppArtifact);
                            }
                            //Granular Access : end
                        }
                    }
                }

                return itemsList;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - OpportunityRepository_GetAllAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - OpportunityRepository_GetAllAsync Service Exception: {ex}");
            }
        }

        public async Task<StatusCodes> DeleteItemAsync(string id, string requestId = "")
        {
            try
            {
                Guard.Against.Null(id, nameof(id), requestId);

                //Granular Access : Start
                var access = await CheckAccessAsync(PermissionNeededTo.WritePartial ,PermissionNeededTo.Write, PermissionNeededTo.WriteAll, requestId);
                var currentUser = (_userContext.User.Claims).ToList().Find(x => x.Type == "preferred_username")?.Value;
                if (!access.haveAccess && !access.haveSuperAcess)
                {
                    // This user is not having any write permissions, so he won't be able to update
                    _logger.LogError($"RequestId: {requestId} - OpportunityRepository_GetItemByIdAsync current user: {currentUser} AccessDeniedException");
                    throw new AccessDeniedException($"RequestId: {requestId} - OpportunityRepository_GetItemByIdAsync current user: {currentUser} AccessDeniedException");
                }

                //Granular Access : End	

                var opportunitySiteList = new SiteList
                {
                    SiteId = _appOptions.ProposalManagementRootSiteId,
                    ListId = _appOptions.OpportunitiesListId
                };

                var opportunity = await _graphSharePointAppService.GetListItemByIdAsync(opportunitySiteList, id, "all", requestId);
                Guard.Against.Null(opportunity, $"OpportunityRepository_y_DeleteItemsAsync getItemsById: {id}", requestId);

                var opportunityJson = opportunity["fields"]["OpportunityObject"].ToString();

                var oppArtifact = JsonConvert.DeserializeObject<Opportunity>(opportunityJson.ToString(), new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                    NullValueHandling = NullValueHandling.Ignore
                });

                var roles = new List<Role>();
                roles.Add(new Role { DisplayName = "RelationshipManager" });

                //Granular Access : Start
                if (!access.haveSuperAcess)
                {
                    if (!(oppArtifact.Content.TeamMembers).ToList().Any
                            (teamMember => teamMember.Fields.UserPrincipalName == currentUser))
                    {
                        // This user is not having any write permissions, so he won't be able to update
                        _logger.LogError($"RequestId: {requestId} - OpportunityRepository_GetItemByIdAsync current user: {currentUser} AccessDeniedException");
                        throw new AccessDeniedException($"RequestId: {requestId} - OpportunityRepository_GetItemByIdAsync current user: {currentUser} AccessDeniedException");
                    }
                }
                //Granular Access : End

                if (oppArtifact.Metadata.OpportunityState == OpportunityState.Creating)
                {
                    var result = await _graphSharePointAppService.DeleteFileOrFolderAsync(_appOptions.ProposalManagementRootSiteId, $"TempFolder/{oppArtifact.DisplayName}", requestId);
                    // TODO: Check response
                }

                var json = await _graphSharePointAppService.DeleteListItemAsync(opportunitySiteList, id, requestId);
                Guard.Against.Null(json, nameof(json), requestId);

                return StatusCodes.Status204NoContent;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - OpportunityRepository_DeleteItemAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - OpportunityRepository_DeleteItemAsync Service Exception: {ex}");
            }
        }
        

        // Private methods
        private void CreateDashBoardEntryAsync(string requestId, string id, Opportunity opportunity)
        {
            _logger.LogInformation($"RequestId: {requestId} - CreateDashBoardEntryAsync called.");
            //TODO : WAVE-4 GENERIC ACCELERATOR Change : start

            //try
            //{
            //    if (opportunity.Metadata.TargetDate != null)
            //    {
            //        if(opportunity.Metadata.TargetDate.Date != null && opportunity.Metadata.TargetDate.Date != DateTimeOffset.MinValue)
            //        {
            //            var dashboardmodel = new DashboardModel();
            //            dashboardmodel.CustomerName = opportunity.Metadata.Customer.DisplayName.ToString();
            //            dashboardmodel.OpportunityId = id;
            //            dashboardmodel.Status = opportunity.Metadata.OpportunityState.Name.ToString();
            //            dashboardmodel.TargetCompletionDate = opportunity.Metadata.TargetDate.Date;
            //            dashboardmodel.StartDate = opportunity.Metadata.OpenedDate.Date;
            //            dashboardmodel.StatusChangedDate = opportunity.Metadata.OpenedDate.Date;
            //            dashboardmodel.OpportunityName = opportunity.DisplayName.ToString();

            //            dashboardmodel.LoanOfficer = opportunity.Content.TeamMembers.ToList().Find(x => x.AssignedRole.DisplayName == "LoanOfficer").DisplayName ?? "";
            //            dashboardmodel.RelationshipManager = opportunity.Content.TeamMembers.ToList().Find(x => x.AssignedRole.DisplayName == "RelationshipManager").DisplayName ?? "";

            //            var result = await _dashboardService.CreateOpportunityAsync(dashboardmodel, requestId);
            //        }
            //    }
            //}
            //catch (Exception ex)
            //{
            //    _logger.LogError($"RequestId: {requestId} - CreateDashBoardEntryAsync Service Exception: {ex}");
            //}

            //TODO : WAVE-4 GENERIC ACCELERATOR Change : end
        }

        private bool CheckTeamMember(dynamic oppArtifact, string currentUser)
        {
            foreach (var member in oppArtifact.Content.TeamMembers)
            {
                if (member.Fields.UserPrincipalName == currentUser)
                    return true;
            }
            return false;
        }

        private async Task<Opportunity> UpdateUsersAsync(Opportunity opportunity, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - OpportunityRepository_UpdateUsersAsync called.");

            try
            {
                Guard.Against.Null(opportunity, "OpportunityRepository_UpdateUsersAsync opportunity is null", requestId);

                var usersList = (await _userProfileRepository.GetAllAsync(requestId)).ToList();
                var teamMembers = opportunity.Content.TeamMembers.ToList();
                var updatedTeamMembers = new List<TeamMember>();
                
                foreach (var item in teamMembers)
                {
                    var updatedItem = TeamMember.Empty;
                    updatedItem.Id = item.Id;
                    updatedItem.DisplayName = item.DisplayName;
                    updatedItem.RoleId = item.RoleId;
                    updatedItem.Fields = item.Fields;

                    var currMember = usersList.Find(x => x.Id == item.Id);

                    if (currMember != null)
                    {
                        updatedItem.DisplayName = currMember.DisplayName;
                        updatedItem.Fields = TeamMemberFields.Empty;
                        updatedItem.Fields.Mail = currMember.Fields.Mail;
                        updatedItem.Fields.Title = currMember.Fields.Title;
                        updatedItem.Fields.UserPrincipalName = currMember.Fields.UserPrincipalName;

                        //var hasAssignedRole = currMember.Fields.UserRoles.Find(x => x.DisplayName == item.AssignedRole.DisplayName);

                        //if (opportunity.Metadata.OpportunityState == OpportunityState.InProgress && hasAssignedRole != null)
                        //{
                        //    updatedTeamMembers.Add(updatedItem);
                        //}
                    }
                    else
                    {
                        if (opportunity.Metadata.OpportunityState != OpportunityState.InProgress)
                        {
                            updatedTeamMembers.Add(updatedItem);
                        }
                    }
                }
                opportunity.Content.TeamMembers = updatedTeamMembers;

                // TODO: Also update other users in opportunity like notes which has owner nd maps to a user profile

                return opportunity;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - OpportunityRepository_UpdateUsersAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - OpportunityRepository_UpdateUsersAsync Service Exception: {ex}");
            }
        }

        //Granular Access : Start
        private async Task<(bool havePartial,bool haveAccess, bool haveSuperAcess)>CheckAccessAsync(PermissionNeededTo partialAccess,PermissionNeededTo actionAccess, PermissionNeededTo superAccess, string requestId)
        {
            bool haveAccess = false, haveSuperAcess = false, havePartial = false;
            if (StatusCodes.Status200OK == await _authorizationService.CheckAccessFactoryAsync(superAccess, requestId))
            {
                havePartial = true; haveAccess = true;haveSuperAcess = true;
            }
            else
            {
                if (StatusCodes.Status200OK == await _authorizationService.CheckAccessFactoryAsync(actionAccess, requestId))
                {
                    havePartial = true; haveAccess = true; haveSuperAcess = false;
                }
                else if (StatusCodes.Status200OK == await _authorizationService.CheckAccessFactoryAsync(partialAccess, requestId))
                {
                    havePartial = true; haveAccess = false; haveSuperAcess = false;
                }
                else
                {
                    havePartial = false; haveAccess = true; haveSuperAcess = false;
                }
            }

            return(havePartial: havePartial,haveAccess: haveAccess, haveSuperAcess: haveSuperAcess);
        }
        //Granular Access : End
    }
}
