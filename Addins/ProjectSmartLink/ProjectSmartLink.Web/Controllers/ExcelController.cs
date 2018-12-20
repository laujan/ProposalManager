// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Localization;
using ProjectSmartLink.Web.Models;
using System.Linq;

namespace ProjectSmartLink.Web.Controllers
{
	public class ExcelController : BaseController
    {
        private readonly IStringLocalizer localizer;
        public ExcelController(IStringLocalizer<Resource> localizer, IConfiguration config) : base(config)
		{
            this.localizer = localizer;
        }

		public IActionResult Point()
        {
			var model = new AuthModel()
			{
				ApplicationId = AzureAdConfig.ClientId,
				TenantId = AzureAdConfig.TenantId,
                Resources = localizer.GetAllStrings().Select(x => new ResourceItem() { Key = x.Name, Value = System.Web.HttpUtility.JavaScriptStringEncode(x.Value) })
            };

			return View(model);
		}
    }
}