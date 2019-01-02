// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using System.Collections.Generic;

namespace SourcePointCreator.Entities
{
    public class SourceCatalog : BaseEntity
    {
        public string Name { get; set; }

        public string DocumentId { get; set; }

        public bool IsDeleted { get; set; }

        public bool IsExternal { get; set; }

        public ICollection<SourcePoint> SourcePoints { get; set; }
    }
}
