// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using ProposalCreation.Core.Configuration;
using ProposalCreation.Core.Models;
using ProposalCreation.Core.Providers;
using Microsoft.AspNetCore.Mvc;

namespace ProposalCreationWeb.Controllers
{
	public class AuthController : Controller
	{
		private readonly AzureAdConfiguration azureAdOptions;

		public AuthController(IRootConfigurationProvider rootConfigurationProvider)
		{
			// Get from config
			azureAdOptions = rootConfigurationProvider.AzureAdConfiguration;
			
		}
		public IActionResult Index()
		{
			var model = new AuthModel() { ApplicationId = azureAdOptions.ClientId };
			return View(model);
		}

		public IActionResult End()
		{
			return View();
		}

	}
}