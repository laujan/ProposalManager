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
using Microsoft.Extensions.FileProviders;
using System.Threading.Tasks;
using Infrastructure.Helpers;
using ApplicationCore.Entities.GraphServices;
using System.Net.Http;
using Microsoft.Graph;

namespace Infrastructure.Services
{
    public class GraphSharePointAppService : GraphSharePointBaseService
    {
        public GraphSharePointAppService(
            ILogger<GraphSharePointBaseService> logger,
            IOptionsMonitor<AppOptions> appOptions,
            IGraphClientAppContext graphClientContext,
            SharePointListsSchemaHelper sharePointListsSchemaHelper) : base(logger, appOptions, graphClientContext)
        {
        }

        
    }

    public class GraphSharePointUserService : GraphSharePointBaseService
    {
        public GraphSharePointUserService(
            ILogger<GraphSharePointBaseService> logger,
            IOptionsMonitor<AppOptions> appOptions,
            IGraphClientUserContext graphClientContext) : base(logger, appOptions, graphClientContext)
        {
        }
    }
}
