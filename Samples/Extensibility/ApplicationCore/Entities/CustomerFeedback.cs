// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using ApplicationCore.Helpers;
using ApplicationCore.Serialization;
using Newtonsoft.Json;

namespace ApplicationCore
{
    public class ContactMeans : SmartEnum<ContactMeans, int>
    {
        public static ContactMeans Telephone = new ContactMeans("Telephone", 0);
        public static ContactMeans Email = new ContactMeans("Email", 1);
        public static ContactMeans Meeting = new ContactMeans("Meeting", 2);
        public static ContactMeans Unkwown = new ContactMeans("Unkwown", 3);

        [JsonConstructor]
        protected ContactMeans(string name, int value) : base(name, value)
        {
        }
    }
}

namespace ApplicationCore.Serialization
{
    /// <summary>
    /// Converts a <see cref="SmartEnum"/> to and from a string.
    /// </summary>
    public class ContactMeansConverter : JsonConverter
    {
        /// <summary>
        /// Writes the JSON representation of the object.
        /// </summary>
        /// <param name="writer">The <see cref="JsonWriter"/> to write to.</param>
        /// <param name="value">The value.</param>
        /// <param name="serializer">The calling serializer.</param>
        public override void WriteJson(JsonWriter writer, object value, JsonSerializer serializer)
        {
            if (value == null)
            {
                writer.WriteNull();
            }
            else if (value is ContactMeans)
            {
                var testVar = value.ToString();
                var role = (ContactMeans)value;

                writer.WriteValue(role);
            }
            else
            {
                throw new JsonSerializationException("Expected ContactMeans object value");
            }
        }

        /// <summary>
        /// Reads the JSON representation of the object.
        /// </summary>
        /// <param name="reader">The <see cref="JsonReader"/> to read from.</param>
        /// <param name="objectType">Type of the object.</param>
        /// <param name="existingValue">The existing property value of the JSON that is being converted.</param>
        /// <param name="serializer">The calling serializer.</param>
        /// <returns>The object value.</returns>
        public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
        {
            if (reader.TokenType == JsonToken.Null)
            {
                return null;
            }
            else
            {
                if (reader.TokenType == JsonToken.Integer)
                {
                    try
                    {
                        var v = ContactMeans.FromValue(Convert.ToInt32(reader.Value));
                        return v;
                    }
                    catch (Exception ex)
                    {
                        throw new JsonSerializationException($"Error parsing version string: {ex.Message}");
                    }
                }
                else
                {
                    throw new JsonSerializationException($"Unexpected token or value when parsing version. Token: {reader.TokenType}, Value: {reader.Value}");
                }
            }
        }

        /// <summary>
        /// Determines whether this instance can convert the specified object type.
        /// </summary>
        /// <param name="objectType">Type of the object.</param>
        /// <returns>
        /// 	<c>true</c> if this instance can convert the specified object type; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanConvert(Type objectType)
        {
            //return objectType == typeof(Version);

            return true;
        }
    }
}

namespace ApplicationCore.Entities
{
    public class CustomerFeedback : BaseEntity<CustomerFeedback>
    {
        /// <summary>
        /// Channel
        /// </summary>
        [JsonProperty("customerFeedbackChannel", Order = 2)]
        public string CustomerFeedbackChannel { get; set; }

        /// <summary>
        /// Feedback list
        /// </summary>
        [JsonProperty("customerFeedbackList", Order = 3)]
        public IList<CustomerFeedbackItem> CustomerFeedbackList { get; set; }

        /// <summary>
        /// Represents the empty user profile. This field is read-only.
        /// </summary>
        public static CustomerFeedback Empty
        {
            get => new CustomerFeedback
            {
                Id = String.Empty,
                CustomerFeedbackList = new List<CustomerFeedbackItem>()
            };
        }
    }

    public class CustomerFeedbackItem
    {
        public CustomerFeedbackItem()
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
        [JsonProperty("feedbackDate", Order = 2)]
        public DateTimeOffset FeedbackDate { get; set; }

        /// <summary>
        /// Feedback contact means
        /// </summary>
        [JsonConverter(typeof(ContactMeansConverter))]
        [JsonProperty("feedbackContactMeans ", Order = 3)]
        public ContactMeans FeedbackContactMeans { get; set; }

        /// <summary>
        /// Summary of the customer feedback
        /// </summary>
        [JsonProperty("feedbackSummary", Order = 4)]
        public string FeedbackSummary { get; set; }

        /// <summary>
        /// Checklist task item completed flag
        /// </summary>
        [JsonProperty("feedbackDetails", Order = 5)]
        public string FeedbackDetails { get; set; }
    }
}
