// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using ApplicationCore;
using ApplicationCore.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Identity.Client;
using Newtonsoft.Json.Linq;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace Infrastructure.Services
{

	public class ProposalManagerClientFactory : IProposalManagerClientFactory
	{

		private readonly AzureAdOptions azureAdConfiguration;
        private readonly IWebApiAuthProvider webApiAuthProvider;
		private readonly ProposalManagerConfiguration proposalManagerConfiguration;

        private string token;
		private DateTimeOffset expirationMoment = DateTimeOffset.MinValue;

		public ProposalManagerClientFactory(IWebApiAuthProvider webApiAuthProvider, IConfiguration configuration)
		{
			proposalManagerConfiguration = new ProposalManagerConfiguration();
			configuration.Bind(ProposalManagerConfiguration.ConfigurationName, proposalManagerConfiguration);
			azureAdConfiguration = new AzureAdOptions();
			configuration.Bind("AzureAd", azureAdConfiguration);
            this.webApiAuthProvider = webApiAuthProvider;
        }

        private HttpClient BuildClient()
		{
			var result = new HttpClient
			{
				BaseAddress = new Uri($"{azureAdConfiguration.BaseUrl}/api")
			};
			result.DefaultRequestHeaders.Accept.Clear();
			result.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
			return result;
		}

		public async Task<HttpClient> GetProposalManagerClientAsync()
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
                var result = await webApiAuthProvider.GetAppAccessTokenAsync();
                token = result.token;
                expirationMoment = result.expiration;
            }
		}

	}

}