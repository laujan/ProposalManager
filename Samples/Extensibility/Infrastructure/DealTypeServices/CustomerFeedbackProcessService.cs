// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using ApplicationCore;
using ApplicationCore.Artifacts;
using ApplicationCore.Authorization;
using ApplicationCore.Entities;
using ApplicationCore.Helpers;
using ApplicationCore.Helpers.Exceptions;
using ApplicationCore.Interfaces;
using ApplicationCore.Models;
using ApplicationCore.Services;
using ApplicationCore.ViewModels;
using Infrastructure.Authorization;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Infrastructure.DealTypeServices
{
	public class CustomerFeedbackProcessService : BaseService<CustomerFeedbackProcessService>, IDealTypeService
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

		public Task<Opportunity> CreateWorkflowAsync(Opportunity opportunity, string requestId = "")
		{
			return Task.FromResult(opportunity);
		}
		public Task<Opportunity> MapToEntityAsync(Opportunity entity, OpportunityViewModel viewModel, string requestId = "")
		{
			//try
			//{

			//	if (entity.Content.CustomerFeedback == null) entity.Content.CustomerFeedback = new CustomerFeedback();
			//	if (viewModel.CustomerFeedback != null)
			//	{

			//		var item = viewModel.CustomerFeedback;

			//		var customerFeedback = CustomerFeedback.Empty;
			//		var existingAnalysis = entity.Content.CustomerFeedback;
			//		if (existingAnalysis != null) customerFeedback = existingAnalysis;

			//		customerFeedback.Id = item.Id ?? String.Empty;
			//		customerFeedback.CustomerFeedbackStatus = ActionStatus.FromValue(item.CustomerFeedbackStatus.Value);
			//		customerFeedback.CustomerFeedbackChannel = item.CustomerFeedbackChannel ?? String.Empty;

			//		entity.Content.CustomerFeedback = customerFeedback;
			//	}

				return Task.FromResult(entity);
			//}
			//catch (Exception ex)
			//{
			//	throw new ResponseException($"RequestId: {requestId} - CustomerFeedbackProcessService MapToEntity oppId: {entity.Id} - failed to map opportunity: {ex}");
			//}
		}

		public async Task<OpportunityViewModel> MapToModelAsync(Opportunity entity, OpportunityViewModel viewModel, string requestId = "")
		{
			//try
			//{
			//	var item = entity.Content.CustomerFeedback;

			//	var overrideAccess = _authorizationService.GetGranularAccessOverride();

			//	var permissionsNeeded = new List<Permission>();
			//	var list = new List<string>();
			//	var access = true;

			//	list.AddRange(new List<string> { Access.Opportunities_Read_All.ToString(), Access.Opportunities_ReadWrite_All.ToString() });
			//	permissionsNeeded = (await _permissionRepository.GetAllAsync(requestId)).ToList().Where(x => list.Any(x.Name.Contains)).ToList();
			//	if (!(StatusCodes.Status200OK == await _authorizationService.CheckAccessAsync(permissionsNeeded, requestId)))
			//	{

			//		access = await _authorizationService.CheckAccessInOpportunityAsync(entity, PermissionNeededTo.Read, requestId);
			//		if (!access)
			//		{
			//			//going for partial accesss
			//			access = await _authorizationService.CheckAccessInOpportunityAsync(entity, PermissionNeededTo.ReadPartial, requestId);
			//			if (access)
			//			{
			//				var channel = item.CustomerFeedbackChannel.Replace(" ", "");
			//				var partialList = new List<string>();
			//				partialList.AddRange(new List<string> { $"CustomerFeedbackProcessService_read", $"CustomerFeedbackProcessService_readwrite" });
			//				permissionsNeeded = (await _permissionRepository.GetAllAsync(requestId)).ToList().Where(x => partialList.Any(x.Name.ToLower().Contains)).ToList();
			//				access = StatusCodes.Status200OK == await _authorizationService.CheckAccessAsync(permissionsNeeded, requestId) ? true : false;
			//			}
			//			else
			//				access = false;

			//		}

			//	}

			//	if (access || overrideAccess)
			//	{
			//		var customerFeedbackModel = new CustomerFeedbackModel
			//		{
			//			Id = item.Id,
			//			CustomerFeedbackStatus = item.CustomerFeedbackStatus,
			//			CustomerFeedbackChannel = item.CustomerFeedbackChannel
			//		};
			//		viewModel.CustomerFeedback = customerFeedbackModel;
			//	}

				return viewModel;
			//}
			//catch (Exception ex)
			//{
			//	throw new ResponseException($"RequestId: {requestId} - CustomerFeedbackProcessService MapToModel oppId: {entity.Id} - failed to map opportunity: {ex}");
			//}
		}

		public Task<Opportunity> UpdateWorkflowAsync(Opportunity opportunity, string requestId = "")
		{
			return Task.FromResult(opportunity);
		}

	}
}