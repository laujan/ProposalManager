// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using System.Collections.Generic;
using System.Linq;

namespace ApplicationCore
{
	public class DocumentIdActivatorConfiguration
	{
		public const string ConfigurationName = "DocumentIdActivator";
		public string WebhookAddress { get; set; }
		public string WebhookUsername { get; set; }
		public string WebhookPassword { get; set; }
	}

}