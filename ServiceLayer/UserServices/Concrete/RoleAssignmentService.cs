// Copyright (c) 2019 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using DataLayer.EfCode;
using DataLayer.ExtraAuthClasses;
using DataLayer.MultiTenantClasses;
using Microsoft.EntityFrameworkCore;
using PermissionParts;
using ScopeAuthorize;
using StatusGeneric;

namespace ServiceLayer.UserServices.Concrete
{
    public class RoleAssignmentService : IRoleAssignmentService
    {
        private readonly ExtraAuthorizeDbContext _context;
        private readonly IAuthorizationEngine _engine;

        public RoleAssignmentService(ExtraAuthorizeDbContext context, IAuthorizationEngine engine)
        {
            _context = context;
            _engine = engine;
        }

        public IReadOnlyList<RoleAssignment> ListAssignments(string userId = null)
        {
            var query = _context.RoleAssignments
                .Include(x => x.Role)
                .Include(x => x.ScopeTenant)
                .Include(x => x.Exclusions)
                    .ThenInclude(x => x.ExcludedTenant)
                .AsQueryable();

            if (!string.IsNullOrEmpty(userId))
                query = query.Where(x => x.UserId == userId);

            return query.OrderBy(x => x.UserId).ThenBy(x => x.RoleName).ToList();
        }

        public IStatusGeneric<RoleAssignment> CreateAssignment(CreateAssignmentRequest request)
        {
            var status = new StatusGenericHandler<RoleAssignment>();
            var scope = _context.Tenants.IgnoreQueryFilters().SingleOrDefault(x => x.TenantItemId == request.ScopeTenantId);
            if (scope == null)
                return status.AddError("Scope tenant was not found.");

            var sod = CheckSeparationOfDuties(request.UserId, request.RoleName, scope);
            if (sod != null)
                return status.AddError(sod);

            var created = RoleAssignment.Create(
                request.UserId, request.RoleName, scope, request.ScopeDepth, request.ResourceReach, _context,
                request.Effect, request.ValidFrom, request.ValidTo, request.GrantedByUserId, request.IsDelegated,
                request.ConditionsJson);
            if (!created.IsValid)
                return created;

            if (request.ExcludeTenantId.HasValue)
            {
                var excluded = _context.Tenants.IgnoreQueryFilters()
                    .SingleOrDefault(x => x.TenantItemId == request.ExcludeTenantId.Value);
                if (excluded == null)
                    return status.AddError("Excluded tenant was not found.");
                created.Result.AddExclusion(excluded);
            }

            _context.Add(created.Result);
            _context.SaveChanges();
            return created;
        }

        public IStatusGeneric<RoleAssignment> DelegateAssignment(string granterUserId, CreateAssignmentRequest request)
        {
            var status = new StatusGenericHandler<RoleAssignment>();
            if (string.IsNullOrEmpty(granterUserId))
                return status.AddError("Granter is required.");
            if (request.Effect == AssignmentEffect.Deny)
                return status.AddError("Deny assignments cannot be delegated.");

            if (!_engine.IsSuperAdmin(granterUserId)
                && !_engine.Can(granterUserId, Permissions.AssignmentDelegate, LoadTenant(request.ScopeTenantId))
                && !_engine.Can(granterUserId, Permissions.UserChange, LoadTenant(request.ScopeTenantId)))
            {
                return status.AddError("You cannot delegate assignments in that scope.");
            }

            var target = LoadTenant(request.ScopeTenantId);
            if (target == null)
                return status.AddError("Scope tenant was not found.");

            if (!_engine.IsSuperAdmin(granterUserId) && !GranterCoversRequest(granterUserId, request, target))
                return status.AddError("You can only delegate a subset of your own scoped permissions, on the same or a narrower scope.");

            request.GrantedByUserId = granterUserId;
            request.IsDelegated = true;
            return CreateAssignment(request);
        }

        public IStatusGeneric AddExclusion(int assignmentId, int excludedTenantId)
        {
            var status = new StatusGenericHandler { Message = "Exclusion added." };
            var assignment = _context.RoleAssignments
                .Include(x => x.Exclusions)
                .Include(x => x.ScopeTenant)
                .SingleOrDefault(x => x.RoleAssignmentId == assignmentId);
            if (assignment == null)
                return status.AddError("Assignment was not found.");

            var excluded = _context.Tenants.IgnoreQueryFilters().SingleOrDefault(x => x.TenantItemId == excludedTenantId);
            if (excluded == null)
                return status.AddError("Excluded tenant was not found.");

            if (!ScopeMatcher.IsSameOrDescendantOf(excluded, assignment.ScopeTenant))
                return status.AddError("An exclusion must be the assigned node or one of its descendants.");

            assignment.AddExclusion(excluded);
            _context.SaveChanges();
            return status;
        }

        public IStatusGeneric DeleteAssignment(int assignmentId)
        {
            var status = new StatusGenericHandler { Message = "Assignment deleted." };
            var assignment = _context.RoleAssignments.Find(assignmentId);
            if (assignment == null)
                return status.AddError("Assignment was not found.");
            _context.Remove(assignment);
            _context.SaveChanges();
            return status;
        }

        private TenantBase LoadTenant(int tenantId) =>
            _context.Tenants.IgnoreQueryFilters().SingleOrDefault(x => x.TenantItemId == tenantId);

        private string CheckSeparationOfDuties(string userId, string roleName, TenantBase newScope)
        {
            var conflicts = _context.RoleConflicts.ToList();
            if (!conflicts.Any())
                return null;

            var existing = _context.RoleAssignments
                .Include(x => x.ScopeTenant)
                .Where(x => x.UserId == userId)
                .ToList();

            foreach (var existingAssignment in existing)
            {
                var pair = new[] { existingAssignment.RoleName, roleName }.OrderBy(x => x).ToArray();
                var conflict = conflicts.FirstOrDefault(x => x.RoleNameA == pair[0] && x.RoleNameB == pair[1]);
                if (conflict == null)
                    continue;
                if (ScopesOverlap(existingAssignment.ScopeTenant, newScope))
                    return $"Roles '{existingAssignment.RoleName}' and '{roleName}' cannot be combined on overlapping scopes (separation of duties).";
            }

            return null;
        }

        private static bool ScopesOverlap(TenantBase a, TenantBase b)
        {
            if (a?.DataKey == null || b?.DataKey == null)
                return false;
            return a.DataKey.StartsWith(b.DataKey) || b.DataKey.StartsWith(a.DataKey);
        }

        private bool GranterCoversRequest(string granterUserId, CreateAssignmentRequest request, TenantBase target)
        {
            var requestedPermissions = RoleHierarchyExpander.PermissionsForRole(_context, request.RoleName);
            if (requestedPermissions.Contains(Permissions.AccessAll))
                return false;

            var granterAssignments = _context.RoleAssignments
                .Include(x => x.Role)
                .Include(x => x.ScopeTenant)
                .Include(x => x.Exclusions)
                    .ThenInclude(x => x.ExcludedTenant)
                .Where(x => x.UserId == granterUserId && x.Effect == AssignmentEffect.Allow)
                .ToList();

            foreach (var assignment in granterAssignments)
            {
                if (!assignment.IsActiveAt(System.DateTime.UtcNow))
                    continue;
                if (!ScopeMatcher.IsSameOrDescendantOf(target, assignment.ScopeTenant))
                    continue;
                if (!ScopeMatcher.IsDepthWithin(request.ScopeDepth, assignment.ScopeDepth))
                    continue;
                if (request.ResourceReach == ResourceReach.OwnedData && assignment.ResourceReach != ResourceReach.OwnedData)
                    continue;

                var granterPermissions = RoleHierarchyExpander.PermissionsForRole(_context, assignment.RoleName);
                if (requestedPermissions.All(p => granterPermissions.Contains(p) || granterPermissions.Contains(Permissions.AccessAll)))
                    return true;
            }

            return false;
        }
    }
}
