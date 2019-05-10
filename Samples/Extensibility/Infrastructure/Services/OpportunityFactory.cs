// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ApplicationCore.Artifacts;
using ApplicationCore.Interfaces;
using ApplicationCore.Entities;
using ApplicationCore.Services;
using ApplicationCore;
using ApplicationCore.Helpers;
using Newtonsoft.Json.Linq;
using ApplicationCore.Helpers.Exceptions;
using System.Linq;
using System.Text.RegularExpressions;

namespace Infrastructure.Services
{
    public class OpportunityFactory : BaseArtifactFactory<Opportunity>, IOpportunityFactory
	{
		private readonly GraphSharePointAppService _graphSharePointAppService;
		private readonly GraphUserAppService _graphUserAppService;

        private readonly CardNotificationService _cardNotificationService;
        private readonly ICheckListProcessService _checkListProcessService;
        private readonly ICustomerDecisionProcessService _customerDecisionProcessService;
        private readonly ICustomerFeedbackProcessService _customerFeedbackProcessService;
        private readonly IProposalDocumentProcessService _proposalStatusProcessService;
        private readonly INewOpportunityProcessService _newOpportunityProcessService;
        private readonly IStartProcessService _startProcessService;
        private readonly IDashboardService _dashboardService;
        private readonly IAuthorizationService _authorizationService;
        private readonly IPermissionRepository _permissionRepository;
        private readonly IDashboardAnalysis _dashboardAnalysis;
        protected readonly GraphTeamsAppService _graphTeamsAppService;
        protected readonly GraphTeamsOnBehalfService _graphTeamsOnBehalfService ;
        private readonly IAddInHelper _addInHelper;
        protected readonly IAzureKeyVaultService _azureKeyVaultService;

        private readonly IMemberService _memberService;
        private readonly ITeamChannelService _teamChannelService;
        private readonly INotesService _notesService;

        public OpportunityFactory(
            ILogger<OpportunityFactory> logger,
            IOptionsMonitor<AppOptions> appOptions,
            GraphSharePointAppService graphSharePointAppService,
            GraphUserAppService graphUserAppService,
            CardNotificationService cardNotificationService,
            INotesService notesService,
            ICheckListProcessService checkListProcessService,
            ICustomerDecisionProcessService customerDecisionProcessService,
            ICustomerFeedbackProcessService customerFeedbackProcessService,
            IProposalDocumentProcessService proposalStatusProcessService,
            INewOpportunityProcessService newOpportunityProcessService,
            IDashboardService dashboardService,
            IAuthorizationService authorizationService,
            IPermissionRepository permissionRepository,
            IStartProcessService startProcessService,
            IDashboardAnalysis dashboardAnalysis,
            GraphTeamsAppService graphTeamsAppService,
            IAddInHelper addInHelper,
            GraphTeamsOnBehalfService graphTeamsOnBeahalfService,
            IMemberService memberService,
            ITeamChannelService teamChannelService,
            IAzureKeyVaultService azureKeyVaultService) : base(logger, appOptions)
		{
			Guard.Against.Null(graphSharePointAppService, nameof(graphSharePointAppService));
			Guard.Against.Null(graphUserAppService, nameof(graphUserAppService));
            Guard.Against.Null(cardNotificationService, nameof(cardNotificationService));
            Guard.Against.Null(notesService, nameof(notesService));
            Guard.Against.Null(checkListProcessService, nameof(checkListProcessService));
            Guard.Against.Null(customerDecisionProcessService, nameof(customerDecisionProcessService));
            Guard.Against.Null(customerFeedbackProcessService, nameof(customerFeedbackProcessService));
            Guard.Against.Null(proposalStatusProcessService, nameof(proposalStatusProcessService));
            Guard.Against.Null(newOpportunityProcessService, nameof(newOpportunityProcessService));
            Guard.Against.Null(startProcessService, nameof(startProcessService));
            Guard.Against.Null(dashboardService, nameof(dashboardService));
            Guard.Against.Null(dashboardAnalysis, nameof(dashboardAnalysis));
            Guard.Against.Null(authorizationService, nameof(authorizationService));
            Guard.Against.Null(permissionRepository, nameof(permissionRepository));
            Guard.Against.Null(graphTeamsAppService, nameof(graphTeamsAppService));
            Guard.Against.Null(addInHelper, nameof(addInHelper));

            _graphSharePointAppService = graphSharePointAppService;
			_graphUserAppService = graphUserAppService;

            _cardNotificationService = cardNotificationService;
            _checkListProcessService = checkListProcessService;
            _customerDecisionProcessService = customerDecisionProcessService;
            _customerFeedbackProcessService = customerFeedbackProcessService;
            _proposalStatusProcessService = proposalStatusProcessService;
            _newOpportunityProcessService = newOpportunityProcessService;
            _startProcessService = startProcessService;
            _dashboardService = dashboardService;
            _authorizationService = authorizationService;
            _permissionRepository = permissionRepository;
            _graphTeamsAppService = graphTeamsAppService;
            _dashboardAnalysis = dashboardAnalysis;
            _addInHelper = addInHelper;
            _graphTeamsOnBehalfService = graphTeamsOnBeahalfService;
            _azureKeyVaultService = azureKeyVaultService;

            _memberService = memberService;
            _teamChannelService = teamChannelService;
            _notesService = notesService;
        }

        public async Task<Opportunity> CreateWorkflowAsync(Opportunity opportunity, string requestId = "")
		{
			try
			{
                _logger.LogError($"RequestId: {requestId} - Opportunityfactory_CreateWorkflowAsync CheckAccess CreateItemAsync");

                // Set initial opportunity state
                opportunity.Metadata.OpportunityState = OpportunityState.Creating;

                if (opportunity.Content.Template != null)
                {
                    if (opportunity.Content.Template.ProcessList != null)
                    {

                        if (opportunity.Content.Template.ProcessList.Count() > 1)
                        {
                            if (!opportunity.TemplateLoaded)
                            {
                                opportunity = await _teamChannelService.CreateWorkflowAsync(opportunity, requestId);
 
                                opportunity.Metadata.OpportunityState = OpportunityState.InProgress;
                            }
                            opportunity = await _memberService.CreateWorkflowAsync(opportunity, requestId);
                        }

                        bool checklistPass = false;
                        foreach (var item in opportunity.Content.Template.ProcessList)
                        {
                            if (item.ProcessType.ToLower() == "checklisttab" && checklistPass == false)
                            {
                                opportunity = await _checkListProcessService.CreateWorkflowAsync(opportunity, requestId);
                                checklistPass = true;
                            }
                            else if (item.ProcessType.ToLower() == "customerdecisiontab")
                            {
                                opportunity = await _customerDecisionProcessService.CreateWorkflowAsync(opportunity, requestId);
                            }
                            else if (item.ProcessType.ToLower() == "customerfeedbacktab")
                            {
                                opportunity = await _customerFeedbackProcessService.CreateWorkflowAsync(opportunity, requestId);
                            }
                            else if (item.ProcessType.ToLower() == "proposalstatustab")
                            {
                                opportunity = await _proposalStatusProcessService.CreateWorkflowAsync(opportunity, requestId);
                            }
                            else if (item.ProcessStep.ToLower() == "start process")
                            {
                                opportunity = await _startProcessService.CreateWorkflowAsync(opportunity, requestId);
                            }
                        }
                    }
                }
                else
                {
                    _logger.LogError($"RequestId: {requestId} - CreateWorkflowAsync Service Exception");
                    throw new AccessDeniedException($"RequestId: {requestId} - CreateWorkflowAsync Service Exception");
                }

                try
                {
                    opportunity = await _notesService.CreateWorkflowAsync(opportunity, requestId);
                    opportunity = await _dashboardService.CreateWorkflowAsync(opportunity, requestId);
                }
                catch
                {
                    _logger.LogError($"RequestId: {requestId} - CreateWorkflowAsync Service Exception");
                }

                return opportunity;
			}
			catch (Exception ex)
			{
				_logger.LogError($"RequestId: {requestId} - CreateWorkflowAsync Service Exception: {ex}");
				throw new ResponseException($"RequestId: {requestId} - CreateWorkflowAsync Service Exception: {ex}");
			}
		}

        public async Task<Opportunity> UpdateWorkflowAsync(Opportunity opportunity, string requestId = "")
		{

            try
			{


                if (opportunity.Content.Template != null)
                {
                    if (opportunity.Content.Template.ProcessList != null)
                    {
                        if (opportunity.Content.Template.ProcessList.Count() > 1)
                        {

                            if (!opportunity.TemplateLoaded)
                            {
                                opportunity = await _teamChannelService.CreateWorkflowAsync(opportunity, requestId);
                                opportunity.Metadata.OpportunityState = OpportunityState.InProgress;
                            }
                            opportunity = await _memberService.CreateWorkflowAsync(opportunity, requestId);
                        }

                        bool checklistPass = false;
                        foreach (var item in opportunity.Content.Template.ProcessList)
                        {
                            if (item.ProcessType.ToLower() == "checklisttab" && checklistPass == false)
                            {
                                opportunity = await _checkListProcessService.UpdateWorkflowAsync(opportunity, requestId);
                                checklistPass = true;
                            }
                            else if (item.ProcessType.ToLower() == "customerdecisiontab")
                            {
                                opportunity = await _customerDecisionProcessService.UpdateWorkflowAsync(opportunity, requestId);
                            }
                            else if (item.ProcessType.ToLower() == "customerfeedbacktab")
                            {
                                opportunity = await _customerFeedbackProcessService.UpdateWorkflowAsync(opportunity, requestId);
                            }
                            else if (item.ProcessType.ToLower() == "proposalstatustab")
                            {
                                opportunity = await _proposalStatusProcessService.UpdateWorkflowAsync(opportunity, requestId);
                            }
                            else if (item.ProcessStep.ToLower() == "start process")
                            {
                                opportunity = await _startProcessService.UpdateWorkflowAsync(opportunity, requestId);
                            }
                        }


                    }
                }
                else
                {
                    _logger.LogError($"RequestId: {requestId} - CreateWorkflowAsync Service Exception");
                    throw new AccessDeniedException($"RequestId: {requestId} - CreateWorkflowAsync Service Exception");
                }


                try
                {
                    opportunity = await MoveTempFileToTeamAsync(opportunity, requestId);
                }
                catch (Exception ex)
                {
                    _logger.LogError($"RequestId: {requestId} - UpdateWorkflowAsync_MoveTempFileToTeam Service Exception: {ex}");
                }

                try
                {
                    opportunity = await _dashboardService.UpdateWorkflowAsync(opportunity, requestId);
                }
                catch(Exception ex)
                {
                    _logger.LogError($"RequestId: {requestId} - UpdateWorkflowAsync_dashboardservice Service Exception: {ex}");

                }
    
                var initialState = opportunity.Metadata.OpportunityState;
                _logger.LogInformation($"RequestId: {requestId} - UpdateWorkflowAsync initialState: {initialState.Name} - {opportunity.Metadata.OpportunityState.Name}");
                if (initialState.Value != opportunity.Metadata.OpportunityState.Value)
                {
                    try
                    {
                        _logger.LogInformation($"RequestId: {requestId} - CreateWorkflowAsync sendNotificationCardAsync opportunity state change notification.");
                        var sendTo = UserProfile.Empty;
                        var sendNotificationCard = await _cardNotificationService.sendNotificationCardAsync(opportunity, sendTo, $"Opportunity state for {opportunity.DisplayName} has been changed to {opportunity.Metadata.OpportunityState.Name}", requestId);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"RequestId: {requestId} - CreateWorkflowAsync sendNotificationCardAsync OpportunityState error: {ex}");
                    }
                }

                return opportunity;
			}
			catch (Exception ex)
			{
				_logger.LogError($"RequestId: {requestId} - UpdateWorkflowAsync Service Exception: {ex}");
				throw new ResponseException($"RequestId: {requestId} - UpdateWorkflowAsync Service Exception: {ex}");
			}
		}

        

        // Workflow Actions
        public async Task<Opportunity> MoveTempFileToTeamAsync(Opportunity opportunity, string requestId = "")
		{
			try
			{

				// Find entries that need to be moved
				var moveFiles = false;
				foreach(var itm in opportunity.DocumentAttachments)
				{
					if (itm.DocumentUri == "TempFolder") moveFiles = true;
				}

				if (moveFiles)
				{
					var fromSiteId = _appOptions.ProposalManagementRootSiteId;
					var toSiteId = String.Empty;
					var fromItemPath = String.Empty;
					var toItemPath = String.Empty;

					string pattern = @"[ `~!@#$%^&*()_|+\-=?;:'" + '"' + @",.<>\{\}\[\]\\\/]";
					string replacement = "";

					Regex regEx = new Regex(pattern);
					var path = regEx.Replace(opportunity.DisplayName, replacement);
					//var path = WebUtility.UrlEncode(opportunity.DisplayName);
					//var path = opportunity.DisplayName.Replace(" ", "");

					var siteIdResponse = await _graphSharePointAppService.GetSiteIdAsync(_appOptions.SharePointHostName, path, requestId);
					dynamic responseDyn = siteIdResponse;
					toSiteId = responseDyn.id.ToString();

					if (!String.IsNullOrEmpty(toSiteId))
					{
						var updatedDocumentAttachments = new List<DocumentAttachment>();
						foreach (var itm in opportunity.DocumentAttachments)
						{
							var updDoc = DocumentAttachment.Empty;
							if (itm.DocumentUri == "TempFolder")
							{
								fromItemPath = $"TempFolder/{opportunity.DisplayName}/{itm.FileName}";
								toItemPath = $"General/{itm.FileName}";

								var resp = new JObject();
								try
								{
									resp = await _graphSharePointAppService.MoveFileAsync(fromSiteId, fromItemPath, toSiteId, toItemPath, requestId);
									updDoc.Id = new Guid().ToString();
									updDoc.DocumentUri = String.Empty;
									//doc.Id = resp.id;
								}
								catch (Exception ex)
								{
									_logger.LogWarning($"RequestId: {requestId} - MoveTempFileToTeam: from: {fromItemPath} to: {toItemPath} Service Exception: {ex}");
								}
							}

							updDoc.FileName = itm.FileName;
							updDoc.Note = itm.Note ?? String.Empty;
							updDoc.Tags = itm.Tags ?? String.Empty;
							updDoc.Category = Category.Empty;
							updDoc.Category.Id = itm.Category.Id;
							updDoc.Category.Name = itm.Category.Name;

							updatedDocumentAttachments.Add(updDoc);
						}

						opportunity.DocumentAttachments = updatedDocumentAttachments;

						// Delete temp files
						var result = await _graphSharePointAppService.DeleteFileOrFolderAsync(_appOptions.ProposalManagementRootSiteId, $"TempFolder/{opportunity.DisplayName}", requestId);

					}
				}

				return opportunity;
			}
			catch (Exception ex)
			{
				_logger.LogError($"RequestId: {requestId} - MoveTempFileToTeam Service Exception: {ex}");
				throw new ResponseException($"RequestId: {requestId} - MoveTempFileToTeam Service Exception: {ex}");
			}
		}
	}
}
