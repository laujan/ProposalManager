// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using System;
using System.Collections.Generic;
using System.Text;
using ApplicationCore;
using ApplicationCore.Helpers;
using ApplicationCore.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ApplicationCore.Entities;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using Microsoft.AspNetCore.Authentication;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Infrastructure.Services
{
    public class PowerBIService : BaseService<PowerBIService>, IPowerBIService
    {
        private readonly IWebApiAuthProvider _webApiAuthProvider;
        private readonly IAzureKeyVaultService _azureKeyVaultService;

        public PowerBIService(
           ILogger<PowerBIService> logger,
           IOptionsMonitor<AppOptions> appOptions,
           IWebApiAuthProvider webApiAuthProvider,
           IAzureKeyVaultService azureKeyVaultService) : base(logger, appOptions)
        {
            _webApiAuthProvider = webApiAuthProvider;
            _azureKeyVaultService = azureKeyVaultService;
        }

        public async Task<String> GenerateTokenAsync(string requestId = "")
        {
            string _userName = await _azureKeyVaultService.GetValueFromVaultAsync(_appOptions.PBIUserName, requestId);
            string _password = await _azureKeyVaultService.GetValueFromVaultAsync(_appOptions.PBIUserPassword, requestId);
            string _applicationId = _appOptions.PBIApplicationId;
            string _workspaceId = _appOptions.PBIWorkSpaceId;
            string _reportId = _appOptions.PBIReportId;
            string _resourceUrl = "https://analysis.windows.net/powerbi/api";

            try
            {
                _logger.LogInformation($"RequestID:{requestId} - PowerBIService_GenerateTokenAsync called.");

                HttpClient client = new HttpClient();

                string tokenEndpoint = "https://login.microsoftonline.com/" + _appOptions.PBITenantId + "/oauth2/token";
                var body = "resource=" + _resourceUrl + "&client_id=" + _applicationId + "&grant_type=password&username=" + _userName + "&password=" + _password;
                var stringContent = new StringContent(body, Encoding.UTF8, "application/x-www-form-urlencoded");

                var result1 = await client.PostAsync(tokenEndpoint, stringContent).ContinueWith<string>((response) =>
                {
                    return response.Result.Content.ReadAsStringAsync().Result;
                });

                JObject jobject = JObject.Parse(result1);

                var token = jobject["access_token"].Value<string>();

                //var (token, expiration) = await _webApiAuthProvider.GetUserAccessTokenWithUsernamePasswordAsync();

                return token;
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - PowerBIService_GenerateTokenAsync Exception: {ex}");
                throw;
            }
        }
    }
}