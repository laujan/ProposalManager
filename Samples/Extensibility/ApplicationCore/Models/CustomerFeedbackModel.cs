// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using ApplicationCore.Serialization;
using Newtonsoft.Json;
using System;

namespace ApplicationCore.Models
{
	public class CustomerFeedbackModel
    {
        public CustomerFeedbackModel()
        {
            Id = String.Empty;
            CustomerFeedbackChannel = String.Empty;
            CustomerFeedbackStatus = ActionStatus.NotStarted;
        }

        [JsonProperty("id")]
        public string Id { get; set; }

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
	}

}