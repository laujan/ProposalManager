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
    public class NewOpportunityProcessService : BaseService<NewOpportunityProcessService>, INewOpportunityProcessService
    {
        protected readonly GraphUserAppService _graphUserAppService;

        public NewOpportunityProcessService(
            ILogger<NewOpportunityProcessService> logger,
            IOptionsMonitor<AppOptions> appOptions,
            GraphUserAppService graphUserAppService) : base(logger, appOptions)
        {
            Guard.Against.Null(logger, nameof(logger));
            Guard.Against.Null(appOptions, nameof(appOptions));

            _graphUserAppService = graphUserAppService;
        }                                                                                        

        public Task<Opportunity> CreateWorkflowAsync(Opportunity opportunity, string requestId = "")
        {
            throw new NotImplementedException();
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
