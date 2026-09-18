// Copyright (c) 2019 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using DataLayer.EfCode;
using DataLayer.ExtraAuthClasses;
using DataLayer.MultiTenantClasses;
using Microsoft.EntityFrameworkCore;
using PermissionParts;

namespace ScopeAuthorize
{
    public static class DataScopeCalculator
    {
        public static UserDataScope Calculate(ExtraAuthorizeDbContext context, string userId)
        {
            var result = new UserDataScope
            {
                DataKey = Guid.NewGuid().ToString("N")
            };

            if (string.IsNullOrEmpty(userId))
                return result;

            if (IsSuperAdmin(context, userId))
            {
                result.BypassTenantFilter = true;
                result.DataKey = string.Empty;
                return result;
            }

            var allTenants = context.Tenants.IgnoreQueryFilters().ToList();
            var allowTenant = new HashSet<string>();
            var allowOwned = new HashSet<string>();
            var denyTenant = new HashSet<string>();
            var denyOwned = new HashSet<string>();
            string primaryPrefix = null;

            var assignments = context.RoleAssignments
                .Include(x => x.Role)
                .Include(x => x.ScopeTenant)
                .Include(x => x.Exclusions)
                    .ThenInclude(x => x.ExcludedTenant)
                .Where(x => x.UserId == userId)
                .ToList()
                .Where(x => x.IsActiveAt(DateTime.UtcNow))
                .ToList();

            if (assignments.Any())
            {
                foreach (var assignment in assignments)
                {
                    ApplyAssignment(assignment, allTenants, allowTenant, allowOwned, denyTenant, denyOwned, ref primaryPrefix);
                }
            }
            else
            {
                var hierarchical = context.DataAccess
                    .Include(x => x.LinkedTenant)
                    .SingleOrDefault(x => x.UserId == userId);
                if (hierarchical?.LinkedTenant != null)
                {
                    var matching = ScopeMatcher.MatchingTenants(
                        hierarchical.LinkedTenant, ScopeDepth.ThisAndEntireSubtree, allTenants).ToList();
                    foreach (var tenant in matching.Where(x => x.DataKey != null))
                        allowTenant.Add(tenant.DataKey);
                    foreach (var shop in matching.OfType<RetailOutlet>().Where(x => x.DataKey != null))
                        allowOwned.Add(shop.DataKey);
                    primaryPrefix = hierarchical.LinkedTenant.DataKey;
                }
            }

            if (!allowTenant.Any() && !allowOwned.Any() && primaryPrefix == null)
                return result;

            allowTenant.ExceptWith(denyTenant);
            allowOwned.ExceptWith(denyOwned);

            result.AllowedTenantDataKeys = allowTenant.ToList();
            result.AllowedOwnedDataKeys = allowOwned.ToList();
            result.DataKey = primaryPrefix
                             ?? allowOwned.FirstOrDefault()
                             ?? allowTenant.FirstOrDefault()
                             ?? Guid.NewGuid().ToString("N");
            return result;
        }

        public static bool IsSuperAdmin(ExtraAuthorizeDbContext context, string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return false;

            if (context.UserToRoles.Any(x => x.UserId == userId && x.RoleName == ExtraAuthConstants.SuperAdminRoleName))
                return true;

            var roleNames = context.UserToRoles.Where(x => x.UserId == userId).Select(x => x.RoleName).ToList();
            roleNames.AddRange(context.RoleAssignments.Where(x => x.UserId == userId).Select(x => x.RoleName));
            foreach (var roleName in roleNames.Distinct())
            {
                if (RoleHierarchyExpander.PermissionsForRole(context, roleName).Contains(Permissions.AccessAll))
                    return true;
            }

            return false;
        }

        private static void ApplyAssignment(
            RoleAssignment assignment,
            IReadOnlyCollection<TenantBase> allTenants,
            HashSet<string> allowTenant,
            HashSet<string> allowOwned,
            HashSet<string> denyTenant,
            HashSet<string> denyOwned,
            ref string primaryPrefix)
        {
            var matching = ScopeMatcher.ApplyExclusions(
                ScopeMatcher.MatchingTenants(assignment.ScopeTenant, assignment.ScopeDepth, allTenants),
                assignment.Exclusions.Select(x => x.ExcludedTenant)).ToList();

            var tenantTarget = assignment.Effect == AssignmentEffect.Deny ? denyTenant : allowTenant;
            foreach (var tenant in matching.Where(x => x.DataKey != null))
                tenantTarget.Add(tenant.DataKey);

            if (assignment.ResourceReach == ResourceReach.OwnedData)
            {
                var ownedTarget = assignment.Effect == AssignmentEffect.Deny ? denyOwned : allowOwned;
                foreach (var shop in matching.OfType<RetailOutlet>().Where(x => x.DataKey != null))
                    ownedTarget.Add(shop.DataKey);
            }

            if (assignment.Effect == AssignmentEffect.Allow
                && assignment.ScopeDepth.IncludesEntireSubtree()
                && assignment.ScopeDepth.IncludesSelf()
                && assignment.ScopeTenant?.DataKey != null
                && primaryPrefix == null)
            {
                primaryPrefix = assignment.ScopeTenant.DataKey;
            }
        }
    }
}
