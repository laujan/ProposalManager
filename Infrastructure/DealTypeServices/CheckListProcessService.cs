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
    public class CheckListProcessService : BaseService<CheckListProcessService>, IDealTypeService
    {
        private readonly CardNotificationService _cardNotificationService;
        private readonly IAuthorizationService _authorizationService;
        private readonly IPermissionRepository _permissionRepository;

        public CheckListProcessService(
            ILogger<CheckListProcessService> logger,
            IOptionsMonitor<AppOptions> appOptions,
            IAuthorizationService authorizationService,
            IPermissionRepository permissionRepository,
            CardNotificationService cardNotificationService) : base(logger, appOptions)
        {
            Guard.Against.Null(logger, nameof(logger));
            Guard.Against.Null(appOptions, nameof(appOptions));
            Guard.Against.Null(cardNotificationService, nameof(cardNotificationService));
            Guard.Against.Null(authorizationService, nameof(authorizationService));
            Guard.Against.Null(permissionRepository, nameof(permissionRepository));

            _cardNotificationService = cardNotificationService;
            _authorizationService = authorizationService;
            _permissionRepository = permissionRepository;
        }

        public async Task<Opportunity> CreateWorkflowAsync(Opportunity opportunity, string requestId = "")
        {
            return await UpdateCheckList(opportunity, requestId);
        }
        public async Task<Opportunity> MapToEntityAsync(Opportunity entity, OpportunityViewModel viewModel, string requestId = "")
        {
            try
            {
                var entityIsEmpty = true;
                if (!String.IsNullOrEmpty(entity.DisplayName)) entityIsEmpty = false; // If empty we should not send any notifications since it is just a reference opportunity schema

                if (entity.Content.Checklists == null) entity.Content.Checklists = new List<Checklist>();
                if (viewModel.Checklists != null)
                {
                    // List of checklists that status changed thus team members need to be sent with a notification
                    var statusChangedChecklists = new List<Checklist>();

                    var updatedList = new List<Checklist>();
                    // LIST: Content/CheckList/ChecklistTaskList
                    foreach (var item in viewModel.Checklists)
                    {
                        var checklist = Checklist.Empty;
                        var existinglist = entity.Content.Checklists.ToList().Find(x => x.Id == item.Id);
                        if (existinglist != null) checklist = existinglist;

                        var addToChangedList = false;
                        if (checklist.ChecklistStatus.Value != item.ChecklistStatus.Value)
                        {
                            addToChangedList = true;
                        }

                        checklist.Id = item.Id ?? String.Empty;
                        checklist.ChecklistStatus = ActionStatus.FromValue(item.ChecklistStatus.Value);
                        checklist.ChecklistTaskList = new List<ChecklistTask>();
                        checklist.ChecklistChannel = item.ChecklistChannel ?? String.Empty;

                        foreach (var subitem in item.ChecklistTaskList)
                        {
                            var checklistTask = new ChecklistTask
                            {
                                Id = subitem.Id ?? String.Empty,
                                ChecklistItem = subitem.ChecklistItem ?? String.Empty,
                                Completed = subitem.Completed,
                                FileUri = subitem.FileUri ?? String.Empty
                            };
                            checklist.ChecklistTaskList.Add(checklistTask);
                        }

                        // Add checklist for notifications, notification is sent below during teamMembers iterations
                        if (addToChangedList)
                        {
                            statusChangedChecklists.Add(checklist);
                        }

                        updatedList.Add(checklist);
                    }

                    // Send notifications for changed checklists
                    if (statusChangedChecklists.Count > 0 && !entityIsEmpty)
                    {
                        try
                        {
                            if (statusChangedChecklists.Count > 0)
                            {
                                var checkLists = String.Empty;
                                foreach (var chkItm in statusChangedChecklists)
                                {
                                    checkLists = checkLists + $"'{chkItm.ChecklistChannel}' ";
                                }

                                var sendToList = new List<UserProfile>();
                                if (!String.IsNullOrEmpty(viewModel.OpportunityChannelId)) entity.Metadata.OpportunityChannelId = viewModel.OpportunityChannelId;

                                _logger.LogInformation($"RequestId: {requestId} - UpdateWorkflowAsync sendNotificationCardAsync checklist status changed notification. Number of hecklists: {statusChangedChecklists.Count}");
                                var sendNotificationCard = await _cardNotificationService.sendNotificationCardAsync(entity, sendToList, $"Status updated for opportunity checklist(s): {checkLists} ", requestId);
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError($"RequestId: {requestId} - UpdateWorkflowAsync sendNotificationCardAsync checklist status change error: {ex}");
                        }
                        //Granular bug fix: end
                    }

                    try
                    {
                        if (entity.Content.Checklists.Count > 0 && updatedList.Count > 0)
                        {
                            var items = entity.Content.Checklists.Where(x => !updatedList.Any(y => y.ChecklistChannel == x.ChecklistChannel));
                            foreach (var item in items)
                            {
                                updatedList.Add(item);
                            }
                        }

                    }
                    catch (Exception ex)
                    {
                        _logger.LogError($"RequestId: {requestId} - UpdateWorkflowAsync Bug fix checklist error: {ex}");
                    }
                    //Granular bug fix: start

                    entity.Content.Checklists = updatedList;
                }

                if (entity.Content.Checklists.Count == 0)
                {
                    // Checklist empty create a default set
                    foreach (var item in viewModel.DealType.ProcessList)
                    {
                        if (item.ProcessType.ToLower() == "checklisttab")
                        {
                            var checklist = new Checklist
                            {
                                Id = item.ProcessStep,
                                ChecklistChannel = item.Channel,
                                ChecklistStatus = ActionStatus.NotStarted,
                                ChecklistTaskList = new List<ChecklistTask>()
                            };
                            entity.Content.Checklists.Add(checklist);
                        }
                    }
                }

                return entity;
            }
            catch(Exception ex)
            {
                throw new ResponseException($"RequestId: {requestId} - CheckListProcessService MapToEntity oppId: {entity.Id} - failed to map opportunity: {ex}");
            }
        }

        public async Task<OpportunityViewModel> MapToModelAsync(Opportunity entity, OpportunityViewModel viewModel, string requestId = "")
        {
            try
            {
                foreach (var item in entity.Content.Checklists)
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
                    list.AddRange(new List<string> { Access.Opportunities_Read_All.ToString(), Access.Opportunities_ReadWrite_All.ToString() });
                    permissionsNeeded = (await _permissionRepository.GetAllAsync(requestId)).ToList().Where(x => list.Any(x.Name.Contains)).ToList();
                    if (!(StatusCodes.Status200OK == await _authorizationService.CheckAccessAsync(permissionsNeeded,requestId)))
                    {
                        //going for opportunity access
                        access = await _authorizationService.CheckAccessInOpportunityAsync(entity, PermissionNeededTo.Read, requestId);
                        if (!access)
                        {
                            //going for partial accesss
                            access = await _authorizationService.CheckAccessInOpportunityAsync(entity, PermissionNeededTo.ReadPartial, requestId);
                            if (access)
                            {
                                var channel = item.ChecklistChannel.Replace(" ", "");
                                List<string> partialList = new List<string>();
                                partialList.AddRange(new List<string> { $"{channel.ToLower()}_read", $"{channel.ToLower()}_readwrite" });
                                permissionsNeeded = (await _permissionRepository.GetAllAsync(requestId)).ToList().Where(x => partialList.Any(x.Name.ToLower().Contains)).ToList();
                                access = StatusCodes.Status200OK == await _authorizationService.CheckAccessAsync(permissionsNeeded, requestId) ? true : false;
                            }
                            else
                                access = false;

                        }

                    }

                    if (access || overrideAccess)
                    {
                        var checklistTasks = new List<ChecklistTaskModel>();
                        foreach (var subitem in item.ChecklistTaskList)
                        {
                            var checklistItem = new ChecklistTaskModel
                            {
                                Id = subitem.Id,
                                ChecklistItem = subitem.ChecklistItem,
                                Completed = subitem.Completed,
                                FileUri = subitem.FileUri
                            };
                            checklistTasks.Add(checklistItem);
                        }

                        var checklistModel = new ChecklistModel
                        {
                            Id = item.Id,
                            ChecklistStatus = item.ChecklistStatus,
                            ChecklistTaskList = checklistTasks,
                            ChecklistChannel = item.ChecklistChannel
                        };
                        viewModel.Checklists.Add(checklistModel);
                    }
                    //Granular Access : End
                }
                return viewModel;
            }
            catch(Exception ex)
            {
                throw new ResponseException($"RequestId: {requestId} - CheckListProcessService MapToModel oppId: {entity.Id} - failed to map opportunity: {ex}");
            }
        }

        public async Task<Opportunity> UpdateWorkflowAsync(Opportunity opportunity, string requestId = "")
        {
            return await UpdateCheckList(opportunity, requestId);
        }
        public async Task<Opportunity> UpdateCheckList(Opportunity opportunity, string requestId = "")
        {
            try
            {
                var oppCheckLists = opportunity.Content.Checklists.ToList();
                var updatedDealTypeList = new List<Process>();
                foreach (var process in opportunity.Content.DealType.ProcessList)
                {
                    if (opportunity.Content.CustomerDecision.Approved)
                    {
                        process.Status = ActionStatus.Completed;
                    }

                    if (process.ProcessType.ToLower() == "checklisttab")
                    {
                        var checklistItm = oppCheckLists.Find(x => x.ChecklistChannel.ToLower() == process.Channel.ToLower());
                        if (checklistItm != null)
                        {
                            //Granular Access : Start
                            var permissionsNeeded = new List<ApplicationCore.Entities.Permission>();
                            List<string> list = new List<string>();
                            var access = true;
                            //going for super access
                            list.AddRange(new List<string> { Access.Opportunities_ReadWrite_All.ToString() });
                            permissionsNeeded = (await _permissionRepository.GetAllAsync(requestId)).ToList().Where(x => list.Any(x.Name.Contains)).ToList();
                            if (!(StatusCodes.Status200OK == await _authorizationService.CheckAccessAsync(permissionsNeeded, requestId)))
                            {
                                //going for opportunity access
                                //some more refactor todo
                                access = await _authorizationService.CheckAccessInOpportunityAsync(opportunity, PermissionNeededTo.Write, requestId);
                                if (!access)
                                {
                                    //going for partial accesss
                                    access = await _authorizationService.CheckAccessInOpportunityAsync(opportunity, PermissionNeededTo.WritePartial, requestId);
                                    if (access)
                                    {
                                        var channel = checklistItm.ChecklistChannel.Replace(" ", "");
                                        list.Clear();
                                        list.AddRange(new List<string> { $"{channel.ToLower()}_readwrite" });
                                        permissionsNeeded = (await _permissionRepository.GetAllAsync(requestId)).ToList().Where(x => list.Any(x.Name.ToLower().Contains)).ToList();
                                        access = StatusCodes.Status200OK == await _authorizationService.CheckAccessAsync(permissionsNeeded, requestId) ? true : false;
                                    }
                                    else access = false;
                                }
                            }
                            if (access)
                                process.Status = checklistItm.ChecklistStatus;
                            //else
                            //    throw new AccessDeniedException("Access Denied for updating checklist");
                            //Granular Access : End
                        }
                    }
                    updatedDealTypeList.Add(process);
                }

                opportunity.Content.DealType.ProcessList = updatedDealTypeList;

                return opportunity;
            }
            catch(Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} CheckListProcessService - UpdateCheckList : {ex.Message} AccessDeniedException");
                // new AccessDeniedException($"RequestId: {requestId} CheckListProcessService - UpdateCheckList : {ex.Message} AccessDeniedException");
                return opportunity;
            }
        }

    }
}
