// Copyright (c) 2019 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using DataKeyParts;
using DataLayer.EfCode;
using DataLayer.ExtraAuthClasses;
using DataLayer.MultiTenantClasses;
using Microsoft.EntityFrameworkCore;
using PermissionParts;

namespace ScopeAuthorize
{
    public class AuthorizationEngine : IAuthorizationEngine
    {
        private readonly ExtraAuthorizeDbContext _context;

        public AuthorizationEngine(ExtraAuthorizeDbContext context)
        {
            _context = context;
        }

        public bool IsSuperAdmin(string userId) => DetectSuperAdmin(userId);

        public bool Can(string userId, Permissions permission, TenantBase resource)
            => Explain(userId, permission, resource).IsAllowed;

        public bool Can(string userId, Permissions permission, IDataKey data)
            => Explain(userId, permission, data).IsAllowed;

        public EffectiveAccess Explain(string userId, Permissions permission, TenantBase resource)
        {
            return ExplainCore(userId, permission, resource, resource, ownedData: false);
        }

        public EffectiveAccess Explain(string userId, Permissions permission, IDataKey data)
        {
            if (data is TenantBase tenant)
                return Explain(userId, permission, tenant);

            TenantBase owningShop = null;
            if (!string.IsNullOrEmpty(data?.DataKey))
            {
                owningShop = LoadTenants().FirstOrDefault(x => x.DataKey == data.DataKey);
            }

            return ExplainCore(userId, permission, owningShop ?? (object)data, data, ownedData: true);
        }

        public IQueryable<T> WhereAllowed<T>(IQueryable<T> query, string userId, Permissions permission) where T : class, IDataKey
        {
            if (DetectSuperAdmin(userId))
                return query;

            var ownedData = typeof(IShopLevelDataKey).IsAssignableFrom(typeof(T));
            var keys = ownedData
                ? GetAllowedOwnedDataKeys(userId, permission)
                : GetAllowedTenantDataKeys(userId, permission);

            if (keys.Count == 0)
                return query.Where(x => false);

            var keyArray = keys.ToArray();
            return query.Where(x => keyArray.Contains(x.DataKey));
        }

        public IReadOnlyCollection<string> GetAllowedTenantDataKeys(string userId, Permissions permission)
            => CalculatePermissionKeys(userId, permission).TenantKeys;

        public IReadOnlyCollection<string> GetAllowedOwnedDataKeys(string userId, Permissions permission)
            => CalculatePermissionKeys(userId, permission).OwnedKeys;

        public UserDataScope CalculateDataScope(string userId)
            => DataScopeCalculator.Calculate(_context, userId);

        private EffectiveAccess ExplainCore(string userId, Permissions permission, object resource, IDataKey data, bool ownedData)
        {
            var result = new EffectiveAccess();
            if (string.IsNullOrEmpty(userId))
            {
                result.Reason = "No user.";
                return result;
            }

            if (DetectSuperAdmin(userId))
            {
                result.IsAllowed = true;
                result.SuperAdminBypass = true;
                result.Reason = "Super Admin bypasses tenant scope.";
                return result;
            }

            var utcNow = DateTime.UtcNow;
            var userModules = _context.ModulesForUsers.Find(userId)?.AllowedPaidForModules ?? PaidForModules.None;
            var resourceType = resource?.GetType();

            foreach (var assignment in LoadAssignments(userId))
            {
                if (!assignment.IsActiveAt(utcNow))
                    continue;
                if (!AssignmentGrantsPermission(assignment, permission))
                    continue;
                if (!AbacConditionEvaluator.IsSatisfied(assignment.ConditionsJson, utcNow, userModules, resourceType))
                    continue;

                var matches = ownedData
                    ? MatchesOwnedData(assignment, data)
                    : MatchesTenant(assignment, resource as TenantBase);
                if (!matches)
                    continue;

                var description = Describe(assignment);
                if (assignment.Effect == AssignmentEffect.Deny)
                    result.MatchingDenies.Add(description);
                else
                    result.MatchingAllows.Add(description);
            }

            if (result.MatchingDenies.Any())
            {
                result.IsAllowed = false;
                result.Reason = "Denied by " + result.MatchingDenies[0];
                return result;
            }

            if (result.MatchingAllows.Any())
            {
                result.IsAllowed = true;
                result.Reason = "Allowed by " + result.MatchingAllows[0];
                return result;
            }

            result.Reason = "No matching scoped assignment.";
            return result;
        }

        private (HashSet<string> TenantKeys, HashSet<string> OwnedKeys) CalculatePermissionKeys(string userId, Permissions permission)
        {
            var tenantKeys = new HashSet<string>();
            var ownedKeys = new HashSet<string>();
            if (DetectSuperAdmin(userId))
                return (tenantKeys, ownedKeys);

            var allTenants = LoadTenants();
            var utcNow = DateTime.UtcNow;
            var userModules = _context.ModulesForUsers.Find(userId)?.AllowedPaidForModules ?? PaidForModules.None;

            var denyTenant = new HashSet<string>();
            var denyOwned = new HashSet<string>();

            foreach (var assignment in LoadAssignments(userId))
            {
                if (!assignment.IsActiveAt(utcNow))
                    continue;
                if (!AssignmentGrantsPermission(assignment, permission))
                    continue;
                if (!AbacConditionEvaluator.IsSatisfied(assignment.ConditionsJson, utcNow, userModules, null))
                    continue;

                var matching = MatchingTenantsFor(assignment, allTenants).ToList();
                var tenantSet = assignment.Effect == AssignmentEffect.Deny ? denyTenant : tenantKeys;
                foreach (var tenant in matching)
                    tenantSet.Add(tenant.DataKey);

                if (assignment.ResourceReach != ResourceReach.OwnedData)
                    continue;

                var ownedSet = assignment.Effect == AssignmentEffect.Deny ? denyOwned : ownedKeys;
                foreach (var shop in matching.OfType<RetailOutlet>())
                    ownedSet.Add(shop.DataKey);
            }

            tenantKeys.ExceptWith(denyTenant);
            ownedKeys.ExceptWith(denyOwned);
            return (tenantKeys, ownedKeys);
        }

        private bool MatchesTenant(RoleAssignment assignment, TenantBase resource)
        {
            if (resource == null)
                return false;
            if (ScopeMatcher.IsExcluded(resource, assignment.Exclusions.Select(x => x.ExcludedTenant)))
                return false;
            return ScopeMatcher.TenantMatches(resource, assignment.ScopeTenant, assignment.ScopeDepth);
        }

        private bool MatchesOwnedData(RoleAssignment assignment, IDataKey data)
        {
            if (assignment.ResourceReach != ResourceReach.OwnedData || data?.DataKey == null)
                return false;

            var shop = LoadTenants().FirstOrDefault(x => x.DataKey == data.DataKey);
            if (shop != null && ScopeMatcher.IsExcluded(shop, assignment.Exclusions.Select(x => x.ExcludedTenant)))
                return false;

            return ScopeMatcher.OwnedDataMatches(data.DataKey, shop, assignment.ScopeTenant, assignment.ScopeDepth, assignment.ResourceReach);
        }

        private IEnumerable<TenantBase> MatchingTenantsFor(RoleAssignment assignment, IReadOnlyCollection<TenantBase> allTenants)
        {
            var matching = ScopeMatcher.MatchingTenants(assignment.ScopeTenant, assignment.ScopeDepth, allTenants);
            return ScopeMatcher.ApplyExclusions(matching, assignment.Exclusions.Select(x => x.ExcludedTenant));
        }

        private bool AssignmentGrantsPermission(RoleAssignment assignment, Permissions permission)
        {
            var permissions = RoleHierarchyExpander.PermissionsForRole(_context, assignment.RoleName);
            return permissions.Contains(permission) || permissions.Contains(Permissions.AccessAll);
        }

        private bool DetectSuperAdmin(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                return false;

            if (_context.UserToRoles.Any(x => x.UserId == userId && x.RoleName == ExtraAuthConstants.SuperAdminRoleName))
                return true;

            var roleNames = _context.UserToRoles.Where(x => x.UserId == userId).Select(x => x.RoleName).ToList();
            roleNames.AddRange(_context.RoleAssignments.Where(x => x.UserId == userId).Select(x => x.RoleName));
            foreach (var roleName in roleNames.Distinct())
            {
                if (RoleHierarchyExpander.PermissionsForRole(_context, roleName).Contains(Permissions.AccessAll))
                    return true;
            }

            return false;
        }

        private List<RoleAssignment> LoadAssignments(string userId)
        {
            return _context.RoleAssignments
                .Include(x => x.Role)
                .Include(x => x.ScopeTenant)
                .Include(x => x.Exclusions)
                    .ThenInclude(x => x.ExcludedTenant)
                .Where(x => x.UserId == userId)
                .ToList();
        }

        private List<TenantBase> LoadTenants()
        {
            return _context.Tenants.IgnoreQueryFilters().ToList();
        }

        private static string Describe(RoleAssignment assignment)
        {
            var scopeName = assignment.ScopeTenant?.Name ?? assignment.ScopeTenantId.ToString();
            return $"{assignment.Effect} {assignment.RoleName} on {scopeName} ({assignment.ScopeDepth}/{assignment.ResourceReach})";
        }
    }
}
