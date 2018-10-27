// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using Microsoft.Azure;
using SmartLink.Web.Common;
using SmartLink.Web.ViewModel;
using System.Web.Mvc;

namespace SmartLink.Web.Controllers
{
    public class WordController : Controller
    {
        // GET: Word
        public ActionResult Point()
        {
            var model = new AuthModel()
            {
                ApplicationId = CloudConfigurationManager.GetSetting("ida:ClientId"),
                TenantId = CloudConfigurationManager.GetSetting("ida:TenantId"),
                Resources = ResourceHelper.GetResourceItems()
            };

            return View(model);
        }
    }
}