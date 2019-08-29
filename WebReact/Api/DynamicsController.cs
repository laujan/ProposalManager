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

        /// <summary>
        /// Processes the creation of an Opportunity from Dynamics 365. This method is executed asynchronously.
        /// Example payload avaliable in Dynamics Integration/Sample payloads/opportunity_creation.json
        /// </summary>
        /// <param name="event"></param>
        /// <param name="data"></param>
        /// <returns></returns>
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

                var attributes = jopp["Attributes"].ToDictionary(p => p["key"], v => v["value"]);
                var formattedValues = jopp["FormattedValues"].ToDictionary(p => p["key"], v => v["value"]);

                var opportunityName = GetAttribute(attributes, opportunityMapping.NameProperty)?.ToString();
                var creator = dynamicsLinkService.GetUserData(data["InitiatingUserId"].ToString());
                var creatorRole = proposalManagerConfiguration.CreatorRole;

                //Determine customer name. Gives priority to 'account' property; otherwise 'contact'.
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
                string dealTypeToAssign = string.Empty;

                //Deal type can be assigned either by configuration, or by Lookup in Dynamics. Gives priority to configuration.
                if (!string.IsNullOrEmpty(dynamicsConfiguration.DefaultDealType))
                {
                    dealTypeToAssign = dynamicsConfiguration.DefaultDealType;
                }
                else
                {
                    string dealType = GetNativeAttribute(attributes, formattedValues, "msbnk_dealtype", FieldType.String);
                    if (!string.IsNullOrEmpty(dealType))
                    {
                        dealTypeToAssign = dealType;
                    }
                }

                if (!string.IsNullOrEmpty(dealTypeToAssign))
                {
                    var updatedOpp = await TryAssignDealType(opp, proposalManagerClient, dealTypeToAssign);

                    if (updatedOpp == null)
                    {
                        return BadRequest();
                    }
                    else
                    {
                        opp = updatedOpp;
                    }
                }

                //Query metadata from Proposal Manager to map fields based on configuration
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
                            Values = GetNativeAttribute(attributes, formattedValues, mappingName.From, metadata.FieldType),
                            FieldType = metadata.FieldType,
                            Screen = metadata.Screen,
                            Required = metadata.Required,
                            UniqueId = metadata.UniqueId
                        });
                    }
                }

                //Query user to Proposal Manager by UPN, to verify it has the configured Opportunity Creator Role
                var userProfileResult = await proposalManagerClient.GetAsync($"/api/UserProfile?upn={creator.Email}");
                if (!userProfileResult.IsSuccessStatusCode)
                {
                    _logger.LogError("DYNAMICS INTEGRATION ENGINE: Proposal Manager did not return a success status code on user query request.");
                    return BadRequest();
                }

                var userProfile = JsonConvert.DeserializeObject<UserProfileViewModel>(await userProfileResult.Content.ReadAsStringAsync());
                if (!userProfile.UserRoles.Any(ur => ur.AdGroupName == creatorRole.AdGroupName))
                    return BadRequest($"{creator.Email} is not a member of role {creatorRole.AdGroupName}.");

                //POST Opportunity to Proposal Manager
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

        /// <summary>
        /// Processes the creation of a Connection from Dynamics 365. This method is executed synchronously (speed is important to not hang Dynamics UI).
        /// Example payload avaliable in Dynamics Integration/Sample payloads/connection_creation.json
        /// </summary>
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

                //We only process connections between opportunities and system users
                if (!record1id["LogicalName"].ToString().Equals(dynamicsConfiguration.OpportunityMapping.EntityName, StringComparison.OrdinalIgnoreCase) ||
                    !record2id["LogicalName"].ToString().Equals("systemuser", StringComparison.OrdinalIgnoreCase))
                {
                    return new EmptyResult();
                }

                //We use tasks through this method to make parallel requests.
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
                    //User is not a Loan Officer. We simply add it to the TeamMembers list.
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

        /// <summary>
        /// Tries searching for a deal type by its name in the Proposal Manager API. If found, assigns it to the provided opportunity.
        /// </summary>
        /// <param name="opportunity">The opportunity to modify.</param>
        /// <param name="proposalManagerClient">A initialized Proposal Manager client.</param>
        /// <param name="dealTypeName">The name of the deal type to assign.</param>
        /// <returns></returns>
        private async Task<OpportunityViewModel> TryAssignDealType(OpportunityViewModel opportunity, HttpClient proposalManagerClient, string dealTypeName)
        {
            var processResult = await proposalManagerClient.GetAsync("/api/Template");
            if (!processResult.IsSuccessStatusCode)
            {
                _logger.LogError("DYNAMICS INTEGRATION ENGINE: Proposal Manager did not return a success status code on Template request.");
                return null;
            }

            var processList = await processResult.Content.ReadAsAsync<TemplateListViewModel>();
            var dealType = processList.ItemsList.FirstOrDefault(x => x.TemplateName == dealTypeName);

            if (dealType == null)
            {
                _logger.LogError($"DYNAMICS INTEGRATION ENGINE: Required deal type ({dealTypeName}) doesn't exist in Proposal Manager.");
                return null;
            }

            //Deal Type found. Assign it to Opportunity. Doing same process as UI...
            opportunity.Template = dealType;
            opportunity.Template.ProcessList.First(p => p.ProcessStep == "Start Process").Status = ActionStatus.Completed;
            opportunity.OpportunityState = OpportunityStateModel.InProgress;

            return opportunity;
        }

        /// <summary>
        /// Returns the required attribute as a JToken object.
        /// </summary>
        /// <param name="attributes">The list of attributes to search in.</param>
        /// <param name="memberName">The attribute name to search for.</param>
        /// <returns></returns>
        private JToken GetAttribute(Dictionary<JToken, JToken> attributes, string memberName)
        {
            attributes.TryGetValue(memberName, out var jToken);

            if (jToken != null && jToken.HasValues && jToken["Value"] != null)
            {
                jToken = jToken["Value"];
            }

            return jToken;
        }

        /// <summary>
        /// Returns the required attribute as a JToken object, checking if it comes from an OptionSet.
        /// </summary>
        /// <param name="attributes">The list of attributes to search in.</param>
        /// <param name="formattedValues">The list of formattedValues to look up if the attribute comes from an OptionSet</param>
        /// <param name="memberName">The attribute name to search for.</param>
        /// <param name="formattedValue">If the attribute comes from an OptionSet, this string will be completed with the corresponding formatted value</param>
        /// <returns></returns>
        private JToken GetAttribute(Dictionary<JToken, JToken> attributes, Dictionary<JToken, JToken> formattedValues, string memberName, out string formattedValue)
        {
            formattedValue = null;

            attributes.TryGetValue(memberName, out var jToken);

            if (jToken != null && jToken.HasValues && jToken["Value"] != null)
            {
                if (jToken["__type"] != null && jToken["__type"].Value<string>().StartsWith("OptionSetValue", StringComparison.OrdinalIgnoreCase))
                {
                    //This attribute comes from an OptionSet. Try to get its formatted value
                    formattedValues.TryGetValue(memberName, out var jTokenFormatted);

                    if (jTokenFormatted != null)
                    {
                        formattedValue = jTokenFormatted.ToObject<string>();
                    }
                }

                jToken = jToken["Value"];
            }

            return jToken;
        }

        /// <summary>
        /// Returns the required attribute as a .NET object with the appropiate type.
        /// </summary>
        /// <param name="attributes">The list of attributes to search in.</param>
        /// <param name="formattedValues">The list of formattedValues to look up if the attribute comes from an OptionSet.</param>
        /// <param name="memberName">The attribute name to search for.</param>
        /// <param name="type">The expected type for the attribute.</param>
        /// <returns></returns>
        private dynamic GetNativeAttribute(Dictionary<JToken, JToken> attributes, Dictionary<JToken, JToken> formattedValues, string memberName, FieldType type)
        {
            var value = GetAttribute(attributes, formattedValues, memberName, out var formattedValue);

            if (formattedValue != null && type.Value == FieldType.String.Value)
            {
                return formattedValue;
            }

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
                else if (type.Value == FieldType.Int.Value)
                {
                    return 0;
                }
                else if (type.Value == FieldType.String.Value)
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

                //This returns a .NET object instead of a JToken
                return ((JValue)value).Value;
            }
        }

        /// <summary>
        /// Initializes and returns a TeamMemberModel ready to be added to the TeamMembers list of an opportunity
        /// </summary>
        /// <param name="user">The user to add as a TeamMember.</param>
        /// <param name="role">The role of the user.</param>
        /// <param name="processStep">The name of the process step.</param>
        /// <returns></returns>
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
    }
}