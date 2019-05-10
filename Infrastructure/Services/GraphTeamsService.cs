// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.


using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ApplicationCore;
using ApplicationCore.Interfaces;
using Infrastructure.GraphApi;
using Microsoft.Extensions.Caching.Memory;

namespace Infrastructure.Services
{
    public class GraphTeamsAppService : GraphTeamsBaseService
    {
        public GraphTeamsAppService(
            ILogger<GraphTeamsAppService> logger,
            IOptionsMonitor<AppOptions> appOptions,
            IGraphClientAppContext graphClientContext,
            IUserContext userContext,
            IAzureKeyVaultService azureKeyVaultService,
            IMemoryCache memoryCache) : base(logger, appOptions, graphClientContext, userContext, azureKeyVaultService, memoryCache)
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
            IAzureKeyVaultService azureKeyVaultService,
            IMemoryCache memoryCache) : base(logger, appOptions, graphClientContext, userContext, azureKeyVaultService, memoryCache)
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
            IAzureKeyVaultService azureKeyVaultService,
            IMemoryCache memoryCache) : base(logger, appOptions, graphClientContext, userContext, azureKeyVaultService, memoryCache)
        {
        }
    }
}
