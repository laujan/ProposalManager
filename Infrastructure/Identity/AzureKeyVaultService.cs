// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using ApplicationCore;
using Microsoft.Azure.KeyVault;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using Microsoft.IdentityModel.Clients.ActiveDirectory;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Web;
using Microsoft.Extensions.Logging;
using ApplicationCore.Helpers.Exceptions;
using ApplicationCore.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Infrastructure.Services;
using ApplicationCore.Helpers;

namespace Infrastructure.Identity
{
    public class AzureKeyVaultService : IAzureKeyVaultService
    {
        private readonly AppOptions _appOptions;
        private readonly ILogger _logger;
        private readonly IWritableOptions<AppOptions> _writableOptions;
        private static string _clientId { get; set; }
        private static string _appSecret { get; set; }


        public AzureKeyVaultService(
            IOptionsMonitor<AppOptions> appOptions,
            IOptionsMonitor<AzureAdOptions> azureAdOptions,
            IConfiguration configuration,
            ILogger<AzureKeyVaultService> logger,
            IWritableOptions<AppOptions> writableOptions)
        {
            configuration.Bind("AzureAd", azureAdOptions);
            _appOptions = appOptions.CurrentValue;
            _clientId = azureAdOptions.CurrentValue.ClientId;
            _appSecret = azureAdOptions.CurrentValue.ClientSecret;
            _logger = logger;
            _writableOptions = writableOptions;
        }

        public async Task<string> GetValueFromVaultAsync(string key, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - GetValueFromVaultAsync called.");
            try
            {
                var keyVaultClient = new KeyVaultClient(new
                    KeyVaultClient.AuthenticationCallback(GetToken), new HttpClient());
                var sec = await keyVaultClient.GetSecretAsync(GetVaultBaseUrl(key));
                return sec.Value;
            }
            catch(Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - GetValueFromVaultAsync Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - GetValueFromVaultAsync  Exception: {ex}");
            }
        }

        public async Task SetValueInVaultAsync(string key, string value, string requestId = "")
        {
            _logger.LogInformation($"RequestId: {requestId} - SetValueInVaultAsync called.");
            try
            {
                var keyVaultClient = new KeyVaultClient(new
                    KeyVaultClient.AuthenticationCallback(GetToken), new HttpClient());
                var sec = await keyVaultClient.SetSecretAsync(_appOptions.VaultBaseUrl, key, value);
                //update appsetings with the new secret identifier.
                await _writableOptions.UpdateAsync(key, sec.SecretIdentifier.ToString());
            }
            catch (Exception ex)
            {
                _logger.LogError($"RequestId: {requestId} - SetValueInVaultAsync Exception: {ex}");
                throw new ResponseException($"RequestId: {requestId} - SetValueInVaultAsync  Exception: {ex}");
            }
        }

        public static async Task<string> GetToken(string authority, string resource, string scope)
        {
            var authContext = new AuthenticationContext(authority);
            ClientCredential clientCred = new ClientCredential( _clientId,
                        _appSecret);
            AuthenticationResult result = await authContext.AcquireTokenAsync(resource, clientCred);

            if (result == null)
                throw new InvalidOperationException("Failed to obtain the JWT token");

            return result.AccessToken;
        }

        private string GetVaultBaseUrl(string key)
        {
            string vaultBaseUrl = string.Empty;

            switch (key)
            {
                case "AccessToken":
                    vaultBaseUrl = _appOptions.AccessToken;
                    break;
                case "Upn":
                    vaultBaseUrl = _appOptions.Upn;
                    break;
                case "Expiration":
                    vaultBaseUrl = _appOptions.Expiration;
                    break;
            }

            return vaultBaseUrl;
        }
    }
}
