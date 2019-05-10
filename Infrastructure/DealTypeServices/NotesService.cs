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
using ApplicationCore.Models;
using System.Net;
using Infrastructure.Services;
using Newtonsoft.Json.Linq;

namespace Infrastructure.DealTypeServices
{
    public class NotesService : BaseService<NotesService>, INotesService
    {
        private readonly IUserContext _userContext;
        private readonly IUserProfileRepository _userProfileRepository;

        public NotesService(
            ILogger<NotesService> logger,
            IOptionsMonitor<AppOptions> appOptions,
            IUserProfileRepository userProfileRepository,
            IUserContext userContext) : base(logger, appOptions)
        {
            Guard.Against.Null(logger, nameof(logger));
            Guard.Against.Null(appOptions, nameof(appOptions));
            Guard.Against.Null(userProfileRepository, nameof(userProfileRepository));
            _userContext = userContext;
            _userProfileRepository = userProfileRepository;
        }

        public async Task<Opportunity> CreateWorkflowAsync(Opportunity opportunity, string requestId = "")
        {
            try
            {
                if (opportunity.Content.Notes != null)
                {
                    // Update note created by (if one) and set it to relationship manager
                    if (opportunity.Content.Notes?.Count > 0)
                    {
                        var currentUser = (_userContext.User.Claims).ToList().Find(x => x.Type == "preferred_username")?.Value;
                        var callerUser = await _userProfileRepository.GetItemByUpnAsync(currentUser, requestId);

                        if (callerUser != null)
                        {
                            opportunity.Content.Notes[0].CreatedBy = callerUser;
                            opportunity.Content.Notes[0].CreatedDateTime = DateTimeOffset.Now;

                        }
                        else
                        {
                            _logger.LogWarning($"RequestId: {requestId} - CreateWorkflowAsync can't find {currentUser} to set note created by");
                        }
                    }
                }
            }
            catch
            {
                _logger.LogError($"RequestId: {requestId} - CreateWorkflowAsync Service Exception");
            }

            return opportunity;
        }

        public Task<Opportunity> MapToEntityAsync(Opportunity entity, OpportunityViewModel viewModel, string requestId = "")
        {
            throw new NotImplementedException();
        }

        public Task<OpportunityViewModel> MapToModelAsync(Opportunity entity, OpportunityViewModel viewModel, string requestId = "")
        {
            throw new NotImplementedException();
        }

        public Task<Opportunity> UpdateWorkflowAsync(Opportunity opportunity, string requestId = "")
        {
            throw new NotImplementedException();
        }
    }
}
