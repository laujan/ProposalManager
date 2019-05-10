// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;
namespace ApplicationCore.Models
{
    public class ProcessRoleModel
    {
        /// <summary>
        /// Category identifier
        /// </summary>
        /// <value>Unique ID to identify the model data</value>
        [JsonProperty("key", Order = 1)]
        public string Key { get; set; }

        /// <summary>
        /// Category display name
        /// </summary>
        [JsonProperty("roleName", Order = 2)]
        public string RoleName { get; set; }
    }
}
