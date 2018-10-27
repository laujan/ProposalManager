// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using ApplicationCore;
using ApplicationCore.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
	public class DynamicsClientFactory : IDynamicsClientFactory
	{

		private readonly AzureAdOptions azureAdConfiguration;
		private readonly Dynamics365Configuration dynamicsConfiguration;

		private string token;
		private DateTimeOffset expirationMoment = DateTimeOffset.MinValue;

		public DynamicsClientFactory(IConfiguration configuration)
		{
			dynamicsConfiguration = new Dynamics365Configuration();
			configuration.Bind(Dynamics365Configuration.ConfigurationName, dynamicsConfiguration);
			azureAdConfiguration = new AzureAdOptions();
			configuration.Bind("AzureAd", azureAdConfiguration);
		}

		private HttpClient BuildClient()
		{
			var result = new HttpClient
			{
				BaseAddress = new Uri(dynamicsConfiguration.OrganizationUri)
			};
			result.DefaultRequestHeaders.Accept.Clear();
			result.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			return result;
		}

		public async Task<HttpClient> GetDynamicsAuthorizedWebClientAsync()
		{

			await RefreshTokenIfNecessaryAsync();
			var result = BuildClient();
			result.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
			return result;

		}

		private async Task RefreshTokenIfNecessaryAsync()
		{
			if (expirationMoment <= DateTimeOffset.UtcNow.AddSeconds(-5))
			{
                var authority = $"{azureAdConfiguration.Instance}{azureAdConfiguration.TenantId}";
                var redirectUri = "http://localhost"; //mock
                var daemonClient = new ConfidentialClientApplication(azureAdConfiguration.ClientId, authority, redirectUri, new ClientCredential(azureAdConfiguration.ClientSecret), null, new TokenCache());
                var authResult = await daemonClient.AcquireTokenForClientAsync(new string[] { $"{dynamicsConfiguration.OrganizationUri}.default" });
                token = authResult.AccessToken;
                expirationMoment = authResult.ExpiresOn;
			}
		}

	}
}