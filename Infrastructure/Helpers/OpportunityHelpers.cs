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
using ApplicationCore.Entities;
using ApplicationCore.Services;
using ApplicationCore.Helpers;
using ApplicationCore.Helpers.Exceptions;
using ApplicationCore.Models;
using Infrastructure.DealTypeServices;
using Infrastructure.Authorization;
using ApplicationCore.Authorization;

namespace Infrastructure.Helpers
{
    public class OpportunityHelpers
    {
        protected readonly ILogger _logger;
        protected readonly AppOptions _appOptions;
        private readonly UserProfileHelpers _userProfileHelpers;
        private readonly IRoleMappingRepository _roleMappingRepository;
        private readonly CardNotificationService _cardNotificationService;
        private readonly TemplateHelpers _templateHelpers;
        private readonly ICheckListProcessService _checkListProcessService;
        private readonly ICustomerDecisionProcessService _customerDecisionProcessService;
        private readonly IProposalDocumentProcessService _proposalStatusProcessService;
        private readonly IAuthorizationService _authorizationService;
        private readonly IPermissionRepository _permissionRepository;
        private readonly IUserContext _userContext;

        /// <summary>
        /// Constructor
        /// </summary>
        public OpportunityHelpers(
            ILogger<OpportunityHelpers> logger,
            IOptions<AppOptions> appOptions,
            UserProfileHelpers userProfileHelpers,
            IRoleMappingRepository roleMappingRepository,
            CardNotificationService cardNotificationService,
            TemplateHelpers templateHelpers,
            ICheckListProcessService checkListProcessService,
            ICustomerDecisionProcessService customerDecisionProcessService,
            IAuthorizationService authorizationService,
            IPermissionRepository permissionRepository,
            IUserContext userContext,
            IProposalDocumentProcessService proposalStatusProcessService)
        {
            Guard.Against.Null(logger, nameof(logger));
            Guard.Against.Null(appOptions, nameof(appOptions));
            Guard.Against.Null(userProfileHelpers, nameof(userProfileHelpers));
            Guard.Against.Null(roleMappingRepository, nameof(roleMappingRepository));
            Guard.Against.Null(cardNotificationService, nameof(cardNotificationService));
            Guard.Against.Null(templateHelpers, nameof(templateHelpers));
            Guard.Against.Null(checkListProcessService, nameof(checkListProcessService));
            Guard.Against.Null(customerDecisionProcessService, nameof(customerDecisionProcessService));
            Guard.Against.Null(proposalStatusProcessService, nameof(proposalStatusProcessService));
            Guard.Against.Null(authorizationService, nameof(authorizationService));
            Guard.Against.Null(permissionRepository, nameof(permissionRepository));

            _logger = logger;
            _appOptions = appOptions.Value;
            _userProfileHelpers = userProfileHelpers;
            _roleMappingRepository = roleMappingRepository;
            _cardNotificationService = cardNotificationService;
            _templateHelpers = templateHelpers;
            _checkListProcessService = checkListProcessService;
            _customerDecisionProcessService = customerDecisionProcessService;
            _proposalStatusProcessService = proposalStatusProcessService;
            _authorizationService = authorizationService;
            _permissionRepository = permissionRepository;
            _userContext = userContext;
        }

        public async Task<OpportunityViewModel> ToOpportunityViewModelAsync(Opportunity opportunity, string requestId = "")
        {
            return await OpportunityToViewModelAsync(opportunity, requestId);
        }

        public async Task<OpportunityViewModel> OpportunityToViewModelAsync(Opportunity entity, string requestId = "")
        {
            var oppId = entity.Id;
            try
            {
                //var entityDto = TinyMapper.Map<OpportunityViewModel>(entity);
                var viewModel = new OpportunityViewModel
                {
                    Id = entity.Id,
                    DisplayName = entity.DisplayName,
                    Reference = entity.Reference,
                    Version = entity.Version,
                    OpportunityState = OpportunityStateModel.FromValue(entity.Metadata.OpportunityState.Value),
                    DealSize = entity.Metadata.DealSize,
                    AnnualRevenue = entity.Metadata.AnnualRevenue,
                    OpenedDate = entity.Metadata.OpenedDate,
                    //For DashBoard
                    TargetDate = entity.Metadata.TargetDate,
                    Industry = new IndustryModel
                    {
                        Name = entity.Metadata.Industry.Name,
                        Id = entity.Metadata.Industry.Id
                    },
                    Region = new RegionModel
                    {
                        Name = entity.Metadata.Region.Name,
                        Id = entity.Metadata.Region.Id
                    },
                    Margin = entity.Metadata.Margin,
                    Rate = entity.Metadata.Rate,
                    DebtRatio = entity.Metadata.DebtRatio,
                    Purpose = entity.Metadata.Purpose,
                    DisbursementSchedule = entity.Metadata.DisbursementSchedule,
                    CollateralAmount = entity.Metadata.CollateralAmount,
                    Guarantees = entity.Metadata.Guarantees,
                    RiskRating = entity.Metadata.RiskRating,
                    OpportunityChannelId = entity.Metadata.OpportunityChannelId,
                    Customer = new CustomerModel
                    {
                        DisplayName = entity.Metadata.Customer.DisplayName,
                        Id = entity.Metadata.Customer.Id,
                        ReferenceId = entity.Metadata.Customer.ReferenceId
                    },
                    TeamMembers = new List<TeamMemberModel>(),
                    Notes = new List<NoteModel>(),
                    Checklists = new List<ChecklistModel>()
                };

                //DealType
                var dealTypeFlag = false;
                dealTypeFlag = entity.Content.DealType is null || entity.Content.DealType.Id is null;
                if (!dealTypeFlag)
                {
                    viewModel.DealType = await _templateHelpers.MapToViewModel(entity.Content.DealType);

                    //DealType Processes
                    var checklistPass = false;
                    foreach (var item in entity.Content.DealType.ProcessList)
                    {

                        if (item.ProcessType.ToLower() == "checklisttab" && checklistPass == false)
                        {
                            viewModel = await _checkListProcessService.MapToModelAsync(entity, viewModel, requestId);
                            checklistPass = true;
                        }
                        if (item.ProcessType.ToLower() == "customerdecisiontab")
                        {
                            viewModel = await _customerDecisionProcessService.MapToModelAsync(entity, viewModel, requestId);
                        }
                        if (item.ProcessType.ToLower() == "proposalstatustab")
                        {
                            viewModel = await _proposalStatusProcessService.MapToModelAsync(entity, viewModel, requestId);
                        }
                    }
                }

                

                // TeamMembers
                foreach (var item in entity.Content.TeamMembers.ToList())
                {
                    var memberModel = new TeamMemberModel();
                    memberModel.AssignedRole = await _userProfileHelpers.RoleToViewModelAsync(item.AssignedRole, requestId);
                    memberModel.Id = item.Id;
                    memberModel.DisplayName = item.DisplayName;
                    memberModel.Mail = item.Fields.Mail;
                    memberModel.UserPrincipalName = item.Fields.UserPrincipalName;
                    memberModel.Title = item.Fields.Title ?? String.Empty;
                    memberModel.ProcessStep = item.ProcessStep;

                    viewModel.TeamMembers.Add(memberModel);
                }

                // Notes
                foreach (var item in entity.Content.Notes.ToList())
                {
                    var note = new NoteModel();
                    note.Id = item.Id;

                    var userProfile = new UserProfileViewModel();
                    userProfile.Id = item.CreatedBy.Id;
                    userProfile.DisplayName = item.CreatedBy.DisplayName;
                    userProfile.Mail = item.CreatedBy.Fields.Mail;
                    userProfile.UserPrincipalName = item.CreatedBy.Fields.UserPrincipalName;
                    userProfile.UserRoles = await _userProfileHelpers.RolesToViewModelAsync(item.CreatedBy.Fields.UserRoles, requestId);

                    note.CreatedBy = userProfile;
                    note.NoteBody = item.NoteBody;
                    note.CreatedDateTime = item.CreatedDateTime;

                    viewModel.Notes.Add(note);
                }

                // DocumentAttachments
                viewModel.DocumentAttachments = new List<DocumentAttachmentModel>();
                if (entity.DocumentAttachments != null)
                {
                    foreach (var itm in entity.DocumentAttachments)
                    {
                        var doc = new DocumentAttachmentModel();
                        doc.Id = itm.Id ?? String.Empty;
                        doc.FileName = itm.FileName ?? String.Empty;
                        doc.Note = itm.Note ?? String.Empty;
                        doc.Tags = itm.Tags ?? String.Empty;
                        doc.Category = new CategoryModel();
                        doc.Category.Id = itm.Category.Id;
                        doc.Category.Name = itm.Category.Name;
                        doc.DocumentUri = itm.DocumentUri;

                        viewModel.DocumentAttachments.Add(doc);
                    }
                }

                return viewModel;
            }
            catch (Exception ex)
            {
                // TODO: _logger.LogError("MapToViewModelAsync error: " + ex);
                throw new ResponseException($"RequestId: {requestId} - OpportunityToViewModelAsync oppId: {oppId} - failed to map opportunity: {ex}");
            }
        }

        public async Task<Opportunity> ToOpportunityAsync(OpportunityViewModel model, Opportunity opportunity, string requestId = "")
        {
            return await OpportunityToEntityAsync(model, opportunity, requestId);
        }

        #region MAP: model -> entity
        private async Task<Opportunity> OpportunityToEntityAsync(OpportunityViewModel viewModel, Opportunity opportunity, string requestId = "")
        {
            var oppId = viewModel.Id;

            try
            {
                var entity = opportunity;                

                entity.Id = viewModel.Id ?? String.Empty;
                entity.DisplayName = viewModel.DisplayName ?? String.Empty;
                entity.Reference = viewModel.Reference ?? String.Empty;
                entity.Version = viewModel.Version ?? String.Empty;

                // DocumentAttachments
                if (entity.DocumentAttachments == null) entity.DocumentAttachments = new List<DocumentAttachment>();
                if (viewModel.DocumentAttachments != null)
                {
                    var newDocumentAttachments = new List<DocumentAttachment>();
                    foreach (var itm in viewModel.DocumentAttachments)
                    {
                        var doc = entity.DocumentAttachments.ToList().Find(x => x.Id == itm.Id);
                        if (doc == null)
                        {
                            doc = DocumentAttachment.Empty;
                        }

                        doc.Id = itm.Id;
                        doc.FileName = itm.FileName ?? String.Empty;
                        doc.DocumentUri = itm.DocumentUri ?? String.Empty;
                        doc.Category = Category.Empty;
                        doc.Category.Id = itm.Category.Id ?? String.Empty;
                        doc.Category.Name = itm.Category.Name ?? String.Empty;
                        doc.Tags = itm.Tags ?? String.Empty;
                        doc.Note = itm.Note ?? String.Empty;

                        newDocumentAttachments.Add(doc);
                    }

                    // TODO: P2 create logic for replace and support for other artifact types for now we replace the whole list
                    entity.DocumentAttachments = newDocumentAttachments;
                }

                // Content
                if (entity.Content == null) entity.Content = OpportunityContent.Empty;
                
                // Proposal Document
                if(viewModel.ProposalDocument != null)
                {
                    entity = await _proposalStatusProcessService.MapToEntityAsync(entity, viewModel, requestId);
                }

                //DealType
                if (viewModel.DealType != null)
                {
                    entity.Content.DealType = await _templateHelpers.MapToEntity(viewModel.DealType);
                    //DealType Processes
                    var checklistPass = false;
                    foreach (var item in viewModel.DealType.ProcessList)
                    {

                        if (item.ProcessType.ToLower() == "checklisttab" && checklistPass == false)
                        {
                            entity = await _checkListProcessService.MapToEntityAsync(entity, viewModel, requestId);
                            checklistPass = true;
                        }
                        if (item.ProcessType.ToLower() == "customerdecisiontab")
                        {
                            entity = await _customerDecisionProcessService.MapToEntityAsync(entity, viewModel, requestId);
                        }
                        if (item.ProcessType.ToLower() == "proposalstatustab")
                        {
                            entity = await _proposalStatusProcessService.MapToEntityAsync(entity, viewModel, requestId);
                        }
                    }
                }

                // LIST: Content/Notes
                if (entity.Content.Notes == null) entity.Content.Notes = new List<Note>();
                if (viewModel.Notes != null)
                {
                    var updatedNotes = entity.Content.Notes.ToList();
                    foreach (var item in viewModel.Notes)
                    {
                        var note = updatedNotes.Find(itm => itm.Id == item.Id);
                        if (note != null)
                        {
                            updatedNotes.Remove(note);
                        }
                        updatedNotes.Add(await NoteToEntityAsync(item, requestId));
                    }

                    entity.Content.Notes = updatedNotes;
                }

                //Granular Access Start
                //Team creation
                var permissionsNeeded = new List<ApplicationCore.Entities.Permission>();
                List<string> list = new List<string>();
                var access = true;
                //going for super access
                list.AddRange(new List<string> { Access.Opportunities_ReadWrite_All.ToString() });
                permissionsNeeded = (await _permissionRepository.GetAllAsync(requestId)).ToList().Where(x => list.Any(x.Name.Contains)).ToList();
                if (!(StatusCodes.Status200OK == await _authorizationService.CheckAccessAsync(permissionsNeeded,requestId)))
                {
                    //going for opportunity access
                    list.Clear();
                    list.AddRange(new List<string> { Access.Opportunity_ReadWrite_All.ToString(),Access.Opportunity_ReadWrite_Partial.ToString() });
                    permissionsNeeded = (await _permissionRepository.GetAllAsync(requestId)).ToList().Where(x => list.Any(x.Name.Contains)).ToList();
                    if (!(StatusCodes.Status200OK == await _authorizationService.CheckAccessAsync(permissionsNeeded,requestId)))
                    {
                        //going for partial accesss
                        list.Clear();
                        list.AddRange(new List<string> { "Opportunity_ReadWrite_Team" });
                        permissionsNeeded = (await _permissionRepository.GetAllAsync(requestId)).ToList().Where(x => list.Any(x.Name.ToLower().Contains)).ToList();
                        access = StatusCodes.Status200OK == await _authorizationService.CheckAccessAsync(permissionsNeeded, requestId) ? true : false;
                    }
                    else
                    {
                        var currentUser = (_userContext.User.Claims).ToList().Find(x => x.Type == "preferred_username")?.Value;
                        if (!(viewModel.TeamMembers).ToList().Any(teamMember => teamMember.UserPrincipalName == currentUser))  access = false;
                    }
                }
                if (access)
                {
                    // TeamMembers
                    if (entity.Content.TeamMembers == null) entity.Content.TeamMembers = new List<TeamMember>();
                    if (viewModel.TeamMembers != null)
                    {
                        var updatedTeamMembers = new List<TeamMember>();

                        // Update team members
                        foreach (var item in viewModel.TeamMembers)
                        {
                            updatedTeamMembers.Add(await TeamMemberToEntityAsync(item));
                        }
                        entity.Content.TeamMembers = updatedTeamMembers;
                    }
                }
                //Granular Access end

                // Metadata
                if (entity.Metadata == null) entity.Metadata = OpportunityMetadata.Empty;
                entity.Metadata.AnnualRevenue = viewModel.AnnualRevenue;
                entity.Metadata.CollateralAmount = viewModel.CollateralAmount;

                if (entity.Metadata.Customer == null) entity.Metadata.Customer = Customer.Empty;
                entity.Metadata.Customer.DisplayName = viewModel.Customer.DisplayName ?? String.Empty;
                entity.Metadata.Customer.Id = viewModel.Customer.Id ?? String.Empty;
                entity.Metadata.Customer.ReferenceId = viewModel.Customer.ReferenceId ?? String.Empty;

                entity.Metadata.DealSize = viewModel.DealSize;
                entity.Metadata.DebtRatio = viewModel.DebtRatio;
                entity.Metadata.DisbursementSchedule = viewModel.DisbursementSchedule ?? String.Empty;
                entity.Metadata.Guarantees = viewModel.Guarantees ?? String.Empty;

                if (entity.Metadata.Industry == null) entity.Metadata.Industry = new Industry();
                if (viewModel.Industry != null) entity.Metadata.Industry = await IndustryToEntityAsync(viewModel.Industry);

                entity.Metadata.Margin = viewModel.Margin;

                if (entity.Metadata.OpenedDate == null) entity.Metadata.OpenedDate = DateTimeOffset.MinValue;
                if (viewModel.OpenedDate != null) entity.Metadata.OpenedDate = viewModel.OpenedDate;

                //For Dashboard --TargetDate
                if (entity.Metadata.TargetDate == null) entity.Metadata.TargetDate = DateTimeOffset.MinValue;
                if (viewModel.TargetDate != null) entity.Metadata.TargetDate = viewModel.TargetDate;

                if (entity.Metadata.OpportunityState == null) entity.Metadata.OpportunityState = OpportunityState.Creating;
                if (viewModel.OpportunityState != null) entity.Metadata.OpportunityState = OpportunityState.FromValue(viewModel.OpportunityState.Value);

                entity.Metadata.Purpose = viewModel.Purpose ?? String.Empty;
                entity.Metadata.Rate = viewModel.Rate;

                if (entity.Metadata.Region == null) entity.Metadata.Region = Region.Empty;
                if (viewModel.Region != null) entity.Metadata.Region = await RegionToEntityAsync(viewModel.Region);

                entity.Metadata.RiskRating = viewModel.RiskRating;

                // if to avoid deleting channelId if vieModel passes empty and a value was already in opportunity
                if (!String.IsNullOrEmpty(viewModel.OpportunityChannelId)) entity.Metadata.OpportunityChannelId = viewModel.OpportunityChannelId;

                return entity;
            }
            catch (Exception ex)
            {
                //_logger.LogError("MapFromViewModelAsync error: " + ex);
                throw new ResponseException($"RequestId: {requestId} - OpportunityToEntityAsync oppId: {oppId} - failed to map opportunity: {ex}");
            }
        }

        private Task<Industry> IndustryToEntityAsync(IndustryModel model)
        {
            return Task.FromResult(new Industry
            {
                Name = model.Name,
                Id = model.Id
            });
        }

        private async Task<Note> NoteToEntityAsync(NoteModel model, string requestId = "")
        {
            var note = Note.Empty;

            if (model.CreatedBy != null) note.CreatedBy = await _userProfileHelpers.UserProfileToEntityAsync(model.CreatedBy, requestId);
            if (model.CreatedDateTime == null)
            {
                note.CreatedDateTime = DateTimeOffset.Now;
            }
            else
            {
                note.CreatedDateTime = model.CreatedDateTime;
            }

            note.Id = model.Id ?? new Guid().ToString();
            note.NoteBody = model.NoteBody ?? String.Empty;

            return note;
        }

        private async Task<TeamMember> TeamMemberToEntityAsync(TeamMemberModel model, string requestId = "")
        {
            var teamMember = TeamMember.Empty;
            teamMember.Id = model.Id;
            teamMember.DisplayName = model.DisplayName;
            teamMember.AssignedRole = await _userProfileHelpers.RoleModelToEntityAsync(model.AssignedRole, requestId);
            teamMember.Fields = TeamMemberFields.Empty;
            teamMember.Fields.Mail = model.Mail;
            teamMember.Fields.Title = model.Title;
            teamMember.Fields.UserPrincipalName = model.UserPrincipalName;
            if (teamMember.AssignedRole.DisplayName.ToLower() == "relationshipmanager")
                teamMember.ProcessStep = "New Opportunity";
            else
                teamMember.ProcessStep = model.ProcessStep;
            return teamMember;
        }

        private Task<Region> RegionToEntityAsync(RegionModel model)
        {
            return Task.FromResult(new Region
            {
                Name = model.Name,
                Id = model.Id
            });
        }
        #endregion
    }
}
