// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApplicationCore;
using ApplicationCore.Serialization;
using Newtonsoft.Json;

namespace ApplicationCore.Models
{
    public class CustomerFeedbackModel
    {
        public CustomerFeedbackModel()
        {
            Id = String.Empty;
            CustomerFeedbackChannel = string.Empty;
            CustomerFeedbackList = new List<CustomerFeedbackItemModel>();
        }

        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Channel
        /// </summary>
        [JsonProperty("customerFeedbackChannel", Order = 2)]
        public string CustomerFeedbackChannel { get; set; }

        /// <summary>
        /// Feedback list
        /// </summary>
        [JsonProperty("customerFeedbackList", Order = 3)]
        public IList<CustomerFeedbackItemModel> CustomerFeedbackList { get; set; }
    }

    public class CustomerFeedbackItemModel
    {
        public CustomerFeedbackItemModel()
        {
            Id = String.Empty;
            FeedbackDate = DateTimeOffset.MinValue;
            FeedbackContactMeans = ContactMeans.Unkwown;
            FeedbackSummary = String.Empty;
            FeedbackDetails = String.Empty;
        }

        /// <summary>
        /// Feedback Id item
        /// </summary>
        [JsonProperty("id", Order = 1)]
        public string Id { get; set; }

        /// <summary>
        /// Feedback occurence date
        /// </summary>
        [JsonProperty("feedbackDate")]
        public DateTimeOffset FeedbackDate { get; set; }

        /// <summary>
        /// Feedback contact means
        /// </summary>
        [JsonConverter(typeof(ContactMeansConverter))]
        [JsonProperty("feedbackContactMeans")]
        public ContactMeans FeedbackContactMeans { get; set; }

        /// <summary>
        /// Summary of the customer feedback
        /// </summary>
        [JsonProperty("feedbackSummary")]
        public string FeedbackSummary { get; set; }

        /// <summary>
        /// Checklist task item completed flag
        /// </summary>
        [JsonProperty("feedbackDetails")]
        public string FeedbackDetails { get; set; }
    }
}
