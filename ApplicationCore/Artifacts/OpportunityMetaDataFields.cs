// Copyright(c) Microsoft Corporation. 
// All rights reserved.
// 
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using Newtonsoft.Json;
using ApplicationCore.Helpers;
using ApplicationCore.Serialization;

//WAVE-4 GENERIC ACCELERATOR Change : start
namespace ApplicationCore.Artifacts
{
    public class OpportunityMetaDataFields : ValueObject
    {
        /// <summary>
        /// Metadata displayName
        /// </summary>
        /// 
        [JsonProperty("id")]
        public string Id { get; set; }
        /// <summary>
        /// Metadata displayName
        /// </summary>
        /// 
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }
        /// <summary>
        /// Metadata values
        /// </summary>
        /// 
        [JsonProperty("values")]
        public dynamic Values { get; set; }
        /// <summary>
        ///  Metadata screen.
        /// </summary>
        /// 
        [JsonProperty("screen")]
        public string Screen { get; set; }
        /// <summary>
        ///  Metadata fieldType.
        /// </summary>
        /// 
        [JsonProperty("fieldType")]
        public FieldType FieldType { get; set; }

        /// <summary>
        ///  Empty object
        /// </summary>
        /// 

        public static OpportunityMetaDataFields Empty
        {
            get => new OpportunityMetaDataFields
            {
                Id = String.Empty,
                DisplayName = String.Empty,
                FieldType = FieldType.None,
                Values = null,
                Screen = String.Empty
            };
        }
    }
}
//WAVE-4 GENERIC ACCELERATOR Change : end