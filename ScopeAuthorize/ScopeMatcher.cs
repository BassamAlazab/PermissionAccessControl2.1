// Copyright (c) 2019 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using DataLayer.ExtraAuthClasses;
using DataLayer.MultiTenantClasses;

namespace ScopeAuthorize
{
    public static class ScopeMatcher
    {
        public static bool IncludesSelf(this ScopeDepth depth) =>
            depth == ScopeDepth.ThisOnly
            || depth == ScopeDepth.ThisAndDirectChildren
            || depth == ScopeDepth.ThisAndEntireSubtree;

        public static bool IncludesDirectChildren(this ScopeDepth depth) =>
            depth == ScopeDepth.DirectChildren
            || depth == ScopeDepth.ThisAndDirectChildren;

        public static bool IncludesEntireSubtree(this ScopeDepth depth) =>
            depth == ScopeDepth.EntireSubtree
            || depth == ScopeDepth.ThisAndEntireSubtree;

        public static bool TenantMatches(TenantBase resource, TenantBase scope, ScopeDepth depth)
        {
            if (resource == null || scope == null || resource.DataKey == null || scope.DataKey == null)
                return false;

            if (depth.IncludesEntireSubtree())
            {
                if (depth == ScopeDepth.EntireSubtree)
                    return resource.TenantItemId != scope.TenantItemId && resource.DataKey.StartsWith(scope.DataKey);
                return resource.DataKey.StartsWith(scope.DataKey);
            }

            if (depth.IncludesSelf() && resource.TenantItemId == scope.TenantItemId)
                return true;

            if (depth.IncludesDirectChildren() && resource.ParentItemId == scope.TenantItemId)
                return true;

            return false;
        }

        public static bool IsExcluded(TenantBase resource, IEnumerable<TenantBase> excludedTenants)
        {
            if (resource?.DataKey == null || excludedTenants == null)
                return false;

            return excludedTenants.Any(excluded =>
                excluded?.DataKey != null && resource.DataKey.StartsWith(excluded.DataKey));
        }

        public static bool OwnedDataMatches(string dataKey, TenantBase owningShop, TenantBase scope, ScopeDepth depth, ResourceReach reach)
        {
            if (reach != ResourceReach.OwnedData || string.IsNullOrEmpty(dataKey) || scope?.DataKey == null)
                return false;

            if (owningShop != null)
                return TenantMatches(owningShop, scope, depth);

            if (depth.IncludesEntireSubtree())
            {
                if (depth == ScopeDepth.EntireSubtree)
                    return dataKey != scope.DataKey && dataKey.StartsWith(scope.DataKey);
                return dataKey.StartsWith(scope.DataKey);
            }

            if (depth.IncludesSelf() && dataKey == scope.DataKey)
                return true;

            // Direct children of a shop-level scope have no owned data of their own.
            return false;
        }

        public static IEnumerable<TenantBase> MatchingTenants(TenantBase scope, ScopeDepth depth, IReadOnlyCollection<TenantBase> allTenants)
        {
            if (scope == null)
                return Enumerable.Empty<TenantBase>();

            return allTenants.Where(tenant => TenantMatches(tenant, scope, depth));
        }

        public static IEnumerable<TenantBase> ApplyExclusions(IEnumerable<TenantBase> tenants, IEnumerable<TenantBase> excludedTenants)
        {
            var excluded = excludedTenants?.Where(x => x != null).ToList() ?? new List<TenantBase>();
            if (!excluded.Any())
                return tenants;

            return tenants.Where(tenant => !IsExcluded(tenant, excluded));
        }

        /// <summary>
        /// True when candidate is the same node or a descendant of ancestor.
        /// Used to ensure delegated scopes cannot be wider than the granter's scope.
        /// </summary>
        public static bool IsSameOrDescendantOf(TenantBase candidate, TenantBase ancestor)
        {
            if (candidate?.DataKey == null || ancestor?.DataKey == null)
                return false;
            return candidate.DataKey.StartsWith(ancestor.DataKey);
        }

        public static bool IsDepthWithin(ScopeDepth requested, ScopeDepth granter)
        {
            if (granter.IncludesEntireSubtree() && granter.IncludesSelf())
                return true;
            if (granter == ScopeDepth.EntireSubtree)
                return requested == ScopeDepth.EntireSubtree || requested == ScopeDepth.DirectChildren || requested == ScopeDepth.ThisOnly;
            if (granter == ScopeDepth.ThisAndDirectChildren)
                return requested == ScopeDepth.ThisAndDirectChildren || requested == ScopeDepth.DirectChildren || requested == ScopeDepth.ThisOnly;
            if (granter == ScopeDepth.DirectChildren)
                return requested == ScopeDepth.DirectChildren || requested == ScopeDepth.ThisOnly;
            return requested == ScopeDepth.ThisOnly;
        }
    }
}
