// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using System;
using System.Collections.Generic;
using System.Text;
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
using Infrastructure.GraphApi;
using ApplicationCore.Helpers.Exceptions;
using Microsoft.Graph;
using System.Linq;
using System.Text.RegularExpressions;
using ApplicationCore.Models;
using Infrastructure.Authorization;

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
        }

        public async Task<StatusCodes> CreateItemAsync(Opportunity opportunity, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - OpportunityRepository_CreateItemAsync called.");

            try
            {
                Guard.Against.Null(opportunity, nameof(opportunity), requestId);
                Guard.Against.NullOrEmpty(opportunity.DisplayName, nameof(opportunity.DisplayName), requestId);

                var roles = new List<Role>();
                roles.Add(new Role { DisplayName = "RelationshipManager" });

                //Granular Access : Start
                if (StatusCodes.Status401Unauthorized == await _authorizationService.CheckAccessFactoryAsync(PermissionNeededTo.Create, requestId)) return StatusCodes.Status401Unauthorized;
                //Granular Access : End
                // Ensure id is blank since it will be set by SharePoint
                opportunity.Id = String.Empty;

                // TODO: This section will be replaced with a workflow
                opportunity = await _opportunityFactory.CreateWorkflowAsync(opportunity, requestId);


                _logger.LogInformation($"RequestId: {requestId} - OpportunityRepository_CreateItemAsync creating SharePoint List for opportunity.");

                //Get loan officer & relationship manager values
                var loanOfficerId = String.Empty;
                var relationshipManagerId = String.Empty;
                var loanOfficerUpn = String.Empty;
                var relationshipManagerUpn = String.Empty;
                foreach (var item in opportunity.Content.TeamMembers)
                {
                    if (item.AssignedRole.DisplayName == "LoanOfficer" && !String.IsNullOrEmpty(item.Id))
                    {
                        loanOfficerId = item.Id;
                        loanOfficerUpn = item.Fields.UserPrincipalName;
                    }
                    if (item.AssignedRole.DisplayName == "RelationshipManager" && !String.IsNullOrEmpty(item.Id))
                    {
                        relationshipManagerId = item.Id;
                        relationshipManagerUpn = item.Fields.UserPrincipalName;
                    }
                }


                // Create Json object for SharePoint create list item
                dynamic opportunityFieldsJson = new JObject();
                opportunityFieldsJson.Name = opportunity.DisplayName;
                opportunityFieldsJson.OpportunityState = opportunity.Metadata.OpportunityState.Name;
                opportunityFieldsJson.OpportunityObject = JsonConvert.SerializeObject(opportunity, Formatting.Indented);
                opportunityFieldsJson.LoanOfficer = loanOfficerId;
                opportunityFieldsJson.RelationshipManager = relationshipManagerId;
                opportunityFieldsJson.Reference = opportunity.Reference ?? String.Empty;

                dynamic opportunityJson = new JObject();
                opportunityJson.fields = opportunityFieldsJson;

                var opportunitySiteList = new SiteList
                {
                    SiteId = _appOptions.ProposalManagementRootSiteId,
                    ListId = _appOptions.OpportunitiesListId
                };

                var result = await _graphSharePointAppService.CreateListItemAsync(opportunitySiteList, opportunityJson.ToString(), requestId);
                //DashBoard Create call Start.
                try
                {
                    var id = JObject.Parse(result.ToString()).SelectToken("id").ToString();
                    await CreateDashBoardEntryAsync(requestId, id, opportunity);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"RequestId: {requestId} - OpportunityRepository_CreateItemAsync create dashboard entry Exception: {ex}");
                    //await CreateDashBoardEntryAsync(requestId, "1", opportunity);
                }
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

                //Get loan officer & relationship manager values
                var loanOfficerId = String.Empty;
                var relationshipManagerId = String.Empty;
                var loanOfficerUpn = String.Empty;
                var relationshipManagerUpn = String.Empty;
                foreach (var item in opportunity.Content.TeamMembers)
                {
                    if (item.AssignedRole.DisplayName == "LoanOfficer" && !String.IsNullOrEmpty(item.Id))
                    {
                        loanOfficerId = item.Id;
                        loanOfficerUpn = item.Fields.UserPrincipalName;
                    }
                    if (item.AssignedRole.DisplayName == "RelationshipManager" && !String.IsNullOrEmpty(item.Id))
                    {
                        relationshipManagerId = item.Id;
                        relationshipManagerUpn = item.Fields.UserPrincipalName;
                    }
                }


                var opportunityJObject = JObject.FromObject(opportunity);

                // Create Json object for SharePoint create list item
                dynamic opportunityJson = new JObject();
                opportunityJson.OpportunityId = opportunity.Id;
                opportunityJson.OpportunityState = opportunity.Metadata.OpportunityState.Name;
                opportunityJson.OpportunityObject = JsonConvert.SerializeObject(opportunity, Formatting.Indented);
                opportunityJson.LoanOfficer = loanOfficerId;
                opportunityJson.RelationshipManager = relationshipManagerId;
                opportunityJson.Reference = opportunity.Reference ?? String.Empty;

                var opportunitySiteList = new SiteList
                {
                    SiteId = _appOptions.ProposalManagementRootSiteId,
                    ListId = _appOptions.OpportunitiesListId
                };
                var result = await _graphSharePointAppService.UpdateListItemAsync(opportunitySiteList, opportunity.Id, opportunityJson.ToString(), requestId);

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

                var opportunityJson = json["fields"]["OpportunityObject"].ToString();

                var oppArtifact = JsonConvert.DeserializeObject<Opportunity>(opportunityJson.ToString(), new JsonSerializerSettings
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
                //Granular Access : End

                oppArtifact.Id = json["fields"]["id"].ToString();

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

                var json = await _graphSharePointAppService.GetListItemAsync(opportunitySiteList, options, "all", requestId);
                Guard.Against.Null(json, "OpportunityRepository_GetItemByNameAsync GetListItemAsync Null", requestId);

                dynamic jsonDyn = json;

                if (jsonDyn.value.HasValues)
                {
                    foreach (var item in jsonDyn.value)
                    {
                        if (item.fields.Name == name)
                        {
                            if (isCheckName)
                            {
                                // If just checking for name, rtunr empty opportunity and skip access check
                                var emptyOpportunity = Opportunity.Empty;
                                emptyOpportunity.DisplayName = name;
                                return emptyOpportunity;
                            }

                            var opportunityJson = item.fields.OpportunityObject.ToString();

                            var oppArtifact = JsonConvert.DeserializeObject<Opportunity>(opportunityJson, new JsonSerializerSettings
                            {
                                MissingMemberHandling = MissingMemberHandling.Ignore,
                                NullValueHandling = NullValueHandling.Ignore
                            });

                            oppArtifact.Id = jsonDyn.value[0].fields.id.ToString();
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

                var json = await _graphSharePointAppService.GetListItemAsync(opportunitySiteList, options, "all", requestId);
                Guard.Against.Null(json, "OpportunityRepository_GetItemByRefAsync GetListItemAsync Null", requestId);

                dynamic jsonDyn = json;

                if (jsonDyn.value.HasValues)
                {
                    foreach (var item in jsonDyn.value)
                    {
                        if (item.fields.Reference == reference)
                        {
                            var opportunityJson = item.fields.OpportunityObject.ToString();

                            var oppArtifact = JsonConvert.DeserializeObject<Opportunity>(opportunityJson, new JsonSerializerSettings
                            {
                                MissingMemberHandling = MissingMemberHandling.Ignore,
                                NullValueHandling = NullValueHandling.Ignore
                            });

                            oppArtifact.Id = jsonDyn.value[0].fields.id.ToString();

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

                var isLoanOfficer = false;
                var isRelationshipManager = false;
                var isAdmin = false;

                if (callerUser.Fields.UserRoles.Find(x => x.DisplayName == "LoanOfficer") != null)
                {
                    isLoanOfficer = true;
                }
                if (callerUser.Fields.UserRoles.Find(x => x.DisplayName == "RelationshipManager") != null)
                {
                    isRelationshipManager = true;
                }
                if (callerUser.Fields.UserRoles.Find(x => x.DisplayName == "Administrator") != null)
                {
                    //Granular Access : Start
                    //Admin access
                    if (StatusCodes.Status200OK == await _authorizationService.CheckAccessFactoryAsync(PermissionNeededTo.Admin, requestId)) isAdmin = true;
                    //Granular Access : End
                }
                if (currentUserScope != "access_as_user") //TODO: Temp conde while graular access control is finished in w3
                {
                    isAdmin = true;
                }

                //Granular Access : Start
                if (access.haveAccess == false && access.haveSuperAcess == false && access.havePartial==false)
                {
                    // This user is not having any read permissions, so he won't be able to list of opportunities
                    _logger.LogError($"RequestId: {requestId} - OpportunityRepository_GetItemByIdAsync current user: {currentUser} AccessDeniedException");
                    throw new AccessDeniedException($"RequestId: {requestId} - OpportunityRepository_GetItemByIdAsync current user: {currentUser} AccessDeniedException");
                }
                //Granular Access : End

                var options = new List<QueryParam>();
                var jsonLoanOfficer = new JObject();
                var jsonRelationshipManager = new JObject();
                var jsonAdmin = new JObject();
                var itemsList = new List<Opportunity>();
                var jsonArray = new JArray();

                if (isAdmin)
                {
                    jsonAdmin = await _graphSharePointAppService.GetListItemsAsync(siteList, "all", requestId);

                    if (jsonAdmin.HasValues)
                    {
                        jsonArray = JArray.Parse(jsonAdmin["value"].ToString());
                    }


                    foreach (var item in jsonArray)
                    {
                        var opportunityJson = item["fields"]["OpportunityObject"].ToString();

                        var oppArtifact = JsonConvert.DeserializeObject<Opportunity>(opportunityJson.ToString(), new JsonSerializerSettings
                        {
                            MissingMemberHandling = MissingMemberHandling.Ignore,
                            NullValueHandling = NullValueHandling.Ignore
                        });

                        oppArtifact.Id = item["fields"]["id"].ToString();

                        itemsList.Add(oppArtifact);
                    }
                }
                else
                {
                    if (isLoanOfficer)
                    {
                        options.Add(new QueryParam("filter", $"startswith(fields/LoanOfficer,'{callerUser.Id}')"));
                        jsonLoanOfficer = await _graphSharePointAppService.GetListItemsAsync(siteList, options, "all", requestId);
                    }


                    if (isRelationshipManager)
                    {
                        options.Add(new QueryParam("filter", $"startswith(fields/RelationshipManager,'{callerUser.Id}')"));
                        jsonRelationshipManager = await _graphSharePointAppService.GetListItemsAsync(siteList, options, "all", requestId);
                    }

                    if (jsonLoanOfficer.HasValues)
                    {
                        jsonArray = JArray.Parse(jsonLoanOfficer["value"].ToString());
                    }


                    foreach (var item in jsonArray)
                    {
                        var opportunityJson = item["fields"]["OpportunityObject"].ToString();

                        var oppArtifact = JsonConvert.DeserializeObject<Opportunity>(opportunityJson.ToString(), new JsonSerializerSettings
                        {
                            MissingMemberHandling = MissingMemberHandling.Ignore,
                            NullValueHandling = NullValueHandling.Ignore
                        });

                        oppArtifact.Id = item["fields"]["id"].ToString();

                        //Granular Access : Start
                        if (access.haveSuperAcess)
                            itemsList.Add(oppArtifact);
                        else
                        {
                            if ((oppArtifact.Content.TeamMembers).ToList().Any
                                (teamMember => teamMember.Fields.UserPrincipalName==currentUser))
                                itemsList.Add(oppArtifact);
                        }
                        //Granular Access : end
                    }

                    if (jsonRelationshipManager.HasValues)
                    {
                        jsonArray = JArray.Parse(jsonRelationshipManager["value"].ToString());
                    }

                    foreach (var item in jsonArray)
                    {
                        var opportunityJson = item["fields"]["OpportunityObject"].ToString();

                        var oppArtifact = JsonConvert.DeserializeObject<Opportunity>(opportunityJson.ToString(), new JsonSerializerSettings
                        {
                            MissingMemberHandling = MissingMemberHandling.Ignore,
                            NullValueHandling = NullValueHandling.Ignore
                        });

                        oppArtifact.Id = item["fields"]["id"].ToString();

                        var dupeOpp = itemsList.Find(x => x.DisplayName == oppArtifact.DisplayName);
                        if (dupeOpp == null)
                        {
                            //Granular Access : Start
                            if (access.haveSuperAcess)
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

                //For DashBorad--delete opportunity
                await DeleteOpportunityFrmDashboardAsync(id, requestId);

                return StatusCodes.Status204NoContent;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - OpportunityRepository_DeleteItemAsync Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - OpportunityRepository_DeleteItemAsync Service Exception: {ex}");
            }
        }
        

        // Private methods
        private async Task CreateDashBoardEntryAsync(string requestId, string id, Opportunity opportunity)
        {
            _logger.LogInformation($"RequestId: {requestId} - CreateDashBoardEntryAsync called.");
            try
            {
                if (opportunity.Metadata.TargetDate != null)
                {
                    if(opportunity.Metadata.TargetDate.Date != null && opportunity.Metadata.TargetDate.Date != DateTimeOffset.MinValue)
                    {
                        var dashboardmodel = new DashboardModel();
                        dashboardmodel.CustomerName = opportunity.Metadata.Customer.DisplayName.ToString();
                        dashboardmodel.OpportunityId = id;
                        dashboardmodel.Status = opportunity.Metadata.OpportunityState.Name.ToString();
                        dashboardmodel.TargetCompletionDate = opportunity.Metadata.TargetDate.Date;
                        dashboardmodel.StartDate = opportunity.Metadata.OpenedDate.Date;
                        dashboardmodel.StatusChangedDate = opportunity.Metadata.OpenedDate.Date;
                        dashboardmodel.OpportunityName = opportunity.DisplayName.ToString();

                        dashboardmodel.LoanOfficer = opportunity.Content.TeamMembers.ToList().Find(x => x.AssignedRole.DisplayName == "LoanOfficer").DisplayName ?? "";
                        dashboardmodel.RelationshipManager = opportunity.Content.TeamMembers.ToList().Find(x => x.AssignedRole.DisplayName == "RelationshipManager").DisplayName ?? "";

                        var result = await _dashboardService.CreateOpportunityAsync(dashboardmodel, requestId);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - CreateDashBoardEntryAsync Service Exception: {ex}");
            }
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

        private async Task DeleteOpportunityFrmDashboardAsync(string id, string requestId)
        {
            _logger.LogInformation($"RequestId: {requestId} - DeleteOpportunityFrmDashboardAsync called.");
            try
            {
                _logger.LogInformation($"RequestId: {requestId} - DeleteOpportunityFrmDashboard called.");

                var dashboardlist = (await _dashboardService.GetAllAsync(requestId)).ToList();
                var dashboardId = dashboardlist.Find(x => x.OpportunityId == id).Id.ToString();
                if (!string.IsNullOrEmpty(dashboardId))
                    await _dashboardService.DeleteOpportunityAsync(dashboardId, requestId);
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - DeleteOpportunityFrmDashboardAsync Service Exception: {ex}");
            }
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
                    updatedItem.AssignedRole = item.AssignedRole;
                    updatedItem.Fields = item.Fields;

                    var currMember = usersList.Find(x => x.Id == item.Id);

                    if (currMember != null)
                    {
                        updatedItem.DisplayName = currMember.DisplayName;
                        updatedItem.Fields = TeamMemberFields.Empty;
                        updatedItem.Fields.Mail = currMember.Fields.Mail;
                        updatedItem.Fields.Title = currMember.Fields.Title;
                        updatedItem.Fields.UserPrincipalName = currMember.Fields.UserPrincipalName;

                        var hasAssignedRole = currMember.Fields.UserRoles.Find(x => x.DisplayName == item.AssignedRole.DisplayName);

                        if (opportunity.Metadata.OpportunityState == OpportunityState.InProgress && hasAssignedRole != null)
                        {
                            updatedTeamMembers.Add(updatedItem);
                        }
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
