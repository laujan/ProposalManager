// Copyright(c) Microsoft Corporation. 
// All rights reserved.
//
// Licensed under the MIT license. See LICENSE file in the solution root folder for full license information.

namespace ProjectSmartLink.Service
{
    using System;
	using System.Linq;
	using Microsoft.EntityFrameworkCore;
	using ProjectSmartLink.Entity;
    public class SmartlinkDbContext : DbContext
    {
        // Your context has been configured to use a 'dbContext' connection string from your application's 
        // configuration file (App.config or Web.config). By default, this connection string targets the 
        // 'SmartLink.Service.dbContext' database on your LocalDb instance. 
        // 
        // If you wish to target a different database and/or database provider, modify the 'dbContext' 
        // connection string in the application configuration file.
        public SmartlinkDbContext(DbContextOptions<SmartlinkDbContext> options)
            : base(options)
        {
            Database.EnsureCreated();
            //Database.SetInitializer<SmartlinkDbContext>(new SmartlinkDbContextInitializer());
            //this.Configuration.LazyLoadingEnabled = false;
        }

        public virtual DbSet<SourceCatalog> SourceCatalogs { get; set; }
        public virtual DbSet<SourcePoint> SourcePoints { get; set; }
        public virtual DbSet<SourcePointGroup> SourcePointGroups { get; set; }
        public virtual DbSet<PublishedHistory> PublishedHistories { get; set; }
        public virtual DbSet<DestinationPoint> DestinationPoints { get; set; }
        public virtual DbSet<DestinationCatalog> DestinationCatalogs { get; set; }
        public virtual DbSet<CustomFormat> CustomFormats { get; set; }
        public virtual DbSet<RecentFile> RecentFiles { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<DestinationPoint>()
                .HasOne(x => x.Catalog)
                .WithMany(x => x.DestinationPoints);

            modelBuilder.Entity<PublishedHistory>()
                .HasOne(x => x.SourcePoint)
                .WithMany(x => x.PublishedHistories)
                .HasForeignKey(x => x.SourcePointId);

            modelBuilder.Entity<SourceCatalog>()
                .HasMany<SourcePoint>(x => x.SourcePoints)
                .WithOne(x => x.Catalog);

            modelBuilder.Entity<SourcePointGroup>()
                .HasMany<SourcePoint>(x => x.SourcePoints);

            modelBuilder.Entity<SourcePoint>()
                .HasMany<SourcePointGroup>(x => x.Groups);

            modelBuilder.Entity<SourcePoint>()
                .HasOne(x => x.Catalog);

            modelBuilder.Entity<DestinationPointCustomFormats>()
                  .HasKey(x => new { x.CustomFormatId, x.DestinationPointId });

            modelBuilder.Entity<DestinationPointCustomFormats>()
                .HasOne(x => x.DestinationPoint)
                .WithMany(x => x.DestinationPointCustomFormats)
                .HasForeignKey(x => x.DestinationPointId);

            modelBuilder.Entity<DestinationPointCustomFormats>()
                .HasOne(x => x.CustomFormat)
                .WithMany(x => x.DestinationPointCustomFormats)
                .HasForeignKey(x => x.CustomFormatId);

            modelBuilder.Entity<DestinationCatalog>()
                .HasMany(x => x.DestinationPoints)
                .WithOne(x => x.Catalog);

            modelBuilder.Entity<SourcePointGroup>().HasData(
                   new SourcePointGroup() { Id = 1, Name = "Current year" },
                   new SourcePointGroup() { Id = 2, Name = "Prior year" },
                   new SourcePointGroup() { Id = 3, Name = "PBP" },
                   new SourcePointGroup() { Id = 4, Name = "IC" },
                   new SourcePointGroup() { Id = 5, Name = "MPC" },
                   new SourcePointGroup() { Id = 6, Name = "Revenue" },
                   new SourcePointGroup() { Id = 7, Name = "Gross Margin" },
                   new SourcePointGroup() { Id = 8, Name = "Operating Income" },
                   new SourcePointGroup() { Id = 9, Name = "EPS" },
                   new SourcePointGroup() { Id = 10, Name = "GAAP" },
                   new SourcePointGroup() { Id = 11, Name = "Non-GAAP" },
                   new SourcePointGroup() { Id = 12, Name = "Outlook" },
                   new SourcePointGroup() { Id = 13, Name = "Momentum Statement" }
                );

            modelBuilder.Entity<CustomFormat>().HasData(
                new CustomFormat()
                {
                    Name = "ConvertToHundreds",
                    DisplayName = "Convert to hundreds",
                    Description = "Divide source point by 100 and insert 0 and decimal",
                    IsDeleted = false,
                    OrderBy = 1,
                    GroupName = "Convert to",
                    GroupOrderBy = 1
                },
                new CustomFormat()
                {
                    Name = "ConvertToThousands",
                    DisplayName = "Convert to thousands",
                    Description = "Divide source point by 1,000",
                    IsDeleted = false,
                    OrderBy = 2,
                    GroupName = "Convert to",
                    GroupOrderBy = 1
                },
                new CustomFormat()
                {
                    Name = "ConvertToMillions",
                    DisplayName = "Convert to millions",
                    Description = "Divide source point by 1,000,000",
                    IsDeleted = false,
                    OrderBy = 3,
                    GroupName = "Convert to",
                    GroupOrderBy = 1
                },
                new CustomFormat()
                {
                    Name = "ConvertToBillions",
                    DisplayName = "Convert to billions",
                    Description = "Divide source point by 1,000,000,000",
                    IsDeleted = false,
                    OrderBy = 4,
                    GroupName = "Convert to",
                    GroupOrderBy = 1
                },
                new CustomFormat()
                {
                    Name = "AddDecimalPlace",
                    DisplayName = "Add decimal place",
                    Description = "Display additional decimal place",
                    IsDeleted = true
                },
                new CustomFormat()
                {
                    Name = "ShowNegativesAsPositives",
                    DisplayName = "Show negatives as positives",
                    Description = "Multiply by -1",
                    IsDeleted = false,
                    OrderBy = 1,
                    GroupName = "Negative number",
                    GroupOrderBy = 4
                },
                new CustomFormat()
                {
                    Name = "IncludeThousandDescriptor",
                    DisplayName = "Include \"thousand\" descriptor",
                    Description = "Insert thousand after numerical value",
                    IsDeleted = false,
                    OrderBy = 2,
                    GroupName = "Descriptor",
                    GroupOrderBy = 2
                },
                new CustomFormat()
                {
                    Name = "IncludeMillionDescriptor",
                    DisplayName = "Include \"million\" descriptor",
                    Description = "Insert million after numerical value",
                    IsDeleted = false,
                    OrderBy = 3,
                    GroupName = "Descriptor",
                    GroupOrderBy = 2
                },
                new CustomFormat()
                {
                    Name = "IncludeBillionDescriptor",
                    DisplayName = "Include \"billion\" descriptor",
                    Description = "Insert billion after numerical value",
                    IsDeleted = false,
                    OrderBy = 4,
                    GroupName = "Descriptor",
                    GroupOrderBy = 2
                },
                new CustomFormat()
                {
                    Name = "IncludeDollarSymbol",
                    DisplayName = "Include $ symbol",
                    Description = "Add dollar sign to front of source point value",
                    IsDeleted = false,
                    OrderBy = 1,
                    GroupName = "Symbol",
                    GroupOrderBy = 3
                },
                new CustomFormat()
                {
                    Name = "ExcludeDollarSymbol",
                    DisplayName = "Exclude $ symbol",
                    Description = "Remove dollar sign to front of source point value",
                    IsDeleted = false,
                    OrderBy = 2,
                    GroupName = "Symbol",
                    GroupOrderBy = 3
                },
                new CustomFormat()
                {
                    Name = "DateShowLongDateFormat",
                    DisplayName = "Date: Show long date format",
                    Description = "Convert MM/DD/YYYY to Month DD, YYYY",
                    IsDeleted = false,
                    OrderBy = 1,
                    GroupName = "Date",
                    GroupOrderBy = 5
                },
                new CustomFormat()
                {
                    Name = "DateShowYearOnly",
                    DisplayName = "Date: Show year only",
                    Description = "Convert MM/DD/YYYY to YYYY",
                    IsDeleted = false,
                    OrderBy = 2,
                    GroupName = "Date",
                    GroupOrderBy = 5
                },
                new CustomFormat()
                {
                    Name = "ConvertNegativeSymbolToParenthesis",
                    DisplayName = "Convert negative symbol to parenthesis",
                    Description = "Remove '-' symbol and replace with '( )'",
                    IsDeleted = false,
                    OrderBy = 2,
                    GroupName = "Negative number",
                    GroupOrderBy = 4
                },
                new CustomFormat()
                {
                    Name = "IncludeHundredDescriptor",
                    DisplayName = "Include \"hundred\" descriptor",
                    Description = "Insert hundred after numerical value",
                    IsDeleted = false,
                    OrderBy = 1,
                    GroupName = "Descriptor",
                    GroupOrderBy = 2
                }
            );
        }
    }
}