// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using System;
using System.Collections.Generic;
using System.Text;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ApplicationCore;
using ApplicationCore.Interfaces;
using Infrastructure.GraphApi;

namespace Infrastructure.Services
{
    public class GraphTeamsAppService : GraphTeamsBaseService
    {
        public GraphTeamsAppService(
            ILogger<GraphTeamsAppService> logger,
            IOptionsMonitor<AppOptions> appOptions,
            IGraphClientAppContext graphClientContext,
            IUserContext userContext,
            IAzureKeyVaultService azureKeyVaultService) : base(logger, appOptions, graphClientContext, userContext, azureKeyVaultService)
        {
        }
    }

    public class GraphTeamsUserService : GraphTeamsBaseService
    {
        public GraphTeamsUserService(
            ILogger<GraphTeamsUserService> logger,
            IOptionsMonitor<AppOptions> appOptions,
            IGraphClientUserContext graphClientContext,
            IUserContext userContext,
            IAzureKeyVaultService azureKeyVaultService) : base(logger, appOptions, graphClientContext, userContext, azureKeyVaultService)
        {
        }
    }
    public class GraphTeamsOnBehalfService : GraphTeamsBaseService
    {
        public GraphTeamsOnBehalfService(
            ILogger<GraphTeamsUserService> logger,
            IOptionsMonitor<AppOptions> appOptions,
            IGraphClientOnBehalfContext graphClientContext,
            IUserContext userContext,
            IAzureKeyVaultService azureKeyVaultService) : base(logger, appOptions, graphClientContext, userContext, azureKeyVaultService)
        {
        }
    }
}
