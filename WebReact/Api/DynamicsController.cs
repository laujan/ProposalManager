// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using ApplicationCore;
using ApplicationCore.Artifacts;
using ApplicationCore.Interfaces;
using ApplicationCore.Models;
using ApplicationCore.ViewModels;
using Infrastructure.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.WebHooks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace WebReact.Api
{
    [Authorize(AuthenticationSchemes = "AzureAdBearer")]
    public class DynamicsController : BaseApiController<DocumentController>
    {
        private readonly IOneDriveLinkService oneDriveLinkService;
        private readonly IDynamicsLinkService dynamicsLinkService;
        private readonly IOpportunityService opportunityService;
        private readonly IGraphClientAppContext graphClientAppContext;
        private readonly IProposalManagerClientFactory proposalManagerClientFactory;
        private readonly OneDriveConfiguration oneDriveConfiguration;
        private readonly Dynamics365Configuration dynamicsConfiguration;
        private readonly ProposalManagerConfiguration proposalManagerConfiguration;

        public DynamicsController(
            ILogger<DocumentController> logger,
            IOptions<AppOptions> appOptions,
            IDocumentService documentService,
            IOpportunityService opportunityService,
            IGraphClientAppContext graphClientAppContext,
            IOneDriveLinkService oneDriveLinkService,
            IConfiguration configuration,
            IDynamicsLinkService dynamicsLinkService,
            IProposalManagerClientFactory proposalManagerClientFactory) : base(logger, appOptions)
        {
            this.oneDriveLinkService = oneDriveLinkService;
            this.graphClientAppContext = graphClientAppContext;
            this.dynamicsLinkService = dynamicsLinkService;
            this.opportunityService = opportunityService;
            this.proposalManagerClientFactory = proposalManagerClientFactory;

            oneDriveConfiguration = new OneDriveConfiguration();
            configuration.Bind(OneDriveConfiguration.ConfigurationName, oneDriveConfiguration);

            dynamicsConfiguration = new Dynamics365Configuration();
            configuration.Bind(Dynamics365Configuration.ConfigurationName, dynamicsConfiguration);

            proposalManagerConfiguration = new ProposalManagerConfiguration();
            configuration.Bind(ProposalManagerConfiguration.ConfigurationName, proposalManagerConfiguration);
        }

        [AllowAnonymous]
        [HttpPost("~/api/[controller]/FormalProposal")]
        [Consumes("text/plain")]
        public IActionResult FormalProposalAuthorization([FromQuery]string validationToken) => Content(validationToken, new Microsoft.Net.Http.Headers.MediaTypeHeaderValue("text/plain"));

        [AllowAnonymous]
        [HttpPost("~/api/[controller]/FormalProposal")]
        [Consumes("application/json")]
        public async Task<IActionResult> FormalProposalNotifyAsync([FromBody] JObject notification)
        {
            try
            {
                var clientState = SubscriptionClientStateDto.FromJson(notification["value"].First["clientState"].ToString());
                if (clientState.Secret != oneDriveConfiguration.WebhookSecret)
                {
                    return Unauthorized();
                }

                var opportunityName = (string)clientState.Data;
                var resource = notification["value"].First["resource"].ToString();
                await oneDriveLinkService.ProcessFormalProposalChangesAsync(opportunityName, resource);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Dynamics365 Integration error: {ex.Message}");
                _logger.LogError(ex.StackTrace);
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [Consumes("text/plain")]
        [HttpPost("~/api/[controller]/Attachment")]
        public IActionResult AttachmentAuthorize([FromQuery] string validationToken) => Content(validationToken, new Microsoft.Net.Http.Headers.MediaTypeHeaderValue("text/plain"));

        [AllowAnonymous]
        [Consumes("application/json")]
        [HttpPost("~/api/[controller]/Attachment")]
        public async Task<IActionResult> AttachmentNotifyAsync([FromBody] JObject notification)
        {
            try
            {
                var clientState = SubscriptionClientStateDto.FromJson(notification["value"].First["clientState"].ToString());
                if (clientState.Secret != oneDriveConfiguration.WebhookSecret)
                {
                    return Unauthorized();
                }

                var resource = notification["value"].First["resource"].ToString();
                await oneDriveLinkService.ProcessAttachmentChangesAsync(resource);
                return Ok();
            }
            catch (Exception ex)
            {
                _logger.LogError($"Dynamics365 Integration error: {ex.Message}");
                _logger.LogError(ex.StackTrace);
                return BadRequest(ex.Message);
            }
        }

        [AllowAnonymous]
        [HttpPost]
        [DynamicsCRMWebHook(Id = "opportunity")]
        public async Task<IActionResult> CreateOpportunityAsync(string @event, [FromBody] JObject data)
        {
            if (string.IsNullOrWhiteSpace(@event))
            {
                return BadRequest($"{nameof(@event)} is required");
            }
            else if (!@event.Equals("create", StringComparison.InvariantCultureIgnoreCase))
            {
                return BadRequest($"{@event} is not supported");
            }
            else if (data["InputParameters"] == null)
            {
                return BadRequest($"Payload is malformed");
            }

            var opportunityMapping = dynamicsConfiguration.OpportunityMapping;
            try
            {
                var jopp = data["InputParameters"].First()["value"];

                if (!jopp["LogicalName"].ToString().Equals(opportunityMapping.EntityName, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogError($"DYNAMICS INTEGRATION ENGINE: Incorrect entity type recieved from opportunity creation. Expected ${opportunityMapping.EntityName}, got {jopp["LogicalName"].ToString()}.");
                    return BadRequest();
                }

                var opportunityId = jopp["Id"].ToString();

                jopp = jopp["Attributes"];
                var attributes = jopp.ToDictionary(p => p["key"], v => v["value"]);

                var opportunityName = GetAttribute(attributes, opportunityMapping.NameProperty)?.ToString();
                var creator = dynamicsLinkService.GetUserData(data["InitiatingUserId"].ToString());
                var creatorRole = proposalManagerConfiguration.CreatorRole;

                //Determine customer name
                string customerDisplayName = string.Empty;
                var customer = GetAttribute(attributes, "customerid");

                if (customer != null)
                {
                    if (customer["LogicalName"].ToString() == "account")
                    {
                        customerDisplayName = dynamicsLinkService.GetAccountName(customer["Id"].ToString());
                    }
                    else if (customer["LogicalName"].ToString() == "contact")
                    {
                        customerDisplayName = dynamicsLinkService.GetContactName(customer["Id"].ToString());
                    }
                }

                var opp = new OpportunityViewModel
                {
                    Reference = opportunityId,
                    DisplayName = opportunityName,
                    OpportunityState = OpportunityStateModel.FromValue(opportunityMapping.MapStatusCode((int)GetAttribute(attributes, "statuscode"))),
                    Customer = new CustomerModel
                    {
                        DisplayName = customerDisplayName
                    },

                    TeamMembers = new TeamMemberModel[]
                    {
                        new TeamMemberModel
                        {
                            DisplayName = creator.DisplayName,
                            Id = creator.Id,
                            Mail = creator.Email,
                            UserPrincipalName = creator.Email,
                            RoleName = creatorRole.DisplayName,
                            RoleId = creatorRole.Id,
                            TeamsMembership = new TeamsMembershipModel()
                            {
                                Name = "Member",
                                Value = 1
                            }
                        }
                    },
                    Checklists = Array.Empty<ChecklistModel>()
                };

                var proposalManagerClient = await proposalManagerClientFactory.GetProposalManagerClientAsync();

                var metaDataResult = await proposalManagerClient.GetAsync("/api/MetaData");
                if (!metaDataResult.IsSuccessStatusCode)
                {
                    _logger.LogError("DYNAMICS INTEGRATION ENGINE: Proposal Manager did not return a success status code on metadata request.");
                    return BadRequest();
                }
                var metadataList = await metaDataResult.Content.ReadAsAsync<List<MetaDataModel>>();
                opp.MetaDataFields = new List<OpportunityMetaDataFields>();

                foreach (var metadata in metadataList)
                {
                    var mappingName = opportunityMapping.MetadataFields.FirstOrDefault(x => x.To == metadata.DisplayName);

                    if (mappingName != null)
                    {
                        opp.MetaDataFields.Add(new OpportunityMetaDataFields
                        {
                            DisplayName = metadata.DisplayName,
                            Values = GetAttribute(attributes, mappingName.From, metadata.FieldType),
                            FieldType = metadata.FieldType,
                            Screen = metadata.Screen
                        });
                    }
                }

                var userProfileResult = await proposalManagerClient.GetAsync($"/api/UserProfile?upn={creator.Email}");
                if (!userProfileResult.IsSuccessStatusCode)
                {
                    _logger.LogError("DYNAMICS INTEGRATION ENGINE: Proposal Manager did not return a success status code on user query request.");
                    return BadRequest();
                }

                var userProfile = JsonConvert.DeserializeObject<UserProfileViewModel>(await userProfileResult.Content.ReadAsStringAsync());
                if (!userProfile.UserRoles.Any(ur => ur.AdGroupName == creatorRole.AdGroupName))
                    return BadRequest($"{creator.Email} is not a member of role {creatorRole.AdGroupName}.");

                var remoteEndpoint = $"/api/Opportunity";
                var result = await proposalManagerClient.PostAsync(remoteEndpoint, new StringContent(JsonConvert.SerializeObject(opp), Encoding.UTF8, "application/json"));

                if (result.IsSuccessStatusCode)
                {
                    await dynamicsLinkService.CreateTemporaryLocationForOpportunityAsync(opportunityId, opportunityName);
                    return Ok();
                }
                else
                {
                    _logger.LogError("DYNAMICS INTEGRATION ENGINE: Proposal Manager did not return a success status code.");
                    return BadRequest();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
                return BadRequest();
            }
        }

        [AllowAnonymous]
        [HttpPost("~/api/[controller]/LinkSharePointLocations")]
        public async Task<IActionResult> LinkSharePointLocationsAsync([FromBody]OpportunityViewModel opportunity)
        {
            try
            {
                if (opportunity is null || !ModelState.IsValid)
                {
                    return BadRequest();
                }

                if (!string.IsNullOrWhiteSpace(opportunity.Reference))
                {
                    var locations = from pl in opportunity.Template.ProcessList
                                    where pl.Channel.ToLower() != "base" && pl.Channel.ToLower() != "none"
                                    select pl.Channel;
                    _logger.LogInformation($"Locations detected for opportunity {opportunity.DisplayName}: {string.Join(", ", locations)}");
                    await dynamicsLinkService.CreateLocationsForOpportunityAsync(opportunity.Reference, opportunity.DisplayName, locations);
                }

                return Ok();
            }
            catch (Exception ex)
            {
                var message = $"LinkSharePointLocationsAsync error: {ex.Message}";
                _logger.LogError(message);
                _logger.LogError(ex.StackTrace);
                return BadRequest(message);
            }
        }

        [AllowAnonymous]
        [HttpPost]
        [DynamicsCRMWebHook(Id = "connection")]
        public async Task<IActionResult> AddTeamMemberAsync(string @event, JObject data)
        {
            if (string.IsNullOrWhiteSpace(@event))
            {
                return BadRequest($"{nameof(@event)} is required");
            }
            else if (!@event.Equals("create", StringComparison.InvariantCultureIgnoreCase))
            {
                return BadRequest($"{@event} is not supported");
            }
            else if (data["InputParameters"] == null)
            {
                return BadRequest($"Payload is malformed");
            }

            try
            {
                var attributes = data["InputParameters"].First()["value"]["Attributes"].ToDictionary(p => p["key"], v => v["value"]);

                var record1id = GetAttribute(attributes, "record1id");
                var record2id = GetAttribute(attributes, "record2id");

                if (!record1id["LogicalName"].ToString().Equals(dynamicsConfiguration.OpportunityMapping.EntityName, StringComparison.OrdinalIgnoreCase) ||
                    !record2id["LogicalName"].ToString().Equals("systemuser", StringComparison.OrdinalIgnoreCase))
                {
                    return new EmptyResult();
                }

                Task<HttpClient> taskClient = proposalManagerClientFactory.GetProposalManagerClientAsync();
                var initiatingUser = dynamicsLinkService.GetUserData(data["InitiatingUserId"].ToString());
                var client = await taskClient;

                var initiatingUserProfileResult = await client.GetAsync($"/api/UserProfile?upn={initiatingUser.Email}");
                var initiatingUserProfile = await initiatingUserProfileResult.Content.ReadAsAsync<UserProfileViewModel>();

                //Check that initiatingUser has either a creator or a lead role
                if (!initiatingUserProfile.UserRoles.Any(ur => proposalManagerConfiguration.LeadRoles.Concat(new string[] {
                    proposalManagerConfiguration.CreatorRole.AdGroupName
                }).Contains(ur.AdGroupName)))
                {
                    return BadRequest($"{initiatingUser.Email} is not a member of either a creator role ({proposalManagerConfiguration.CreatorRole.AdGroupName}) or any of the defined lead roles.");
                }

                var opportunityId = record1id["Id"].ToString();
                var userId = record2id["Id"].ToString();
                var connectionRoleId = GetAttribute(attributes, "record2roleid")["Id"].ToString();

                Task<HttpResponseMessage> taskOpportunity = client.GetAsync($"/api/Opportunity?reference={opportunityId}");
                var user = dynamicsLinkService.GetUserData(userId);
                Task<HttpResponseMessage> taskUserProfile = client.GetAsync($"/api/UserProfile?upn={user.Email}");
                var roleName = dynamicsLinkService.GetConnectionRoleName(connectionRoleId);

                await taskOpportunity;
                if (!taskOpportunity.Result.IsSuccessStatusCode)
                {
                    _logger.LogError($"DYNAMICS INTEGRATION ENGINE: Proposal Manager did not return a success status code on opportunity id {opportunityId} query.");
                    return BadRequest();
                }

                var opportunity = await taskOpportunity.Result.Content.ReadAsAsync<OpportunityViewModel>();

                await taskUserProfile;
                if (!taskUserProfile.Result.IsSuccessStatusCode)
                {
                    _logger.LogError($"DYNAMICS INTEGRATION ENGINE: Proposal Manager did not return a success status code on userProfile {user.Email} query.");
                    return BadRequest();
                }

                var userProfile = await taskUserProfile.Result.Content.ReadAsAsync<UserProfileViewModel>();
                var role = userProfile.UserRoles.Where(r => r.UserPermissions.Any(p => p.Name == "Opportunity_ReadWrite_Dealtype")).FirstOrDefault();

                if (role == null)
                {
                    //User is not a Loan Officer.
                    role = userProfile.UserRoles.Where(r => r.DisplayName == roleName).FirstOrDefault();

                    if (role != null)
                    {
                        opportunity.TeamMembers.Add(CreateBaseProcessPersonal(userProfile, role, roleName));
                    }
                    else
                    {
                        return BadRequest($"{userProfile.Mail} is not a member of {roleName}.");
                    }
                }
                else
                {
                    //User is a Loan Officer. Doing the same procedure as UI.
                    opportunity.TeamMembers.Add(CreateBaseProcessPersonal(userProfile, role, "Start Process"));
                    opportunity.TeamMembers.Add(CreateBaseProcessPersonal(userProfile, role, "Customer Decision"));
                }

                var result = await client.PatchAsync("/api/Opportunity", new StringContent(JsonConvert.SerializeObject(opportunity), Encoding.UTF8, "application/json"));

                if (result.IsSuccessStatusCode)
                {
                    return Ok();
                }
                else
                {
                    _logger.LogError("DYNAMICS INTEGRATION ENGINE: Proposal Manager did not return a success status code.");
                    return BadRequest();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                _logger.LogError(ex.StackTrace);
                return BadRequest();
            }
        }

        private TeamMemberModel CreateBaseProcessPersonal(UserProfileViewModel user, RoleModel role, string processStep)
        {
            return new TeamMemberModel
            {
                Id = user.Id,
                DisplayName = user.DisplayName,
                Mail = user.Mail,
                UserPrincipalName = user.UserPrincipalName,
                RoleId = role.Id,
                RoleName = role.DisplayName,
                AdGroupName = role.DisplayName,
                TeamsMembership = new TeamsMembershipModel()
                {
                    Name = role.TeamsMembership.Name,
                    Value = role.TeamsMembership.Value,
                },

                Permissions = role.UserPermissions.Select(r => new PermissionModel { Id = r.Id, Name = r.Name }).ToList(),

                ProcessStep = processStep
            };
        }

        private JToken GetAttribute(Dictionary<JToken, JToken> input, string memberName)
        {
            input.TryGetValue(memberName, out var value);

            if (value != null && value.HasValues && value["Value"] != null)
            {
                value = value["Value"];
            }

            return value;
        }

        private dynamic GetAttribute(Dictionary<JToken, JToken> input, string memberName, FieldType type)
        {
            var value = GetAttribute(input, memberName);

            if (value == null)
            {
                if (type.Value == FieldType.Date.Value)
                {
                    return DateTimeOffset.Now;
                }
                else if (type.Value == FieldType.Double.Value)
                {
                    return (double)0;
                }
                else if (type.Value == FieldType.Int)
                {
                    return 0;
                }
                else if (type.Value == FieldType.String)
                {
                    return String.Empty;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                if (type.Value == FieldType.Date.Value)
                {
                    return DateTimeOffset.TryParse(value.ToString(), out var dto) ? dto : DateTimeOffset.Now;
                }

                return value;
            }
        }
    }
}