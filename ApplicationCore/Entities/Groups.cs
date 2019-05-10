// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ApplicationCore.Entities
{
    public class Groups : BaseEntity<Groups>
    {
        /// <summary>
        /// Represents the empty client. This field is read-only.
        /// </summary>
        /// 
        public static Groups Empty
        {
            get => new Groups
            {
                Id = String.Empty,
                GroupName = String.Empty,
                Processes = new List<ProcessesType>()
            };
        }

        /// <summary>
        /// Group Name
        /// </summary>
        [JsonProperty("groupName",Order = 1)]
        public string GroupName { get; set; }

        /// <summary>
        /// Process
        /// </summary>
        [JsonProperty("processes", Order = 2)]
        public IList<ProcessesType> Processes { get; set; }

        

    }
}
