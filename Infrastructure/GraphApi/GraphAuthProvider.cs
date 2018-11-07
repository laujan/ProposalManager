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
using Microsoft.AspNetCore.Authentication.JwtBearer;
using System.Security.Claims;
using System.Globalization;
using ApplicationCore.Helpers;
using ApplicationCore.Helpers.Exceptions;

namespace Infrastructure.GraphApi
{
    /// <summary>
    /// Provider to get the access token 
    /// </summary>
    public class GraphAuthProvider : IGraphAuthProvider
    {
        private readonly IMemoryCache _memoryCache;
        private TokenCache _userTokenCache;
        private TokenCache _appTokenCache;
        private readonly IAzureKeyVaultService _azureKeyVaultService;

        // Properties used to get and manage an access token.
        private readonly string _clientId;
        private readonly string _aadInstance;
        private readonly ClientCredential _credential;
        private readonly string _appSecret;
        private readonly string[] _scopes;
        private readonly string[] _graphScopes;
        private readonly string _redirectUri;
        private readonly string _graphResourceId;
        private readonly string _tenantId;
        private readonly string _authority;
        private readonly IHttpContextAccessor _httpContextAccessor;


        public GraphAuthProvider(
            IMemoryCache memoryCache, 
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            IAzureKeyVaultService azureKeyVaultService)
        {
            var azureOptions = new AzureAdOptions();
            configuration.Bind("AzureAd", azureOptions);

            _clientId = azureOptions.ClientId;
            _aadInstance = azureOptions.Instance;
            _appSecret = azureOptions.ClientSecret;
            _credential = new ClientCredential(azureOptions.ClientSecret); // For development mode purposes only. Production apps should use a client certificate.
            _scopes = azureOptions.Scopes.Split(new[] { ' ' });
            _graphScopes = azureOptions.GraphScopes.Split(new[] { ' ' });
            _redirectUri = azureOptions.BaseUrl + azureOptions.CallbackPath;
            _graphResourceId = azureOptions.GraphResourceId;
            _tenantId = azureOptions.TenantId;

            _memoryCache = memoryCache;

            _authority = azureOptions.Authority;
            _httpContextAccessor = httpContextAccessor;
            _azureKeyVaultService = azureKeyVaultService;
        }

        // Gets an access token. First tries to get the access token from the token cache.
        // Using password (secret) to authenticate. Production apps should use a certificate.
        public async Task<string> GetUserAccessTokenAsync(string userId, bool appOnBehalf = false)
        {
            if (_userTokenCache == null) _userTokenCache = new TokenCache();

            var originalToken = "";

            var userContextClient = new ConfidentialClientApplication(
                _clientId,
                _redirectUri,
                _credential,
                _userTokenCache,
                null);
            
            if (String.IsNullOrEmpty(originalToken))
            {
                originalToken = await _httpContextAccessor.HttpContext.GetTokenAsync("AzureAdBearer", "access_token");
                //originalToken = _httpContextAccessor.HttpContext.Request.Headers["authorization"][0].Split(' ')[1];
            }

            var userAssertion = new UserAssertion(originalToken,
                "urn:ietf:params:oauth:grant-type:jwt-bearer");

            try
            {
                var result = await userContextClient.AcquireTokenOnBehalfOfAsync(_scopes, userAssertion);

                Guard.Against.NullOrEmpty(result.AccessToken, "GraphAuthProvider_GetUserAccessTokenAsync result.AccessToken is null or empty");

                return result.AccessToken;
            }
            catch (Exception ex)
            {
                // Unable to retrieve the access token silently.
                throw new ServiceException(new Error
                {
                    Code = GraphErrorCode.AuthenticationFailure.ToString(),
                    Message = $"GraphAuthProvider_GetUserAccessTokenAsync Caller needs to authenticate. Unable to retrieve the access token silently. error: {ex}"
                });
            }
        }


        // Gets an access token. First tries to get the access token from the token cache.
        // This app uses a password (secret) to authenticate. Production apps should use a certificate.
        public async Task<string> GetAppAccessTokenAsync()
        {
            try
            {
                if (_appTokenCache == null) _appTokenCache = new TokenCache();

                var appContextClient = new ConfidentialClientApplication(
                    _clientId,
                    _authority, 
                    _redirectUri, 
                    _credential, 
                    null,
                    _appTokenCache);

                var result = await appContextClient.AcquireTokenForClientAsync(_scopes);

                return result.AccessToken;
            }
            catch (Exception ex)
            {
                // Unable to retrieve the access token silently.
                throw new ServiceException(new Error
                {
                    Code = GraphErrorCode.AuthenticationFailure.ToString(),
                    Message = $"GetAppAccessTokenAsync Caller needs to authenticate. Unable to retrieve the access token silently. error: {ex}"
                });
            }
        }

        /// <summary>
        /// This method is used to initialize the on behalf token, currently not in use although the code is left
        /// to show how azure vault can be used to store secrets
        /// </summary>
        /// <param name="userId">UPN of the on behalf user</param>
        /// <returns>access token</returns>
        public async Task<string> SetOnBehalfAccessTokenAsync(string userId)
        {
            if (_userTokenCache == null) _userTokenCache = new TokenCache();

            var originalToken = await _httpContextAccessor.HttpContext.GetTokenAsync("AzureAdBearer", "access_token");

            var userContextClient = new ConfidentialClientApplication(
                _clientId,
                _redirectUri,
                _credential,
                _userTokenCache,
                null);

            var userAssertion = new UserAssertion(originalToken,
                "urn:ietf:params:oauth:grant-type:jwt-bearer");

            try
            {
                var result = await userContextClient.AcquireTokenOnBehalfOfAsync(_scopes, userAssertion);

                Guard.Against.NullOrEmpty(result.AccessToken, "GraphAuthProvider_SetOnBehalfAccessTokenAsync result.AccessToken is null or empty");

                //var userTokenCacheSerialized = userTokenCache.Serialize();

                // Store token in secured vault
                await _azureKeyVaultService.SetValueInVaultAsync(VaultKeys.AccessToken, result.AccessToken);
                await _azureKeyVaultService.SetValueInVaultAsync(VaultKeys.Expiration, result.ExpiresOn.ToString());
                await _azureKeyVaultService.SetValueInVaultAsync(VaultKeys.Upn, userId);

                return result.AccessToken;
            }
            catch (Exception ex)
            {
                // Unable to retrieve the access token silently.
                throw new ServiceException(new Error
                {
                    Code = GraphErrorCode.AuthenticationFailure.ToString(),
                    Message = $"GraphAuthProvider_SetOnBehalfAccessTokenAsync Caller needs to authenticate. Unable to retrieve the access token silently. error: {ex}"
                });
            }
        }
    }
}
