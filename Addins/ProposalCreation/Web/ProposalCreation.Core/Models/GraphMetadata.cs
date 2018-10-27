// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ProposalCreation.Core.Models
{
    public class GraphMetadata
    {
		[JsonProperty("@microsoft.graph.downloadUrl")]
		public string DownloadUrl { get; set; }
		[JsonProperty("@odata.deltalink")]
		public string DeltaLink { get; set; }
	}
}
