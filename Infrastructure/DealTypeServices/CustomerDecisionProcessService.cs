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
    public class CustomerDecisionProcessService : BaseService<CustomerDecisionProcessService>, ICustomerDecisionProcessService
    {
        private readonly IAuthorizationService _authorizationService;
        private readonly IPermissionRepository _permissionRepository;

        public CustomerDecisionProcessService(
        ILogger<CustomerDecisionProcessService> logger,
        IAuthorizationService authorizationService,
        IPermissionRepository permissionRepository,
        IOptionsMonitor<AppOptions> appOptions) : base(logger, appOptions)
        {
            Guard.Against.Null(logger, nameof(logger));
            Guard.Against.Null(appOptions, nameof(appOptions));
            Guard.Against.Null(authorizationService, nameof(authorizationService));
            Guard.Against.Null(permissionRepository, nameof(permissionRepository));

            _authorizationService = authorizationService;
            _permissionRepository = permissionRepository;

        }

        public async Task<Opportunity> CreateWorkflowAsync(Opportunity opportunity, string requestId = "")
        {
            return await UpdateCustomerDecision(opportunity, requestId);
        }

        public async Task<Opportunity> MapToEntityAsync(Opportunity entity, OpportunityViewModel viewModel, string requestId = "")
        {
            if (entity.Content.CustomerDecision == null) entity.Content.CustomerDecision = CustomerDecision.Empty;
            if (viewModel.CustomerDecision != null)
            {
                entity.Content.CustomerDecision.Id = viewModel.CustomerDecision.Id ?? String.Empty;
                entity.Content.CustomerDecision.Approved = viewModel.CustomerDecision.Approved;
                if (viewModel.CustomerDecision.ApprovedDate != null) entity.Content.CustomerDecision.ApprovedDate = viewModel.CustomerDecision.ApprovedDate;
                if (viewModel.CustomerDecision.LoanDisbursed != null) entity.Content.CustomerDecision.LoanDisbursed = viewModel.CustomerDecision.LoanDisbursed;
            }
            return entity;
        }

        public async Task<OpportunityViewModel> MapToModelAsync(Opportunity entity, OpportunityViewModel viewModel, string requestId = "")
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
            list.AddRange(new List<string> { Access.Opportunities_Read_All.ToString(), Access.Opportunities_ReadWrite_All.ToString()});
            permissionsNeeded = (await _permissionRepository.GetAllAsync(requestId)).ToList().Where(x => list.Any(x.Name.Contains)).ToList();
            if(!(StatusCodes.Status200OK == await _authorizationService.CheckAccessAsync(permissionsNeeded, requestId))){
                //going for opportunity access
                access = await _authorizationService.CheckAccessInOpportunityAsync(entity, PermissionNeededTo.Read, requestId);
                if (!access)
                {
                    access = await _authorizationService.CheckAccessInOpportunityAsync(entity, PermissionNeededTo.ReadPartial, requestId);
                    if (access)
                    {
                        //going for partial accesss
                        list.Clear();
                        list.AddRange(new List<string> { "customerdecision_read", "customerdecision_readwrite" });
                        permissionsNeeded = (await _permissionRepository.GetAllAsync(requestId)).ToList().Where(x => list.Any(x.Name.Contains)).ToList();
                        access = StatusCodes.Status200OK == await _authorizationService.CheckAccessAsync(permissionsNeeded, requestId) ? true : false;
                    }
                    else access = false;
                }
            }

            if (access || overrideAccess)     
            {
                viewModel.CustomerDecision = new CustomerDecisionModel
                {
                    Id = entity.Content.CustomerDecision.Id,
                    Approved = entity.Content.CustomerDecision.Approved,
                    ApprovedDate = entity.Content.CustomerDecision.ApprovedDate,
                    LoanDisbursed = entity.Content.CustomerDecision.LoanDisbursed
                };
            }
            //Granular Access : End

            return viewModel;
        }

        public async Task<Opportunity> UpdateWorkflowAsync(Opportunity opportunity, string requestId = "")
        {
            return await UpdateCustomerDecision(opportunity,requestId);
        }
        public async Task<Opportunity> UpdateCustomerDecision(Opportunity opportunity, string requestId = "")
        {
            try
            {
                if (opportunity.Content.CustomerDecision.Approved)
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
                        access = await _authorizationService.CheckAccessInOpportunityAsync(opportunity, PermissionNeededTo.Write, requestId);
                        if (!access)
                        {
                            access = await _authorizationService.CheckAccessInOpportunityAsync(opportunity, PermissionNeededTo.WritePartial, requestId);
                            //going for partial accesss
                            if (access)
                            {
                                list.Clear();
                                list.AddRange(new List<string> { "customerdecision_readwrite" });
                                permissionsNeeded = (await _permissionRepository.GetAllAsync(requestId)).ToList().Where(x => list.Any(x.Name.Contains)).ToList();
                                access = StatusCodes.Status200OK == await _authorizationService.CheckAccessAsync(permissionsNeeded, requestId) ? true : false;
                            }
                            else access = false;

                        }
                    }

                    if (access)
                    {
                        //update the state of the opportunity
                        opportunity.Metadata.OpportunityState = OpportunityState.Accepted;
                        //update the state of the processes
                        foreach (var process in opportunity.Content.DealType.ProcessList)
                            process.Status = ActionStatus.Completed;
                    }
                    else
                       throw new AccessDeniedException("Access Denied for updating customer decision");
                    //else throw access denied exeception
                }
                //Granular Access : End

                return opportunity;
            }
            catch(Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} CustomerDecisionProcessService - UpdateCustomerDecision : {ex.Message} AccessDeniedException");
                //throw new AccessDeniedException($"RequestId: {requestId} CustomerDecisionProcessService - UpdateCustomerDecision : {ex.Message} AccessDeniedException");
                return opportunity;
            }
        }
    }
}
