using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;

namespace ApplicationCore.Entities
{
    public class Permission : BaseEntity<Permission>
    {
        /// <summary>
        /// Roles Name
        /// </summary>
        [JsonProperty("name",Order =2)]
        public string Name { get; set; }
        /// <summary>
        /// Represents the empty object. This field is read-only.
        /// </summary>
        public static Permission Empty
        {
            get => new Permission
            {
                Id = string.Empty,
                Name = string.Empty
            };
        }
    }
}
