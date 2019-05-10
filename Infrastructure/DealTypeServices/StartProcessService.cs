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
    public class StartProcessService : BaseService<StartProcessService>, IStartProcessService
    {
        protected readonly GraphUserAppService _graphUserAppService;

        public StartProcessService(
            ILogger<StartProcessService> logger,
            IOptionsMonitor<AppOptions> appOptions,
            GraphUserAppService graphUserAppService) : base(logger, appOptions)
        {
            Guard.Against.Null(logger, nameof(logger));
            Guard.Against.Null(appOptions, nameof(appOptions));

            _graphUserAppService = graphUserAppService;
        }

        public async Task<Opportunity> CreateWorkflowAsync(Opportunity opportunity, string requestId = "")
        {
            return await UpdateStartProcessStatus(opportunity, requestId);
        }

        public Task<Opportunity> MapToEntityAsync(Opportunity entity, OpportunityViewModel viewModel, string requestId = "")
        {
            throw new NotImplementedException();
        }

        public Task<OpportunityViewModel> MapToModelAsync(Opportunity entity, OpportunityViewModel viewModel, string requestId = "")
        {
            throw new NotImplementedException();
        }

        public async Task<Opportunity> UpdateWorkflowAsync(Opportunity opportunity, string requestId = "")
        {
            return await UpdateStartProcessStatus(opportunity, requestId);
        }
        private async Task<Opportunity> UpdateStartProcessStatus(Opportunity opportunity, string requestId = "")
        {
       
            //get the process steps that have been asigned at least member
            var teamMembersCheck = new List<string>();
            foreach (var item in opportunity.Content.TeamMembers)
            {
                //process steps check
                if (!String.IsNullOrEmpty(item.ProcessStep))
                {
                    var process = opportunity.Content.Template.ProcessList.ToList().Find(x => x.ProcessStep.ToLower() == item.ProcessStep.ToLower());
                    if (process != null)
                    {
                        if (process.ProcessType.ToLower() == "checklisttab")
                            teamMembersCheck.Add(process.ProcessStep.ToLower());
                    }
                }


            }
            //check if all the processes in the deal type are assigned to at least one team member
            bool statusCheck = true;
            foreach(var process in opportunity.Content.Template.ProcessList)
            {
                if (process.ProcessType.ToLower() == "checklisttab")
                {
                    if (!teamMembersCheck.Contains(process.ProcessStep.ToLower()))
                        statusCheck = false;
                }
            }
            //update the status of the start process step if all processes are assigned to a member
            if(statusCheck)
            {
                foreach (var process in opportunity.Content.Template.ProcessList)
                    if (process.ProcessStep.ToLower() == "start process")
                    {
                        if(process.Status != ActionStatus.Completed)
                            process.Status = ActionStatus.Completed;
                    }
                      
            }

            return opportunity;
        }
    }
}
