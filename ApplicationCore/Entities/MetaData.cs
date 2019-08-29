// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace ApplicationCore.Entities
{
    public class MetaData : BaseEntity<MetaData>
    {

        /// <summary>
        /// Metadata displayName
        /// </summary>
        /// 
        [JsonProperty("displayName",Order =2)]
        public string DisplayName { get; set; }
        /// <summary>
        /// Metadata values
        /// </summary>
        /// 
        [JsonProperty("values",Order = 3)]
        public dynamic Values { get; set; }
        /// <summary>
        ///  Metadata screen.
        /// </summary>
        /// 
        [JsonProperty("screen",Order = 4)]
        public string Screen { get; set; }
        /// <summary>
        ///  Metadata fieldType.
        /// </summary>
        /// 
        [JsonProperty("fieldType", Order = 5)]
        public FieldType FieldType { get; set; }
       
        /// <summary>
        ///  Specifies if value is required
        /// </summary>
        [JsonProperty("required", Order = 6)]
        public bool Required { get; set; }
        /// <summary>
        ///  Unique identifier
        /// </summary>
        [JsonProperty("uniqueId", Order = 7)]
        public string UniqueId { get; set; }

        /// <summary>
        ///  Empty object
        /// </summary>
        /// 
        public static MetaData Empty
        {
            get => new MetaData
            {
                Id = String.Empty,
                DisplayName = String.Empty,
                FieldType = FieldType.None,
                Values = null,
                Screen = String.Empty,
                Required = false,
                UniqueId = string.Empty
            };
        }
    }
}
