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
            IUserContext userContext) : base(logger, appOptions, graphClientContext, userContext)
        {
        }
    }

    public class GraphTeamUserService : GraphTeamsBaseService
    {
        public GraphTeamUserService(
            ILogger<GraphTeamUserService> logger,
            IOptionsMonitor<AppOptions> appOptions,
            IGraphClientUserContext graphClientContext,
            IUserContext userContext) : base(logger, appOptions, graphClientContext, userContext)
        {
        }
    }
}
