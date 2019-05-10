// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information

using ApplicationCore;
using ApplicationCore.Serialization;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApplicationCore.ViewModels;

namespace ApplicationCore.Models
{
    public class TeamMemberModel
    {
        public TeamMemberModel()
        {
            Id = String.Empty;
            DisplayName = String.Empty;
            Mail = String.Empty;
            UserPrincipalName = String.Empty;
            Title = String.Empty;
            ProcessStep = String.Empty;
            Permissions = new List<PermissionModel>();
            RoleId = String.Empty;
            TeamsMembership = new TeamsMembershipModel();
            AdGroupName = String.Empty;
            RoleName = String.Empty;
        }

        /// <summary>
        /// Unique identifier of the artifact
        /// </summary>
        [JsonProperty("id")]
        public string Id { get; set; }

        /// <summary>
        /// Unique identifier of the artifact
        /// </summary>
        [JsonProperty("adGroupName")]
        public string AdGroupName { get; set; }

        /// <summary>
        /// User display name
        /// </summary>
        [JsonProperty("displayName")]
        public string DisplayName { get; set; }

        /// <summary>
        /// User email
        /// </summary>
        [JsonProperty("mail")]
        public string Mail { get; set; }

        /// <summary>
        /// User Principal Name
        /// </summary>
        [JsonProperty("userPrincipalName")]
        public string UserPrincipalName { get; set; }

        /// <summary>
        /// User title
        /// </summary>
        [JsonProperty("title")]
        public string Title { get; set; }

        [JsonProperty("permissions")]
        public IList<PermissionModel> Permissions { get; set; }


        [JsonProperty("teamsMembership")]
        public TeamsMembershipModel TeamsMembership { get; set; }

        [JsonProperty("processStep")]
        public string ProcessStep { get; set; }

        /// <summary>
        /// Unique identifier of the RoleId
        /// </summary>
        [JsonProperty("roleId")]
        public string RoleId { get; set; }

        [JsonProperty("roleName")]
        public string RoleName { get; set; }
    }

    public class TeamsMembershipModel
    {
        public TeamsMembershipModel()
        {
            Value = -1;
            Name = String.Empty;
        }
        /// <summary>
        /// Category identifier
        /// </summary>
        /// <value>Unique ID to identify the model data</value>
        [JsonProperty("Name", Order = 1)]
        public string Name { get; set; }

        /// <summary>
        /// Category display name
        /// </summary>
        [JsonProperty("Value", Order = 2)]
        public int Value { get; set; }
    }
}
