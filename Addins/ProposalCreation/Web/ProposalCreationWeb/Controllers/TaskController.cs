// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ProposalCreation.Core.Helpers;
using ProposalCreation.Core.Interfaces;
using ProposalCreation.Core.Models;
using ProposalCreation.Core.Providers;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProposalCreationWeb.Controllers
{
    //[Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TaskController : BaseController
    {

        private readonly string SiteId;
        private readonly string ProposalManagerApiUrl;
        private readonly IGraphSdkHelper httpHelper;

        private readonly ITaskProvider taskProvider;

        public TaskController(
            IGraphSdkHelper graphSdkHelper,
            IRootConfigurationProvider rootConfigurationProvider,
            ITaskProvider taskProvider) : base(graphSdkHelper)
        {
            // Get from config
            var appOptions = rootConfigurationProvider.GeneralConfiguration;

            ProposalManagerApiUrl = appOptions.ProposalManagerApiUrl;
            SiteId = appOptions.SiteId;

            httpHelper = graphSdkHelper;

            this.taskProvider = taskProvider;
        }

        public async Task<IEnumerable<string>> GetTasks()
        {
            var result = await taskProvider.GetTasksAsync();
            return result;
        }

    }
}