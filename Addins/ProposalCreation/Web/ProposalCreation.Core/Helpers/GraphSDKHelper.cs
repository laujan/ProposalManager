// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using Microsoft.Graph;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;

namespace ProposalCreation.Core.Helpers
{
	public class GraphSdkHelper : IGraphSdkHelper
	{
		private readonly IDaemonHelper daemonHelper;
		private readonly IGraphAuthProvider authProvider;
		private GraphServiceClient graphClient;

		public GraphSdkHelper(IDaemonHelper daemonHelper, IGraphAuthProvider authProvider)
		{
			this.daemonHelper = daemonHelper;
			this.authProvider = authProvider;
		}

		public IGraphServiceClient GetAuthenticatedClient()
		{
			graphClient = new GraphServiceClient(new DelegateAuthenticationProvider(
				async requestMessage =>
				{
					// Passing tenant ID to the sample auth provider to use as a cache key
					var accessToken = await GetGraphTokenAsync();

					// Append the access token to the request
					requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
				}));

			return graphClient;
		}

		public GraphServiceClient GetDaemonClient()
		{
			graphClient = new GraphServiceClient(new DelegateAuthenticationProvider(
				async requestMessage =>
				{
					// Passing tenant ID to the sample auth provider to use as a cache key
					var accessToken = await daemonHelper.GetGraphTokenAsync();

					// Append the access token to the request
					requestMessage.Headers.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
				}));

			return graphClient;
		}

		public async Task<HttpClient> GetProposalManagerWebClientAsync()
		{
			var token = await authProvider.GetProposalManagerTokenOnBehalfOfAsync();

			var client = new HttpClient();

			client.DefaultRequestHeaders.Accept.Clear();
			client.DefaultRequestHeaders.Accept.Add(
				new MediaTypeWithQualityHeaderValue("application/json"));
			client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

			return client;
		}

		public async Task<string> GetGraphTokenAsync()
		{
			var token = await authProvider.GetTokenOnBehalfOfAsync();
			return token;
		}

	}

	public interface IGraphSdkHelper
	{
		IGraphServiceClient GetAuthenticatedClient();
		GraphServiceClient GetDaemonClient();
		Task<HttpClient> GetProposalManagerWebClientAsync();
	}

}