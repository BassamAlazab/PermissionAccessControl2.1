// Copyright (c) 2019 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using DataKeyParts;
using DataLayer.ExtraAuthClasses;
using DataLayer.MultiTenantClasses;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace DataLayer.EfCode.Configurations
{
    /// <summary>
    /// I need to place the configs for the databases in one place because I use context.Database.EnsureCreated to create it.
    /// This is only for a demo app - I would normally do this via SQL scripts and EFSchemaCompare
    /// https://www.thereformedprogrammer.net/handling-entity-framework-core-database-migrations-in-production-part-1/#2b-hand-coding-sql-migration-scripts
    /// </summary>
    public static class ConfigExtensions
    {
        public static void TenantBaseConfig(this ModelBuilder modelBuilder)
        {
            //for some reason ExtraAuthorizeConfig doesn't config this properly so I need to add this
            modelBuilder.Entity<TenantBase>()
                .HasDiscriminator<string>("TenantType")
                .HasValue<Company>(nameof(Company))
                .HasValue<SubGroup>(nameof(SubGroup))
                .HasValue<RetailOutlet>(nameof(RetailOutlet));
        }

        public static void ExtraAuthorizeConfig(this ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<UserToRole>().HasKey(x => new { x.UserId, x.RoleName });

            modelBuilder.Entity<RoleToPermissions>()
                .Property<string>("_permissionsInRole")
                .HasField("_permissionsInRole")
                .HasColumnName("PermissionsInRole");

            modelBuilder.Entity<RoleAssignment>(entity =>
            {
                entity.HasOne(x => x.Role)
                    .WithMany()
                    .HasForeignKey(x => x.RoleName)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasOne(x => x.ScopeTenant)
                    .WithMany()
                    .HasForeignKey(x => x.ScopeTenantId)
                    .OnDelete(DeleteBehavior.Restrict);
                entity.HasMany(x => x.Exclusions)
                    .WithOne(x => x.RoleAssignment)
                    .HasForeignKey(x => x.RoleAssignmentId)
                    .OnDelete(DeleteBehavior.Cascade);
                entity.HasIndex(x => x.UserId);
            });

            modelBuilder.Entity<AssignmentExclusion>()
                .HasOne(x => x.ExcludedTenant)
                .WithMany()
                .HasForeignKey(x => x.ExcludedTenantId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RoleHierarchy>().HasKey(x => new { x.ParentRoleName, x.ChildRoleName });
            modelBuilder.Entity<RoleHierarchy>()
                .HasOne(x => x.ParentRole)
                .WithMany()
                .HasForeignKey(x => x.ParentRoleName)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<RoleHierarchy>()
                .HasOne(x => x.ChildRole)
                .WithMany()
                .HasForeignKey(x => x.ChildRoleName)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RoleConflict>().HasKey(x => new { x.RoleNameA, x.RoleNameB });
            modelBuilder.Entity<RoleConflict>()
                .HasOne(x => x.RoleA)
                .WithMany()
                .HasForeignKey(x => x.RoleNameA)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<RoleConflict>()
                .HasOne(x => x.RoleB)
                .WithMany()
                .HasForeignKey(x => x.RoleNameB)
                .OnDelete(DeleteBehavior.Restrict);
        }

        public static void CompanyDbConfig(this ModelBuilder modelBuilder, CompanyDbContext context)
        {
            AddHierarchicalQueryFilter(modelBuilder.Entity<TenantBase>(), context, ownedData: false);
            AddHierarchicalQueryFilter(modelBuilder.Entity<ShopStock>(), context, ownedData: true);
            AddHierarchicalQueryFilter(modelBuilder.Entity<ShopSale>(), context, ownedData: true);
        }

        private static void AddHierarchicalQueryFilter<T>(EntityTypeBuilder<T> builder, CompanyDbContext context, bool ownedData) where T : class, IDataKey
        {
            // CombinedDbContext passes a null context when EnsureCreated builds the schema.
            // Skip the filter in that case so the model can still be created.
            if (context != null)
            {
                if (ownedData)
                {
                    builder.HasQueryFilter(x =>
                        context.BypassTenantFilter
                        || context.AllowedOwnedDataKeys.Contains(x.DataKey)
                        || (context.UseLegacyDataKeyFilter && x.DataKey.StartsWith(context.DataKey)));
                }
                else
                {
                    builder.HasQueryFilter(x =>
                        context.BypassTenantFilter
                        || context.AllowedTenantDataKeys.Contains(x.DataKey)
                        || (context.UseLegacyDataKeyFilter && x.DataKey.StartsWith(context.DataKey)));
                }
            }
            builder.HasIndex(x => x.DataKey);
        }
    }
}