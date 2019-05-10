// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using ApplicationCore.Serialization;
using Newtonsoft.Json;
using System;

namespace ApplicationCore.Entities
{
	public class CustomerFeedback : BaseEntity<CustomerFeedback>
    {
        /// <summary>
        /// Checklist overall status
        /// </summary>
        [JsonProperty("customerFeedbackChannel", Order = 2)]
        public string CustomerFeedbackChannel { get; set; }

        /// <summary>
        /// Checklist overall status
        /// </summary>
        [JsonConverter(typeof(StatusConverter))]
        [JsonProperty("customerFeedbackStatus", Order = 3)]
        public ActionStatus CustomerFeedbackStatus { get; set; }

        /// <summary>
        /// Represents the empty user profile. This field is read-only.
        /// </summary>
        public static CustomerFeedback Empty
        {
            get => new CustomerFeedback
            {
                Id = String.Empty,
                CustomerFeedbackStatus = ActionStatus.NotStarted,
            };
        }
    }

}