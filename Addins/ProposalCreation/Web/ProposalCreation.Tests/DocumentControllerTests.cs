// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using ProposalCreation.Core.Helpers;
using ProposalCreation.Core.Models;
using ProposalCreationWeb.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace ProposalCreation.Tests
{
	[TestClass]
	public class DocumentControllerTests : TestBase
	{
		[TestMethod]
		public void WhenListIsCalledAndResponseIsBad_ThenBadRequestIsReturned()
		{
			var response = new HttpResponseMessage(System.Net.HttpStatusCode.BadRequest);

			var mockGraphHelper = GetGraphSdkHelper(response).Object;

			var controller = new DocumentController(mockGraphHelper, ConfigurationProvider);
			var result = controller.ListAsync("param").GetAwaiter().GetResult() as BadRequestObjectResult;

			Assert.IsNotNull(result);
			StringAssert.StartsWith(result.Value.ToString(), "Error retrieving documents:");
		}

		[TestMethod]
		public void WhenListIsCalledAndExceptionOccurs_ThenBadRequestIsReturned()
		{
			var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
			{
				Content = new StringContent("")
			};

			var mockGraphHelper = GetGraphSdkHelper(response).Object;

            var controller = new DocumentController(mockGraphHelper, ConfigurationProvider);
			var result = controller.ListAsync("param").GetAwaiter().GetResult() as BadRequestObjectResult;

			Assert.IsNotNull(result);
			StringAssert.StartsWith(result.Value.ToString(), "An error occurred:");
		}

		[TestMethod]
		public void WhenListIsCalledWithNoParam_ThenBadRequestIsReturned()
		{
			var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK);

			var mockGraphHelper = GetGraphSdkHelper(response).Object;

            var controller = new DocumentController(mockGraphHelper, ConfigurationProvider);

            var result = controller.ListAsync(null).GetAwaiter().GetResult() as BadRequestObjectResult;

			Assert.IsNotNull(result);
			StringAssert.Equals(result.Value, "id is required.");
		}

		[TestMethod]
		public void WhenListIsCalledWithParam_ThenDocumentsAreReturned()
		{
			var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
			{
				Content = new StringContent(ReadContentFromFile("documents.json"))
			};

			var mockGraphHelper = GetGraphSdkHelper(response).Object;

            var controller = new DocumentController(mockGraphHelper, ConfigurationProvider);
            var result = controller.ListAsync("param").GetAwaiter().GetResult() as OkObjectResult;

			Assert.IsNotNull(result);
			var data = result.Value as IEnumerable<Document>;
			Assert.AreEqual(2, data.Count());
		}

		[TestMethod]
		public void WhenListHasNoItems_ThenEmptyDocumentsAreReturned()
		{
			var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
			{
				Content = new StringContent(ReadContentFromFile("documentsEmpty.json"))
			};

			var mockGraphHelper = GetGraphSdkHelper(response).Object;

            var controller = new DocumentController(mockGraphHelper, ConfigurationProvider);
            var result = controller.ListAsync("param").GetAwaiter().GetResult() as OkObjectResult;

			Assert.IsNotNull(result);
			var data = result.Value as IEnumerable<Document>;
			Assert.AreEqual(0, data.Count());
		}

		[TestMethod]
		public void WhenUpdateIsCalledWithInvalidOppId_ThenBadRequestIsReturned()
		{
			var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
			{
				Content = new StringContent("")
			};

			var mockGraphHelper = GetGraphSdkHelper(response).Object;

            var controller = new DocumentController(mockGraphHelper, ConfigurationProvider);
            var result = controller.UpdateTaskAsync(null, "data").GetAwaiter().GetResult() as BadRequestObjectResult;

			Assert.IsNotNull(result);
			StringAssert.Equals("opportunityId is required", result.Value.ToString());
		}

		[TestMethod]
		public void WhenUpdateIsCalledWithInvalidDocumentData_ThenBadRequestIsReturned()
		{
			var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
			{
				Content = new StringContent("")
			};

			var mockGraphHelper = GetGraphSdkHelper(response).Object;

            var controller = new DocumentController(mockGraphHelper, ConfigurationProvider);
            var result = controller.UpdateTaskAsync("id", null).GetAwaiter().GetResult() as BadRequestObjectResult;

			Assert.IsNotNull(result);
			StringAssert.Equals("documentData is required", result.Value.ToString());
		}

		[TestMethod]
		public void GetFormalProposalWithInvalidOpportunity_ThenBadRequestIsReturned()
		{
			var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
			{
				Content = new StringContent("")
			};

			var mockGraphHelper = GetGraphSdkHelper(response).Object;

            var controller = new DocumentController(mockGraphHelper, ConfigurationProvider);
            var result = controller.GetFormalProposalAsync(null).GetAwaiter().GetResult() as BadRequestObjectResult;

			Assert.IsNotNull(result);
			StringAssert.Equals("id is required", result.Value.ToString());
		}

		[TestMethod]
		public void GetFormalProposalWithValidOpportunity_ThenOkIsReturned()
		{
			var documentData = JsonConvert.SerializeObject(new OpportunityViewModel()
			{
				Id = "1",
				DisplayName = "Display Name"
			}, new JsonSerializerSettings
			{
				ContractResolver = new CamelCasePropertyNamesContractResolver()
			});

			var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
			{
				Content = new StringContent(documentData)
			};

			var mockGraphHelper = GetGraphSdkHelper(response).Object;

			var mockHttpClient = new HttpClient(new MockHttpMessageHandler(response));

            var controller = new DocumentController(mockGraphHelper, ConfigurationProvider);
            var result = controller.GetFormalProposalAsync("id").GetAwaiter().GetResult() as OkObjectResult;

			Assert.IsNotNull(result);
			StringAssert.Equals(documentData, result.Value.ToString());
		}

		[TestMethod]
		public void WhenUpdateIsCalledWithCorrectData_ThenBadRequestIsReturned()
		{
			var documentData = JsonConvert.SerializeObject(new OpportunityViewModel()
			{
				Id = "1",
				DisplayName = "Display Name"
			});

			var response = new HttpResponseMessage(System.Net.HttpStatusCode.OK)
			{
				Content = new StringContent(documentData)
			};

			var mockGraphHelper = GetGraphSdkHelper(response).Object;

			var mockHttpClient = new HttpClient(new MockHttpMessageHandler(response));

            var controller = new DocumentController(mockGraphHelper, ConfigurationProvider);
            var result = controller.UpdateTaskAsync("id", documentData).GetAwaiter().GetResult() as OkResult;

			Assert.IsNotNull(result);
		}
	}
}
