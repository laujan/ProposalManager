// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

using System;

namespace SourcePointCreator.Entities
{
    public enum SourcePointStatus
    {
        Created = 0,
        Deleted = 1
    }

    public enum SourceTypes
    {
        Point = 1,
        Table = 2,
        Chart = 3,
        Image = 4
    }

    public class SourcePoint : BaseEntity
    {
        public string Name { get; set; }
        public string RangeId { get; set; }

        public string Position { get; set; }
        public string Value { get; set; }
        public string Creator { get; set; }
        public DateTime Created { get; set; }
        public SourcePointStatus Status { get; set; }

        public SourceTypes SourceType { get; set; }

        public string NamePosition { get; set; }
        public string NameRangeId { get; set; }

        public Guid CatalogId { get; set; }
    }
}
