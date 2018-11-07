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
using System.Security;
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
        private PublicClientApplication _publicClientApplication;

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
        private readonly AppOptions _appOptions;


        public WebApiAuthProvider(
            IMemoryCache memoryCache, 
            IConfiguration configuration,
            IOptionsMonitor<AppOptions> appOptions,
            IHttpContextAccessor httpContextAccessor)
        {
            _appOptions = appOptions.CurrentValue;

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

            _publicClientApplication = new PublicClientApplication(_clientId, _authority);
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


        /// <summary>
        /// Security token provider using username password.
        /// Note that using username/password is not recommanded. See https://aka.ms/msal-net-up
        /// </summary>
        public async Task<(string token, DateTimeOffset expiration)> GetUserAccessTokenWithUsernamePasswordAsync()
        {
            AuthenticationResult result = null;
            var accounts = await _publicClientApplication.GetAccountsAsync();
            var scopes = new List<string>() { $"api://{proposalManagerClientId}/.default" };

            if (accounts.Any())
            {
                try
                {
                    // Attempt to get a token from the cache (or refresh it silently if needed)
                    result = await _publicClientApplication.AcquireTokenSilentAsync(scopes, accounts.FirstOrDefault());
                }
                catch (MsalUiRequiredException)
                {
                    // No token for the account. Will proceed below
                }
            }

            // Cache empty or no token for account in the cache, attempt by username/password
            if (result == null)
            {
                var password = new SecureString();
                foreach (char c in _appOptions.PBIUserPassword)
                {
                    password.AppendChar(c);
                }

                result = await GetTokenForWebApiUsingUsernamePasswordAsync(scopes, _appOptions.PBIUserName, password);
            }

            return (result.AccessToken, result.ExpiresOn);
        }

        /// <summary>
        /// Gets an access token so that the application accesses the web api in the name of the user
        /// who is signed-in in Windows (for a domain joined or AAD joined machine)
        /// </summary>
        /// <returns>An authentication result, or null if the user canceled sign-in</returns>
        private async Task<AuthenticationResult> GetTokenForWebApiUsingUsernamePasswordAsync(IEnumerable<string> scopes, string username, SecureString password)
        {
            AuthenticationResult result = null;
            try
            {
                result = await _publicClientApplication.AcquireTokenByUsernamePasswordAsync(scopes, username, password);
            }
            catch (MsalUiRequiredException ex) when (ex.Message.Contains("AADSTS65001"))
            {
                // MsalUiRequiredException: AADSTS65001: The user or administrator has not consented to use the application 
                // with ID '{appId}' named '{appName}'. Send an interactive authorization request for this user and resource.

                // Mitigation: you need to get user consent first. This can be done either statically (through the portal), or dynamically (but this
                // requires an interaction with Azure AD, which is not possible with the username/password flow)

                // Statically: in the portal by doing the following in the "API permissions" tab of the application registration: 
                // 1. Click "Add a permission" and add all the delegated permissions corresponding to the scopes you want (for instance
                // User.Read and User.ReadBasic.All)
                // 2. Click "Grant/revoke admin consent for <tenant>") and click "yes".

                // Dynamically, if you are not using .NET Core (which does not have any Web UI) by calling (once only) AcquireTokenAsync interactive. 
                // remember that Username/password is for public client applications that is desktop/mobile applications.
                // If you are using .NET core or don't want to call AcquireTokenAsync, you might want to:
                // - use device code flow (See https://aka.ms/msal-net-device-code-flow)
                // - or suggest the user to navigate to a URL to consent: https://login.microsoftonline.com/common/oauth2/v2.0/authorize?client_id={clientId}&response_type=code&scope=user.read
                throw;
            }
            catch (MsalUiRequiredException ex) when (ex.Message.Contains("AADSTS50079"))
            {
                // MsalUiRequiredException: AADSTS50079: The user is required to use multi-factor authentication.
                // The tenant admin for your organization has chosen to oblige users to perform multi-factor authentication. 
                // Mitigation: none
                // Your application cannot use the Username/Password grant. 
                // Like in the previous case, you might want to use an interactive flow (AcquireTokenAsync()), or Device Code Flow instead.

                // Note this is one of the reason why using username/password is not recommanded;
                throw;
            }
            catch (MsalUiRequiredException ex) when (ex.Message.Contains("AADSTS70002") || ex.Message.Contains("AADSTS50126"))
            {
                // Message = "AADSTS70002: Error validating credentials. AADSTS50126: Invalid username or password
                // In the case of a managed user (user from an Azure AD tenant opposed to a federated user, which would be owned
                // in another IdP through ADFS), the user has entered the wrong password

                // Mitigation: ask the user to re-enter the password
                throw new ArgumentException("U/P: Wrong password", ex);
            }
            catch (MsalClientException ex) when (ex.Message.Contains("ID3242"))
            {
                // In the case of a Federated user (that is owned by a federated IdP, as opposed to a managed user owned in an Azure AD tenant) 
                // ID3242: The security token could not be authenticated or authorized.
                // The user does not exist or has entered the wrong password
                throw new ArgumentException("U/P: Wrong username or password", ex);
            }
            catch (MsalServiceException ex) when (ex.Message.Contains("AADSTS90010"))
            {
                // MsalServiceException: AADSTS90010: The grant type is not supported over the /common or /consumers endpoints. Please use the /organizations or tenant-specific endpoint.
                // you used common.
                // Mitigation: as explained in the message from Azure AD, the authoriy you use in the application needs to be tenanted or otherwise "organizations". change the 
                // "Tenant": property in the appsettings.json to be a GUID (tenant Id), or domain name (contoso.com) if such a domain is registered with your tenant
                // or "organizations", if you want this application to sign-in users in any Work and School accounts.
                throw;
            }
            catch (MsalServiceException ex) when (ex.Message.Contains("AADSTS70002"))
            {
                // MsalServiceException: AADSTS70002: The request body must contain the following parameter: 'client_secret or client_assertion'.
                // Explanation: this can happen if your application was not registered as a public client application in Azure AD 
                // Mitigation: in the Azure portal, edit the manifest for your application and set the `allowPublicClient` to `true` 
                throw;
            }
            catch (MsalServiceException ex) when (ex.Message.Contains("ADSTS50034"))
            {
                // MsalServiceException: ADSTS50034: To sign into this application the account must be added to the {domainName} directory.
                // The user was not found in the directory
                throw new ArgumentException("U/P: Wrong username", ex);
            }
            catch (MsalServiceException)
            {
                throw;
            }

            catch (MsalClientException ex) when (ex.ErrorCode == "unknown_user_type")
            {
                // ErrorCode = "unknown_user_type"
                // Message = "Unsupported User Type 'Unknown'. Please see https://aka.ms/msal-net-up"
                // The user is not recognized as a managed user, or a federated user. Azure AD was not
                // able to identify the IdP that needs to process the user
                throw new ArgumentException("U/P: Wrong username", ex);
            }
            catch (MsalClientException ex) when (ex.ErrorCode == "user_realm_discovery_failed")
            {
                // The user is not recognized as a managed user, or a federated user. Azure AD was not
                // able to identify the IdP that needs to process the user. That's for instance the case
                // if you use a phone number
                throw new ArgumentException("U/P: Wrong username", ex);
            }
            catch (MsalClientException ex) when (ex.ErrorCode == "unknown_user")
            {
                // the username was probably empty
                // ex.Message = "Could not identify the user logged into the OS. See http://aka.ms/msal-net-iwa for details."
                throw new ArgumentException("U/P: Wrong username", ex);
            }
            catch (MsalClientException ex)
            {
                // Other client exception
                throw;
            }
            return result;
        }
    }
}
