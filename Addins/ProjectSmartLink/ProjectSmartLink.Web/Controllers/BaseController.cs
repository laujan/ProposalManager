// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using ProjectSmartLink.Web.Extensions;
using ProjectSmartLink.Web.Models;

namespace ProjectSmartLink.Web.Controllers
{
	[Produces("application/json")]
	public class BaseController : Controller
	{
		public BaseController(IConfiguration config)
		{
			var configOptions = new AzureAdOptions();
			config.Bind("AzureAd", configOptions);
			AzureAdConfig = configOptions;
		}

		protected AzureAdOptions AzureAdConfig { private set; get; }
	}
}