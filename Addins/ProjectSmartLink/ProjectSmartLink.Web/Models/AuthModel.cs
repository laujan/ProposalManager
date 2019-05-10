// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ProjectSmartLink.Web.Models
{
	public class AuthModel
	{
		public string ApplicationId { get; set; }
		public string TenantId { get; set; }
        public IEnumerable<ResourceItem> Resources { get; set; }
        public static string ApplicationName => "Project Smart Link";
    }
}