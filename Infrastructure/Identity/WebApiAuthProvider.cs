// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Identity.Client;
using Microsoft.Identity;
using Microsoft.Graph;
using ApplicationCore;
using ApplicationCore.Interfaces;
using Infrastructure.Identity;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Authentication;
//using Microsoft.IdentityModel.Clients.ActiveDirectory;

namespace Infrastructure.Identity
{
    /// <summary>
    /// Provider to get the access token 
    /// </summary>
    public class WebApiAuthProvider : IWebApiAuthProvider
    {
        private readonly IMemoryCache _memoryCache;
        private TokenCache _userTokenCache;

        // Properties used to get and manage an access token.
        private readonly string proposalManagerClientId;
        private readonly string _clientId;
        private readonly string _aadInstance;
        private readonly ClientCredential _credential;
        private readonly string _appSecret;
        private readonly string[] _scopes;
        private readonly string _redirectUri;
        private readonly string _graphResourceId;
        private readonly string _tenantId;
        private readonly string _authority;
        private readonly IHttpContextAccessor _httpContextAccessor;


        public WebApiAuthProvider(
            IMemoryCache memoryCache, 
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor)
        {
            var azureOptions = new AzureAdOptions();
            configuration.Bind("AzureAd", azureOptions);
            var dynamicsConfiguration = new Dynamics365Configuration();
            configuration.Bind(Dynamics365Configuration.ConfigurationName, dynamicsConfiguration);

            proposalManagerClientId = azureOptions.ClientId;

            _clientId = azureOptions.ClientId;
            _aadInstance = azureOptions.Instance;
            _appSecret = azureOptions.ClientSecret;
            _credential = new Microsoft.Identity.Client.ClientCredential(_appSecret); // For development mode purposes only. Production apps should use a client certificate.
            _scopes = azureOptions.GraphScopes.Split(new[] { ' ' });
            _redirectUri = azureOptions.BaseUrl + azureOptions.CallbackPath;
            _graphResourceId = azureOptions.GraphResourceId;
            _tenantId = azureOptions.TenantId;

            _memoryCache = memoryCache;

            _authority = azureOptions.Authority;
            _httpContextAccessor = httpContextAccessor;
        }

        // Gets an access token. First tries to get the access token from the token cache.
        // Using password (secret) to authenticate. Production apps should use a certificate.
        public async Task<string> GetUserAccessTokenAsync(string userId)
        {
            if (_userTokenCache == null) _userTokenCache = new SessionTokenCache(userId, _memoryCache).GetCacheInstance();

            var cca = new ConfidentialClientApplication(
                _clientId,
                _redirectUri,
                _credential,
                _userTokenCache,
                null);

            var originalToken = await _httpContextAccessor.HttpContext.GetTokenAsync("access_token");

            var userAssertion = new UserAssertion(originalToken,
                "urn:ietf:params:oauth:grant-type:jwt-bearer");

            try
            {
                var result = await cca.AcquireTokenOnBehalfOfAsync(_scopes, userAssertion);

                return result.AccessToken;
            }
            catch (Exception ex)
            {
                // Unable to retrieve the access token silently.
                throw new ServiceException(new Error
                {
                    Code = GraphErrorCode.AuthenticationFailure.ToString(),
                    Message = $"Caller needs to authenticate. Unable to retrieve the access token silently. error: {ex}"
                });
            }
        }


        // Gets an access token. First tries to get the access token from the token cache.
        // This app uses a password (secret) to authenticate. Production apps should use a certificate.
        public async Task<(string token, DateTimeOffset expiration)> GetAppAccessTokenAsync()
        {

            try
            {
                var authorityFormat = "https://login.microsoftonline.com/{0}/v2.0"; // /token   /authorize
                ConfidentialClientApplication daemonClient = new ConfidentialClientApplication(_clientId, String.Format(authorityFormat, _tenantId), _redirectUri, _credential, null, new TokenCache());

                var scopes = new List<string>() { $"api://{proposalManagerClientId}/.default" };

                AuthenticationResult result = await daemonClient.AcquireTokenForClientAsync(scopes);

                return (result.AccessToken, result.ExpiresOn);
            }
            catch (Exception ex)
            {
                // Unable to retrieve the access token silently.
                throw new ServiceException(new Error
                {
                    Code = GraphErrorCode.AuthenticationFailure.ToString(),
                    Message = $"WebApiAuthProvider_GetAppAccessTokenAsync Caller needs to authenticate. Unable to retrieve the access token silently. error: {ex}"
                });
            }
        }
    }
}
