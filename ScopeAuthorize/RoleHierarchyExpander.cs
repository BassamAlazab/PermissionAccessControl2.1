// Copyright (c) 2019 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using DataLayer.EfCode;
using DataLayer.ExtraAuthClasses;
using PermissionParts;

namespace ScopeAuthorize
{
    public static class RoleHierarchyExpander
    {
        public static HashSet<string> ExpandRoleNames(ExtraAuthorizeDbContext context, IEnumerable<string> startingRoleNames)
        {
            var links = context.RoleHierarchies
                .Select(x => new { x.ParentRoleName, x.ChildRoleName })
                .ToList();

            var result = new HashSet<string>(startingRoleNames ?? Enumerable.Empty<string>());
            var queue = new Queue<string>(result);
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var child in links.Where(x => x.ParentRoleName == current).Select(x => x.ChildRoleName))
                {
                    if (result.Add(child))
                        queue.Enqueue(child);
                }
            }

            return result;
        }

        public static IReadOnlyCollection<Permissions> PermissionsForRole(ExtraAuthorizeDbContext context, string roleName)
        {
            var roleNames = ExpandRoleNames(context, new[] { roleName });
            var roles = context.RolesToPermissions.Where(x => roleNames.Contains(x.RoleName)).ToList();
            return roles.SelectMany(x => x.PermissionsInRole).Distinct().ToList();
        }
    }
}
