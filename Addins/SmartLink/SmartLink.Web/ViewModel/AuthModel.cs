// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using System.Collections.Generic;

namespace SmartLink.Web.ViewModel
{
    public class AuthModel
	{
		public string ApplicationId { get; set; }
		public string TenantId { get; set; }
        public IEnumerable<ResourceItem> Resources { get; set; }
        public string ApplicationName => "Project Smart Link";
    }
}