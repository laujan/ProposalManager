// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using ProposalCreation.Core.Configuration;
using ProposalCreation.Core.Helpers;
using ProposalCreation.Core.Providers;
using Microsoft.Extensions.Configuration;
using Microsoft.Graph;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace ProposalCreation.Tests
{
	[TestClass]
	public class TestBase
	{
		protected IRootConfigurationProvider ConfigurationProvider { get; private set; }
		[TestInitialize]
		public virtual void Setup()
		{
			ConfigurationProvider = ConfigHelper.GetRootConfigurationProvider(System.IO.Directory.GetCurrentDirectory());
		}

		protected Mock<IGraphSdkHelper> GetGraphSdkHelper(HttpResponseMessage expectedResponse)
		{
			var mockHttpProvider = new Mock<IHttpProvider>();
			mockHttpProvider.Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>())).Returns(Task.FromResult(expectedResponse));

			var mockAuthenticationProvider = new Mock<IAuthenticationProvider>();

			var mockGraphClient = new Mock<IGraphServiceClient>();
			mockGraphClient.Setup(x => x.AuthenticationProvider).Returns(mockAuthenticationProvider.Object);
			mockGraphClient.Setup(x => x.HttpProvider).Returns(mockHttpProvider.Object);

			var mockGraphHelper = new Mock<IGraphSdkHelper>();

			mockGraphHelper.Setup(x => x.GetAuthenticatedClient()).Returns(mockGraphClient.Object);

			var mockHttpHandler = new MockHttpMessageHandler(expectedResponse);

			var mockHttpClient = new HttpClient(mockHttpHandler)
			{
				BaseAddress = new Uri("https://mock.net")
			};

			mockGraphHelper.Setup(x => x.GetProposalManagerWebClientAsync()).Returns(Task.FromResult(mockHttpClient));

			return mockGraphHelper;
		}

		protected Mock<IDaemonHelper> GetDaemonHelper(HttpResponseMessage expectedResponse)
		{
			var mockHttpProvider = new Mock<IHttpProvider>();
			mockHttpProvider.Setup(x => x.SendAsync(It.IsAny<HttpRequestMessage>())).Returns(Task.FromResult(expectedResponse));

			var mockDaemonHelper = new Mock<IDaemonHelper>();

			var mockHttpHandler = new MockHttpMessageHandler(expectedResponse);

			var mockHttpClient = new HttpClient(mockHttpHandler)
			{
				BaseAddress = new Uri("https://mock.net")
			};

			mockDaemonHelper.Setup(x => x.GetProposalManagerAuthorizedWebClientAsync()).Returns(Task.FromResult(mockHttpClient));

			return mockDaemonHelper;
		}

		protected string ReadContentFromFile(string fileName)
		{
			return System.IO.File.ReadAllText(Path.Combine(System.IO.Directory.GetCurrentDirectory(), "JsonSample", fileName));
		}
	}

	internal class ConfigHelper
	{

		public static IConfigurationRoot GetConfig(string path)
		{
			return new ConfigurationBuilder()
			.SetBasePath(path)
			.AddJsonFile("appsettings.json", optional: true)
			.Build();
		}

		public static IRootConfigurationProvider GetRootConfigurationProvider(string path)
		{
			var config = GetConfig(path);
			return new RootConfigurationProvider(
				azureAdConfigurationProvider: new ConventionBasedConfigurationProvider<AzureAdConfiguration>(config),
				generalConfigurationProvider: new ConventionBasedConfigurationProvider<GeneralConfiguration>(config),
				proposalManagerConfigurationProvider: new ConventionBasedConfigurationProvider<ProposalManagerConfiguration>(config)
			);
		}

	}

	internal class MockHttpMessageHandler : HttpMessageHandler
	{
		private readonly HttpResponseMessage expectedResponse;
		public MockHttpMessageHandler(HttpResponseMessage expectedResponse)
		{
			this.expectedResponse = expectedResponse;
		}

		protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
		{
			return Task.FromResult(expectedResponse);
		}
	}
}