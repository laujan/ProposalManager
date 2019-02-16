// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using ApplicationCore;
using ApplicationCore.Interfaces;
using ApplicationCore.Interfaces.SmartLink;
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

			if (!string.IsNullOrWhiteSpace(@event) && @event.Equals("create", StringComparison.InvariantCultureIgnoreCase))
			{
                try
                {
                    var jopp = data["InputParameters"].First()["value"]["Attributes"];

                    var attributes = jopp.ToDictionary(p => p["key"], v => v["value"]);

                    var opportunityMapping = dynamicsConfiguration.OpportunityMapping;
                    var opportunityName = GetAttribute(attributes, opportunityMapping.DisplayName)?.ToString();
                    var opportunityId = GetAttribute(attributes, "opportunityid").ToString();
                    var creator = dynamicsLinkService.GetUserData(data["InitiatingUserId"].ToString());
                    var creatorRole = proposalManagerConfiguration.CreatorRole;
                    var opp = new OpportunityViewModel
                    {
                        Reference = opportunityId,
                        DisplayName = opportunityName,
                        OpportunityState = OpportunityStateModel.FromValue(opportunityMapping.MapStatusCode((int)GetAttribute(attributes, "statuscode")["Value"])),
                        Customer = new CustomerModel
                        {
                            DisplayName = dynamicsLinkService.GetAccountName(GetAttribute(attributes, "customerid")?["Id"].ToString())
                        },
                        DealSize = (double?)GetAttribute(attributes, opportunityMapping.DealSize) ?? 0,
                        AnnualRevenue = (double?)GetAttribute(attributes, opportunityMapping.AnnualRevenue) ?? 0,
                        OpenedDate = DateTimeOffset.TryParse(GetAttribute(attributes, opportunityMapping.OpenedDate)?.ToString(), out var dto) ? dto : DateTimeOffset.Now,
                        Margin = (double?)GetAttribute(attributes, opportunityMapping.Margin) ?? 0,
                        Rate = (double?)GetAttribute(attributes, opportunityMapping.Rate) ?? 0,
                        DebtRatio = (double?)GetAttribute(attributes, opportunityMapping.DebtRatio) ?? 0,
                        Purpose = GetAttribute(attributes, opportunityMapping.Purpose)?.ToString(),
                        DisbursementSchedule = GetAttribute(attributes, opportunityMapping.DisbursementSchedule)?.ToString(),
                        CollateralAmount = (double?)GetAttribute(attributes, opportunityMapping.CollateralAmount) ?? 0,
                        Guarantees = GetAttribute(attributes, opportunityMapping.Guarantees)?.ToString(),
                        RiskRating = (int?)GetAttribute(attributes, opportunityMapping.RiskRating) ?? 0,
                        TeamMembers = new TeamMemberModel[]
                        {
                        new TeamMemberModel
                        {
                            DisplayName = creator.DisplayName,
                            Id = creator.Id,
                            Mail = creator.Email,
                            UserPrincipalName = creator.Email,
                            AssignedRole = new RoleModel
                            {
                                AdGroupName = creatorRole.AdGroupName,
                                DisplayName = creatorRole.DisplayName,
                                Id = creatorRole.Id
                            }
                        }
                        },
                        Checklists = new ChecklistModel[] { }
                    };

                    var proposalManagerClient = await proposalManagerClientFactory.GetProposalManagerClientAsync();

                    var userProfileResult = await proposalManagerClient.GetAsync($"/api/UserProfile?upn={creator.Email}");
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
                catch(Exception ex)
                {
                    _logger.LogError(ex.Message);
                    _logger.LogError(ex.StackTrace);
                    return BadRequest();
                }
			}

			return BadRequest($"{nameof(@event)} is required");
		}

		[AllowAnonymous]
		[HttpPost("~/api/[controller]/LinkSharePointLocations")]
		public async Task<IActionResult> LinkSharePointLocationsAsync([FromBody]OpportunityViewModel opportunity)
		{
			try
			{
				if (opportunity is null || !ModelState.IsValid)
					return BadRequest();
                if (!string.IsNullOrWhiteSpace(opportunity.Reference))
                {
                    var locations = from pl in opportunity.DealType.ProcessList
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

			if (!string.IsNullOrWhiteSpace(@event) && @event.Equals("create", StringComparison.InvariantCultureIgnoreCase))
			{
				var initiatingUser = dynamicsLinkService.GetUserData(data["InitiatingUserId"].ToString());
				var client = await proposalManagerClientFactory.GetProposalManagerClientAsync();

				var userProfileResult = await client.GetAsync($"/api/UserProfile?upn={initiatingUser.Email}");
				var userProfile = JsonConvert.DeserializeObject<UserProfileViewModel>(await userProfileResult.Content.ReadAsStringAsync());
				if (!userProfile.UserRoles.Any(ur => new string[]
				{
					"Relationship Managers",
					"Loan Officers"
				}.Contains(ur.AdGroupName)))
					return BadRequest($"{initiatingUser.Email} is not a member of either roles Loan Officers or Relationship Managers.");

				var jconn = data["InputParameters"].First()["value"]["Attributes"];
				var attributes = jconn.ToDictionary(p => p["key"], v => v["value"]);
				var record1id = GetAttribute(attributes, "record1id");
				var record2id = GetAttribute(attributes, "record2id");
				if (record1id["LogicalName"].ToString() != "opportunity" || record2id["LogicalName"].ToString() != "systemuser")
					return new EmptyResult();
				var opportunityId = record1id["Id"].ToString();
				var userId = record2id["Id"].ToString();
				var connectionRoleId = GetAttribute(attributes, "record2roleid")["Id"].ToString();
				var opportunityResult = await client.GetAsync($"/api/Opportunity?reference={opportunityId}");
				if (!opportunityResult.IsSuccessStatusCode)
					return BadRequest();
				var opp = JsonConvert.DeserializeObject<JObject>(await opportunityResult.Content.ReadAsStringAsync());
				var user = dynamicsLinkService.GetUserData(userId);
				var roleName = dynamicsLinkService.GetConnectionRoleName(connectionRoleId);
				((JArray)opp["teamMembers"]).Add(JObject.FromObject(new
				{
					displayName = user.DisplayName,
					id = user.Id,
					mail = user.Email,
					userPrincipalName = user.Email,
					assignedRole = new
					{
						adGroupName = roleName,
						displayName = roleName.Replace(" ", string.Empty)
					}
				}
				));
				var result = await client.PatchAsync("/api/Opportunity", new StringContent(JsonConvert.SerializeObject(opp), Encoding.UTF8, "application/json"));
				return result.IsSuccessStatusCode ? Ok() : (IActionResult)BadRequest();
			}
			return BadRequest($"{nameof(@event)} is required");

		}

		private JToken GetAttribute(Dictionary<JToken, JToken> input, string memberName)
		{
			input.TryGetValue(memberName, out var value);
			return value;
		}
	}
}