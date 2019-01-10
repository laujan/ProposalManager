// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using ApplicationCore;
using ApplicationCore.Artifacts;
using ApplicationCore.Helpers;
using ApplicationCore.Interfaces;
using ApplicationCore.Interfaces.SmartLink;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Helpers
{
    public class AddInHelper : IAddInHelper
    {

        protected readonly ILogger _logger;
        protected readonly AppOptions _appOptions;
        private readonly IUserContext _userContext;
        private readonly OpportunityHelpers opportunityHelpers;
        private IProposalManagerClientFactory proposalManagerClientFactory;
        private IDocumentIdService documentIdService;

        /// <summary>
        /// Constructor
        /// </summary>
        public AddInHelper(
            ILogger<AddInHelper> logger,
            IOptions<AppOptions> appOptions,
            IUserContext userContext,
            OpportunityHelpers opportunityHelpers,
            IConfiguration configuration,
            IProposalManagerClientFactory proposalManagerClientFactory,
            IDocumentIdService documentIdService)
        {
            Guard.Against.Null(logger, nameof(logger));
            Guard.Against.Null(appOptions, nameof(appOptions));

            _logger = logger;
            _appOptions = appOptions.Value;
            _userContext = userContext;
            this.opportunityHelpers = opportunityHelpers;

            this.proposalManagerClientFactory = proposalManagerClientFactory;
            this.documentIdService = documentIdService;
        }

        public async Task<StatusCodes> CallAddInWebhookAsync(Opportunity opportunity, string requestId = "")
        {
            var client = await proposalManagerClientFactory.GetProposalManagerClientAsync();
            var result = await client.PostAsync("/api/dynamics/LinkSharePointLocations", new StringContent(JsonConvert.SerializeObject(await opportunityHelpers.OpportunityToViewModelAsync(opportunity, requestId)), Encoding.UTF8, "application/json"));
            return result.IsSuccessStatusCode ? StatusCodes.Status200OK : StatusCodes.Status400BadRequest;
        }

        public async Task<StatusCodes> ActivateDocumentId(Opportunity opportunity, string requestId = "")
        {
            try
            {
                await documentIdService.ActivateForSite($"https://{_appOptions.SharePointHostName}/sites/{opportunity.DisplayName.Replace(" ", string.Empty)}");
                return StatusCodes.Status204NoContent;
            }
            catch(Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - AddInHelper_ActivateDocumentId failed: {ex.Message} ");
                return StatusCodes.Status400BadRequest;
            }
        }
    }
}