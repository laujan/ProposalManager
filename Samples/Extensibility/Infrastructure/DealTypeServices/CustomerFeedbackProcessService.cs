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
    public class CustomerFeedbackProcessService : BaseService<CustomerFeedbackProcessService> , ICustomerFeedbackProcessService
    {
        private readonly CardNotificationService _cardNotificationService;
        private readonly IAuthorizationService _authorizationService;
        private readonly IPermissionRepository _permissionRepository;

        public CustomerFeedbackProcessService(
            ILogger<CustomerFeedbackProcessService> logger,
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
            return await UpdateCustomerFeedback(opportunity, requestId);
        }

        public async Task<Opportunity> MapToEntityAsync(Opportunity entity, OpportunityViewModel viewModel, string requestId = "")
        {
            try
            {
                if (entity.Content.CustomerFeedback == null) entity.Content.CustomerFeedback = CustomerFeedback.Empty;
                if (viewModel.CustomerFeedback != null)
                {
                    var updatedFeedbacks = new CustomerFeedback();

                    updatedFeedbacks.Id = viewModel.CustomerFeedback.Id ?? String.Empty;
                    updatedFeedbacks.CustomerFeedbackChannel = viewModel.CustomerFeedback.CustomerFeedbackChannel ?? String.Empty;
                    updatedFeedbacks.CustomerFeedbackList = viewModel.CustomerFeedback.CustomerFeedbackList.Select(feedback => new CustomerFeedbackItem
                    {
                        Id = feedback.Id ?? String.Empty,
                        FeedbackContactMeans = feedback.FeedbackContactMeans ?? ContactMeans.Unkwown,
                        FeedbackDate = feedback.FeedbackDate,
                        FeedbackSummary = feedback.FeedbackSummary ?? String.Empty,
                        FeedbackDetails = feedback.FeedbackDetails ?? String.Empty
                    }).ToList();

                    entity.Content.CustomerFeedback = updatedFeedbacks;
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
                //Granular bug fix : Start
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
                            List<string> partialList = new List<string>();
                            partialList.AddRange(new List<string> { "customerfeedback_read", "customerfeedback_readwrite" });
                            permissionsNeeded = (await _permissionRepository.GetAllAsync(requestId)).ToList().Where(x => partialList.Any(x.Name.ToLower().Contains)).ToList();
                            access = StatusCodes.Status200OK == await _authorizationService.CheckAccessAsync(permissionsNeeded, requestId) ? true : false;
                        }
                        else
                            access = false;
                    }
                }

                if (access || overrideAccess)
                {
                    var feedbackList = entity.Content.CustomerFeedback.CustomerFeedbackList.Select(feedback => new CustomerFeedbackItemModel
                    {
                        Id = feedback.Id,
                        FeedbackContactMeans = feedback.FeedbackContactMeans,
                        FeedbackDate = feedback.FeedbackDate,
                        FeedbackSummary = feedback.FeedbackSummary,
                        FeedbackDetails = feedback.FeedbackDetails
                    }).ToList();

                    viewModel.CustomerFeedback = new CustomerFeedbackModel
                    {
                        Id = entity.Content.CustomerFeedback.Id,
                        CustomerFeedbackList = feedbackList,
                        CustomerFeedbackChannel = entity.Content.CustomerFeedback.CustomerFeedbackChannel
                    };                    
                }

                //Granular Access : End
                return viewModel;
            }
            catch(Exception ex)
            {
                throw new ResponseException($"RequestId: {requestId} - CheckListProcessService MapToModel oppId: {entity.Id} - failed to map opportunity: {ex}");
            }
        }

        public async Task<Opportunity> UpdateWorkflowAsync(Opportunity opportunity, string requestId = "")
        {
            return await UpdateCustomerFeedback(opportunity, requestId);
        }

        private async Task<Opportunity> UpdateCustomerFeedback(Opportunity opportunity, string requestId = "")
        {
            opportunity.Content.CustomerFeedback = await RemoveEmptyFromFeedbackAsync(opportunity.Content.CustomerFeedback, requestId);

            return opportunity;
        }

        private Task<CustomerFeedback> RemoveEmptyFromFeedbackAsync(CustomerFeedback feedback, string requestId = "")
        {
            try
            {
                var newFeedback = new CustomerFeedback();

                newFeedback.Id = feedback.Id;
                newFeedback.CustomerFeedbackChannel = feedback.CustomerFeedbackChannel;
                newFeedback.CustomerFeedbackList = new List<CustomerFeedbackItem>();

                foreach (var item in feedback.CustomerFeedbackList)
                {
                    var newFeedbackItem = new CustomerFeedbackItem();
                    if (!String.IsNullOrEmpty(item.Id) && !String.IsNullOrEmpty(item.FeedbackSummary))
                    {
                        newFeedbackItem.Id = item.Id;
                        newFeedbackItem.FeedbackDate = item.FeedbackDate;
                        newFeedbackItem.FeedbackContactMeans = item.FeedbackContactMeans;
                        newFeedbackItem.FeedbackSummary = item.FeedbackSummary;
                        newFeedbackItem.FeedbackDetails = item.FeedbackDetails;

                        newFeedback.CustomerFeedbackList.Add(newFeedbackItem);
                    }
                }

                return Task.FromResult<CustomerFeedback>(newFeedback);
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - RemoveEmptyFromFeedback Service Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - RemoveEmptyFromFeedback Service Exception: {ex}");
            }
        }

    }
}
