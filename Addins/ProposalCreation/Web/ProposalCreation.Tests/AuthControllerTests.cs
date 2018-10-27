// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using ProposalCreation.Core.Models;
using ProposalCreationWeb.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ProposalCreation.Tests
{
	[TestClass]
	public class AuthControllerTests : TestBase
	{
		[TestMethod]
		public void WhenIndexIsCalled_ThenViewIsReturned()
		{
			var controller = new AuthController(ConfigurationProvider);
			var result = controller.Index() as ViewResult;

			Assert.IsNotNull(result);

			var model = result.Model as AuthModel;

			Assert.IsNotNull(model);

			Assert.AreEqual(ConfigurationProvider.AzureAdConfiguration.ClientId, model.ApplicationId);
		}
	}
}
