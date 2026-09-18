// Copyright (c) 2019 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using DataLayer.EfCode;
using DataLayer.MultiTenantClasses;
using Microsoft.EntityFrameworkCore;
using PermissionParts;
using ScopeAuthorize;

namespace ServiceLayer.UserServices.Concrete
{
    public class EffectivePermissionsService : IEffectivePermissionsService
    {
        private readonly ExtraAuthorizeDbContext _context;
        private readonly IAuthorizationEngine _engine;

        public EffectivePermissionsService(ExtraAuthorizeDbContext context, IAuthorizationEngine engine)
        {
            _context = context;
            _engine = engine;
        }

        public IReadOnlyList<EffectiveNodeAccessDto> BuildTree(string userId)
        {
            var companies = _context.Tenants.IgnoreQueryFilters().OfType<Company>()
                .Include(x => x.Children)
                .ThenInclude(x => x.Children)
                .ThenInclude(x => x.Children)
                .ThenInclude(x => x.Children)
                .ThenInclude(x => x.Children)
                .ToList();

            var inspectPermissions = Enum.GetValues(typeof(Permissions))
                .Cast<Permissions>()
                .Where(x => x != Permissions.NotSet && x != Permissions.AccessAll)
                .Where(x => x.GetType().GetField(x.ToString())?
                    .GetCustomAttributes(typeof(ObsoleteAttribute), false).Length == 0)
                .ToList();

            var result = new List<EffectiveNodeAccessDto>();
            foreach (var company in companies)
                AddNode(result, company, userId, inspectPermissions, 0);
            return result;
        }

        private void AddNode(List<EffectiveNodeAccessDto> result, TenantBase tenant, string userId,
            List<Permissions> inspectPermissions, int indent)
        {
            var dto = new EffectiveNodeAccessDto
            {
                TenantItemId = tenant.TenantItemId,
                Name = tenant.Name,
                TenantType = tenant.GetType().Name,
                DataKey = tenant.DataKey,
                Indent = indent,
                SuperAdminBypass = _engine.IsSuperAdmin(userId)
            };

            foreach (var permission in inspectPermissions)
            {
                var explanation = _engine.Explain(userId, permission, tenant);
                if (!explanation.IsAllowed)
                    continue;
                dto.AllowedPermissions.Add(permission);
                foreach (var match in explanation.MatchingAllows)
                {
                    if (!dto.MatchingAssignments.Contains(match))
                        dto.MatchingAssignments.Add(match);
                }
                if (explanation.SuperAdminBypass && !dto.MatchingAssignments.Contains(explanation.Reason))
                    dto.MatchingAssignments.Add(explanation.Reason);
            }

            result.Add(dto);
            if (tenant.Children == null)
                return;
            foreach (var child in tenant.Children)
                AddNode(result, child, userId, inspectPermissions, indent + 1);
        }
    }
}
