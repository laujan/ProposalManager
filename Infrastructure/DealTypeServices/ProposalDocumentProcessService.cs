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
using ApplicationCore.Helpers;
using ApplicationCore.Entities;
using ApplicationCore.Helpers.Exceptions;
using ApplicationCore.Services;
using ApplicationCore.Models;
using Infrastructure.Authorization;
using ApplicationCore.Authorization;

namespace Infrastructure.DealTypeServices
{
    public class ProposalDocumentProcessService : BaseService<ProposalDocumentProcessService>, IProposalDocumentProcessService
    {
        private readonly UserProfileHelpers _userProfileHelpers;
        private readonly CardNotificationService _cardNotificationService;
        private readonly IAuthorizationService _authorizationService;
        private readonly IPermissionRepository _permissionRepository;

        public ProposalDocumentProcessService(
        ILogger<ProposalDocumentProcessService> logger,
        IOptionsMonitor<AppOptions> appOptions,
        UserProfileHelpers userProfileHelpers,
        IAuthorizationService authorizationService,
        IPermissionRepository permissionRepository,
        CardNotificationService cardNotificationService) : base(logger, appOptions)
        {
            Guard.Against.Null(logger, nameof(logger));
            Guard.Against.Null(appOptions, nameof(appOptions));
            Guard.Against.Null(userProfileHelpers, nameof(userProfileHelpers));
            Guard.Against.Null(cardNotificationService, nameof(cardNotificationService));
            Guard.Against.Null(authorizationService, nameof(authorizationService));
            Guard.Against.Null(permissionRepository, nameof(permissionRepository));

            _userProfileHelpers = userProfileHelpers;
            _cardNotificationService = cardNotificationService;
            _authorizationService = authorizationService;
            _permissionRepository = permissionRepository;

        }

        public async Task<Opportunity> CreateWorkflowAsync(Opportunity opportunity, string requestId = "")
        {
            //Granular Access : Start
            try
            {
                //Granular Access : Start
                var permissionsNeeded = new List<ApplicationCore.Entities.Permission>();
                List<string> list = new List<string>();
                var access = true;
                //going for super access
                list.AddRange(new List<string> { Access.Opportunities_ReadWrite_All.ToString() });
                permissionsNeeded = (await _permissionRepository.GetAllAsync(requestId)).ToList().Where(x => list.Any(x.Name.Contains)).ToList();
                if (!(StatusCodes.Status200OK == await _authorizationService.CheckAccessAsync(permissionsNeeded,requestId)))
                {
                    //going for opportunity access
                    access = await _authorizationService.CheckAccessInOpportunityAsync(opportunity, PermissionNeededTo.Write, requestId);
                    if (!access)
                    {
                        access = await _authorizationService.CheckAccessInOpportunityAsync(opportunity, PermissionNeededTo.WritePartial, requestId);
                        //going for partial accesss
                        if (access)
                        {
                            list.Clear();
                            list.AddRange(new List<string> { "proposaldocument_readwrite" });
                            permissionsNeeded = (await _permissionRepository.GetAllAsync(requestId)).ToList().Where(x => list.Any(x.Name.Contains)).ToList();
                            access = StatusCodes.Status200OK == await _authorizationService.CheckAccessAsync(permissionsNeeded, requestId) ? true : false;
                        }
                        else
                            access = false;

                    }

                }

                Guard.Equals(true, access);

                return opportunity;
            }
            catch(Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - CreateItemAsync Service Exception: {ex}");
                //throw new AccessDeniedException($"RequestId: {requestId} - CreateItemAsync Service Exception: {ex}");
                return opportunity;
            }
            //Granular Access : End
        }

        public async Task<Opportunity> MapToEntityAsync(Opportunity entity, OpportunityViewModel viewModel, string requestId = "")
        {
            try
            {
                if (entity.Content.ProposalDocument == null) entity.Content.ProposalDocument = ProposalDocument.Empty;
                if (viewModel.ProposalDocument != null) entity.Content.ProposalDocument = await ProposalDocumentToEntityAsync(viewModel, entity.Content.ProposalDocument, requestId);
                return entity;
            }
            catch(Exception ex)
            {
                throw new ResponseException($"RequestId: {requestId} - ProposalStatusProcessService MapToEntity oppId: {entity.Id} - failed to map opportunity: {ex}");
            }
        }

        public async Task<OpportunityViewModel> MapToModelAsync(Opportunity entity, OpportunityViewModel viewModel, string requestId = "")
        {
            try
            {
                //Granular bug fix : Start
                //Temp fix for checklist process update
                //Overriding granular access while getting exsiting opportunity model from sharepoint
                var overrideAccess = _authorizationService.GetGranularAccessOverride();
                //Granular bug fix : End

                //Granular Access : Start
                var permissionsNeeded = new List<ApplicationCore.Entities.Permission>();
                List<string> list = new List<string>();
                var access = true;
                //going for super access
                list.AddRange(new List<string> { Access.Opportunities_Read_All.ToString() , Access.Opportunities_ReadWrite_All.ToString()});
                permissionsNeeded = (await _permissionRepository.GetAllAsync(requestId)).ToList().Where(x => list.Any(x.Name.Contains)).ToList();
                if (!(StatusCodes.Status200OK == await _authorizationService.CheckAccessAsync(permissionsNeeded,requestId)))
                {
                    //going for opportunity access
                    access = await _authorizationService.CheckAccessInOpportunityAsync(entity, PermissionNeededTo.Read, requestId);
                    if (!access)
                    {
                        access = await _authorizationService.CheckAccessInOpportunityAsync(entity, PermissionNeededTo.ReadPartial, requestId);
                        //going for partial accesss
                        if (access)
                        {
                            list.Clear();
                            list.AddRange(new List<string> { "proposaldocument_read", "proposaldocument_readwrite" });
                            permissionsNeeded = (await _permissionRepository.GetAllAsync(requestId)).ToList().Where(x => list.Any(x.Name.Contains)).ToList();
                            access = StatusCodes.Status200OK == await _authorizationService.CheckAccessAsync(permissionsNeeded, requestId) ? true : false;
                        }
                        else access = false;
                    }
                }

                if (access || overrideAccess)
                {
                    //Granular Access : End

                    viewModel.ProposalDocument = new ProposalDocumentModel();
                    viewModel.ProposalDocument.Id = entity.Content.ProposalDocument.Id;
                    viewModel.ProposalDocument.DisplayName = entity.Content.ProposalDocument.DisplayName;
                    viewModel.ProposalDocument.Reference = entity.Content.ProposalDocument.Reference;
                    viewModel.ProposalDocument.DocumentUri = entity.Content.ProposalDocument.Metadata.DocumentUri;
                    viewModel.ProposalDocument.Category = new CategoryModel();
                    viewModel.ProposalDocument.Category.Id = entity.Content.ProposalDocument.Metadata.Category.Id;
                    viewModel.ProposalDocument.Category.Name = entity.Content.ProposalDocument.Metadata.Category.Name;
                    viewModel.ProposalDocument.Content = new ProposalDocumentContentModel();
                    viewModel.ProposalDocument.Content.ProposalSectionList = new List<DocumentSectionModel>();
                    viewModel.ProposalDocument.Notes = new List<NoteModel>();
                    viewModel.ProposalDocument.Tags = entity.Content.ProposalDocument.Metadata.Tags;
                    viewModel.ProposalDocument.Version = entity.Content.ProposalDocument.Version;

                    // ProposalDocument Notes
                    foreach (var item in entity.Content.ProposalDocument.Metadata.Notes.ToList())
                    {
                        var docNote = new NoteModel();

                        docNote.Id = item.Id;
                        docNote.CreatedDateTime = item.CreatedDateTime;
                        docNote.NoteBody = item.NoteBody;
                        docNote.CreatedBy = new UserProfileViewModel
                        {
                            Id = item.CreatedBy.Id,
                            DisplayName = item.CreatedBy.DisplayName,
                            Mail = item.CreatedBy.Fields.Mail,
                            UserPrincipalName = item.CreatedBy.Fields.UserPrincipalName,
                            UserRoles = await _userProfileHelpers.RolesToViewModelAsync(item.CreatedBy.Fields.UserRoles, requestId)
                        };

                        viewModel.ProposalDocument.Notes.Add(docNote);
                    }


                    // ProposalDocument ProposalSectionList
                    foreach (var item in entity.Content.ProposalDocument.Content.ProposalSectionList.ToList())
                    {
                        if (!String.IsNullOrEmpty(item.Id))
                        {
                            var docSectionModel = new DocumentSectionModel();
                            docSectionModel.Id = item.Id;
                            docSectionModel.DisplayName = item.DisplayName;
                            docSectionModel.LastModifiedDateTime = item.LastModifiedDateTime;
                            docSectionModel.Owner = new UserProfileViewModel();
                            if (item.Owner != null)
                            {
                                docSectionModel.Owner.Id = item.Owner.Id ?? String.Empty;
                                docSectionModel.Owner.DisplayName = item.Owner.DisplayName ?? String.Empty;
                                if (item.Owner.Fields != null)
                                {
                                    docSectionModel.Owner.Mail = item.Owner.Fields.Mail ?? String.Empty;
                                    docSectionModel.Owner.UserPrincipalName = item.Owner.Fields.UserPrincipalName ?? String.Empty;
                                    docSectionModel.Owner.UserRoles = new List<RoleModel>();

                                    if (item.Owner.Fields.UserRoles != null)
                                    {
                                        docSectionModel.Owner.UserRoles = await _userProfileHelpers.RolesToViewModelAsync(item.Owner.Fields.UserRoles, requestId);
                                    }
                                }
                                else
                                {
                                    docSectionModel.Owner.Mail = String.Empty;
                                    docSectionModel.Owner.UserPrincipalName = String.Empty;
                                    docSectionModel.Owner.UserRoles = new List<RoleModel>();

                                    if (item.Owner.Fields.UserRoles != null)
                                    {
                                        docSectionModel.Owner.UserRoles = await _userProfileHelpers.RolesToViewModelAsync(item.Owner.Fields.UserRoles, requestId);
                                    }
                                }
                            }

                            docSectionModel.SectionStatus = item.SectionStatus;
                            docSectionModel.SubSectionId = item.SubSectionId;
                            docSectionModel.AssignedTo = new UserProfileViewModel();
                            if (item.AssignedTo != null)
                            {
                                docSectionModel.AssignedTo.Id = item.AssignedTo.Id;
                                docSectionModel.AssignedTo.DisplayName = item.AssignedTo.DisplayName;
                                docSectionModel.AssignedTo.Mail = item.AssignedTo.Fields.Mail;
                                docSectionModel.AssignedTo.Title = item.AssignedTo.Fields.Title;
                                docSectionModel.AssignedTo.UserPrincipalName = item.AssignedTo.Fields.UserPrincipalName;
                                // TODO: Not including role info since it is not relevant but if needed it needs to be set here
                            }
                            docSectionModel.Task = item.Task;

                            viewModel.ProposalDocument.Content.ProposalSectionList.Add(docSectionModel);
                        }
                    }
                }
                return viewModel;
            }
            catch (Exception ex)
            {
                throw new ResponseException($"RequestId: {requestId} - ProposalStatusProcessService MapToViewModel oppId: {entity.Id} - failed to map opportunity: {ex}");
            }

        }

        public async Task<Opportunity> UpdateWorkflowAsync(Opportunity opportunity, string requestId = "")
        {
            //Granular Access : Start
            try
            {

                //Granular Access : Start
                var permissionsNeeded = new List<ApplicationCore.Entities.Permission>();
                List<string> list = new List<string>();
                var access = true;
                //going for super access
                list.AddRange(new List<string> { Access.Opportunities_ReadWrite_All.ToString() });
                permissionsNeeded = (await _permissionRepository.GetAllAsync(requestId)).ToList().Where(x => list.Any(x.Name.Contains)).ToList();
                if (!(StatusCodes.Status200OK == await _authorizationService.CheckAccessAsync(permissionsNeeded,requestId)))
                {
                    //going for opportunity access
                    access = await _authorizationService.CheckAccessInOpportunityAsync(opportunity, PermissionNeededTo.Write, requestId);
                    if (!access)
                    {
                        access = await _authorizationService.CheckAccessInOpportunityAsync(opportunity, PermissionNeededTo.WritePartial, requestId);
                        //going for partial accesss
                        if (access)
                        {
                            list.Clear();
                            list.AddRange(new List<string> { "proposaldocument_readwrite" });
                            permissionsNeeded = (await _permissionRepository.GetAllAsync(requestId)).ToList().Where(x => list.Any(x.Name.Contains)).ToList();
                            access = StatusCodes.Status200OK == await _authorizationService.CheckAccessAsync(permissionsNeeded, requestId) ? true : false;
                        }
                        else
                            access = false;
                    }
                }

                Guard.Equals(true,access);

                return opportunity;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - UpdateWorkflowAsync Service Exception: {ex}");
                //throw new AccessDeniedException($"RequestId: {requestId} - UpdateWorkflowAsync Service Exception: {ex}");
                return opportunity;
            }
            //Granular Access : End
        }
        private async Task<ProposalDocument> ProposalDocumentToEntityAsync(OpportunityViewModel viewModel, ProposalDocument proposalDocument, string requestId = "")
        {
            try
            {
                Guard.Against.Null(proposalDocument, "ProposalDocumentToEntityAsync", requestId);
                var entity = proposalDocument;
                var model = viewModel.ProposalDocument;

                entity.Id = model.Id ?? String.Empty;
                entity.DisplayName = model.DisplayName ?? String.Empty;
                entity.Reference = model.Reference ?? String.Empty;
                entity.Version = model.Version ?? String.Empty;

                if (entity.Content == null)
                {
                    entity.Content = ProposalDocumentContent.Empty;
                }


                // Storing previous section lists to compare and trigger notification if assigment changes
                var currProposalSectionList = entity.Content.ProposalSectionList.ToList();

                // Proposal sections are always overwritten
                entity.Content.ProposalSectionList = new List<DocumentSection>();

                if (model.Content.ProposalSectionList != null)
                {
                    var OwnerSendList = new List<UserProfile>(); //receipients list for notifications
                    var AssignedToSendList = new List<UserProfile>(); //receipients list for notifications
                    // LIST: ProposalSectionList
                    foreach (var item in model.Content.ProposalSectionList)
                    {
                        var documentSection = new DocumentSection();
                        documentSection.DisplayName = item.DisplayName ?? String.Empty;
                        documentSection.Id = item.Id ?? String.Empty;
                        documentSection.LastModifiedDateTime = item.LastModifiedDateTime;
                        documentSection.Owner = await _userProfileHelpers.UserProfileToEntityAsync(item.Owner ?? new UserProfileViewModel(), requestId);
                        documentSection.SectionStatus = ActionStatus.FromValue(item.SectionStatus.Value);
                        documentSection.SubSectionId = item.SubSectionId ?? String.Empty;
                        documentSection.AssignedTo = await _userProfileHelpers.UserProfileToEntityAsync(item.AssignedTo ?? new UserProfileViewModel(), requestId);
                        documentSection.Task = item.Task ?? String.Empty;

                        // Check values to see if notification trigger is needed
                        var prevSectionList = currProposalSectionList.Find(x => x.Id == documentSection.Id);
                        if (prevSectionList != null)
                        {
                            if (prevSectionList.Owner.Id != documentSection.Owner.Id)
                            {
                                OwnerSendList.Add(documentSection.Owner);
                            }

                            if (prevSectionList.AssignedTo.Id != documentSection.AssignedTo.Id)
                            {
                                AssignedToSendList.Add(documentSection.AssignedTo);
                            }
                        }

                        entity.Content.ProposalSectionList.Add(documentSection);
                    }

                    // AssignedToSendList notifications
                    // Section owner changed / assigned
                    try
                    {
                        if (OwnerSendList.Count > 0)
                        {
                            _logger.LogInformation($"RequestId: {requestId} - ProposalDocumentToEntityAsync sendNotificationCardAsync for owner changed notification.");
                            var notificationOwner = await _cardNotificationService.sendNotificationCardAsync(
                                viewModel.DisplayName,
                                viewModel.OpportunityChannelId,
                                OwnerSendList,
                                $"Section(s) in the proposal document for opportunity {viewModel.DisplayName} has new/updated owners ",
                                requestId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"RequestId: {requestId} - ProposalDocumentToEntityAsync sendNotificationCardAsync for owner error: {ex}");
                    }

                    // Section AssignedTo changed / assigned
                    try
                    {
                        if (AssignedToSendList.Count > 0)
                        {
                            _logger.LogInformation($"RequestId: {requestId} - ProposalDocumentToEntityAsync sendNotificationCardAsync for AssigedTo changed notification.");
                            var notificationAssignedTo = await _cardNotificationService.sendNotificationCardAsync(
                                viewModel.DisplayName,
                                viewModel.OpportunityChannelId,
                                AssignedToSendList,
                                $"Task(s) in the proposal document for opportunity {viewModel.DisplayName} has new/updated assigments ",
                                requestId);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"RequestId: {requestId} - ProposalDocumentToEntityAsync sendNotificationCardAsync for AssignedTo error: {ex}");
                    }
                }


                // Metadata
                if (entity.Metadata == null)
                {
                    entity.Metadata = DocumentMetadata.Empty;
                }

                entity.Metadata.DocumentUri = model.DocumentUri;
                entity.Metadata.Tags = model.Tags;
                if (entity.Metadata.Category == null)
                {
                    entity.Metadata.Category = new Category();
                }

                entity.Metadata.Category.Id = model.Category.Id ?? String.Empty;
                entity.Metadata.Category.Name = model.Category.Name ?? String.Empty;

                if (entity.Metadata.Notes == null)
                {
                    entity.Metadata.Notes = new List<Note>();
                }

                if (model.Notes != null)
                {
                    // LIST: Notes
                    foreach (var item in model.Notes)
                    {
                        entity.Metadata.Notes.Add(await NoteToEntityAsync(item, requestId));
                    }
                }

                return entity;
            }
            catch (Exception ex)
            {
                //_logger.LogError(ex.Message);
                throw ex;
            }
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

    }
}
