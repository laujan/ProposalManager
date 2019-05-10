// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using ApplicationCore.Entities;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace ApplicationCore.Models
{
    public class GroupModel
    {
        public GroupModel()
        {
            Id = String.Empty;
            GroupName = String.Empty;
        }

        /// <summary>
        /// Role identifier
        /// </summary>
        /// <value>Unique ID to identify the model data</value>
        [JsonProperty("id", Order = 1)]
        public string Id { get; set; }

        /// <summary>
        /// Group Name
        /// </summary>
        [JsonProperty("groupName", Order = 1)]
        public string GroupName { get; set; }

        /// <summary>
        /// Process
        /// </summary>
        [JsonProperty("processes", Order = 2)]
        public IList<ProcessesType> Processes { get; set; }

        public static GroupModel Empty
        {
            get => new GroupModel
            {
                Id = String.Empty,
                GroupName = String.Empty,
                Processes = new List<ProcessesType>()
            };
        }
    }
}
