﻿// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using ProjectSmartLink.Web.Models;

namespace ProjectSmartLink.Web.Controllers
{
	public class AuthController : BaseController
    {
		public AuthController(IConfiguration config) :
			base(config)
		{

		}

        public ActionResult Index()
        {
			var model = new AuthModel()
			{
				ApplicationId = AzureAdConfig.ClientId,
				TenantId = AzureAdConfig.TenantId
			};

			return View(model);
        }

		public ActionResult End()
		{
			return View();
		}
	}
}