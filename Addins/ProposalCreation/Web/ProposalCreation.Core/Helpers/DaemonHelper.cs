// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using ProposalCreation.Core.Configuration;
using ProposalCreation.Core.Providers;
using Microsoft.Identity.Client;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace ProposalCreation.Core.Helpers
{

	public class DaemonHelper : IDaemonHelper
	{

		private readonly AzureAdConfiguration azureAdConfiguration;
		private readonly ProposalManagerConfiguration proposalManagerConfiguration;

		private string token;
		private DateTimeOffset expirationMoment = DateTimeOffset.MinValue;
		private static HttpClient client = new HttpClient();


		public DaemonHelper(IRootConfigurationProvider rootConfigurationProvider)
		{
			azureAdConfiguration         = rootConfigurationProvider.AzureAdConfiguration;
			proposalManagerConfiguration = rootConfigurationProvider.ProposalManagerConfiguration;
			client.BaseAddress = new Uri(proposalManagerConfiguration.ApiUrl);
			client.DefaultRequestHeaders.Accept.Clear();
			client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
		}

		private HttpClient BuildClient() => client;

		private async Task RefreshTokenIfNecessaryAsync()
		{
			if (expirationMoment <= DateTimeOffset.UtcNow.AddSeconds(-5))
			{
				var authority = $"{azureAdConfiguration.Instance}{azureAdConfiguration.TenantId}";
				var redirectUri = "http://localhost"; //mock
				var daemonClient = new ConfidentialClientApplication(azureAdConfiguration.ClientId, authority, redirectUri, new ClientCredential(azureAdConfiguration.ClientSecret), null, new TokenCache());
				var authResult = await daemonClient.AcquireTokenForClientAsync(new string[] { $"{azureAdConfiguration.ProposalManagerApiId}/.default" });
				token = authResult.AccessToken;
				expirationMoment = authResult.ExpiresOn;
			}
		}

		public async Task<string> GetGraphTokenAsync()
		{
			var authority = $"{azureAdConfiguration.Instance}{azureAdConfiguration.TenantId}";
			var redirectUri = "http://localhost"; //mock
			var daemonClient = new ConfidentialClientApplication(azureAdConfiguration.ClientId, authority, redirectUri, new ClientCredential(azureAdConfiguration.ClientSecret), null, new TokenCache());
			var authResult = await daemonClient.AcquireTokenForClientAsync(new string[] { "https://graph.microsoft.com/.default" });
			return authResult.AccessToken;
		}

		public async Task<HttpClient> GetProposalManagerAuthorizedWebClientAsync()
		{
			await RefreshTokenIfNecessaryAsync();
			var result = BuildClient();
			result.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
			return result;
		}

	}
}