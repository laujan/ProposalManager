// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

namespace ApplicationCore
{
	public class OneDriveConfiguration
	{
		public const string ConfigurationName = "OneDrive";
		public string FormalProposalCallbackRelativeUrl { get; set; }
		public string AttachmentCallbackRelativeUrl { get; set; }
		public string WebhookSecret { get; set; }
	}
}