// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using ProposalCreationWeb;
using Microsoft.Extensions.Localization;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using ProposalCreationWeb.Controllers;

namespace ProposalCreation.Tests
{
	[TestClass]
	public class HomeControllerTests
	{
		[TestMethod]
		public void WhenIndexIsCalled_ThenViewIsReturned()
		{
			var mockStringLoc = Mock.Of<IStringLocalizer<Resource>>();
			var controller = new HomeController(mockStringLoc);
			var result = controller.Index();

			Assert.IsNotNull(result);
		}
	}
}
