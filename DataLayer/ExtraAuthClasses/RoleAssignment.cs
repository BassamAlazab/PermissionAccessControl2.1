// Copyright (c) 2019 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DataLayer.EfCode;
using DataLayer.ExtraAuthClasses.Support;
using DataLayer.MultiTenantClasses;
using StatusGeneric;

namespace DataLayer.ExtraAuthClasses
{
    /// <summary>
    /// Binds a role to a user on a hierarchical tenant node, with inheritance, reach, and optional deny/JIT/delegation.
    /// </summary>
    public class RoleAssignment : IAddRemoveEffectsUser, IChangeEffectsUser
    {
        private RoleAssignment()
        {
            Exclusions = new HashSet<AssignmentExclusion>();
        }

        public RoleAssignment(
            string userId,
            RoleToPermissions role,
            TenantBase scopeTenant,
            ScopeDepth scopeDepth,
            ResourceReach resourceReach,
            AssignmentEffect effect = AssignmentEffect.Allow,
            DateTime? validFrom = null,
            DateTime? validTo = null,
            string grantedByUserId = null,
            bool isDelegated = false,
            string conditionsJson = null,
            int priority = 0)
        {
            UserId = userId ?? throw new ArgumentNullException(nameof(userId));
            Role = role ?? throw new ArgumentNullException(nameof(role));
            RoleName = role.RoleName;
            UpdateScope(scopeTenant, scopeDepth, resourceReach);
            Effect = effect;
            ValidFrom = validFrom;
            ValidTo = validTo;
            GrantedByUserId = grantedByUserId;
            IsDelegated = isDelegated;
            ConditionsJson = conditionsJson;
            Priority = priority;
            Exclusions = new HashSet<AssignmentExclusion>();
        }

        public int RoleAssignmentId { get; private set; }

        [Required(AllowEmptyStrings = false)]
        [MaxLength(ExtraAuthConstants.UserIdSize)]
        public string UserId { get; private set; }

        [Required(AllowEmptyStrings = false)]
        [MaxLength(ExtraAuthConstants.RoleNameSize)]
        public string RoleName { get; private set; }

        [ForeignKey(nameof(RoleName))]
        public RoleToPermissions Role { get; private set; }

        public int ScopeTenantId { get; private set; }

        [ForeignKey(nameof(ScopeTenantId))]
        public TenantBase ScopeTenant { get; private set; }

        public ScopeDepth ScopeDepth { get; private set; }

        public ResourceReach ResourceReach { get; private set; }

        public AssignmentEffect Effect { get; private set; }

        public DateTime? ValidFrom { get; private set; }

        public DateTime? ValidTo { get; private set; }

        [MaxLength(ExtraAuthConstants.UserIdSize)]
        public string GrantedByUserId { get; private set; }

        public bool IsDelegated { get; private set; }

        [MaxLength(2000)]
        public string ConditionsJson { get; private set; }

        public int Priority { get; private set; }

        public ICollection<AssignmentExclusion> Exclusions { get; private set; }

        public void UpdateScope(TenantBase scopeTenant, ScopeDepth scopeDepth, ResourceReach resourceReach)
        {
            if (scopeTenant == null) throw new ArgumentNullException(nameof(scopeTenant));
            if (scopeTenant.TenantItemId == 0)
                throw new InvalidOperationException("The scope tenant must already be in the database.");

            ScopeTenant = scopeTenant;
            ScopeTenantId = scopeTenant.TenantItemId;
            ScopeDepth = scopeDepth;
            ResourceReach = resourceReach;
        }

        public void UpdateValidity(DateTime? validFrom, DateTime? validTo)
        {
            ValidFrom = validFrom;
            ValidTo = validTo;
        }

        public void UpdateConditions(string conditionsJson)
        {
            ConditionsJson = conditionsJson;
        }

        public AssignmentExclusion AddExclusion(TenantBase excludedTenant)
        {
            if (excludedTenant == null) throw new ArgumentNullException(nameof(excludedTenant));
            if (excludedTenant.TenantItemId == 0)
                throw new InvalidOperationException("The excluded tenant must already be in the database.");

            Exclusions ??= new HashSet<AssignmentExclusion>();
            var exclusion = new AssignmentExclusion(this, excludedTenant);
            Exclusions.Add(exclusion);
            return exclusion;
        }

        public bool IsActiveAt(DateTime utcNow)
        {
            if (ValidFrom.HasValue && utcNow < ValidFrom.Value)
                return false;
            if (ValidTo.HasValue && utcNow > ValidTo.Value)
                return false;
            return true;
        }

        public static IStatusGeneric<RoleAssignment> Create(
            string userId,
            string roleName,
            TenantBase scopeTenant,
            ScopeDepth scopeDepth,
            ResourceReach resourceReach,
            EfCode.ExtraAuthorizeDbContext context,
            AssignmentEffect effect = AssignmentEffect.Allow,
            DateTime? validFrom = null,
            DateTime? validTo = null,
            string grantedByUserId = null,
            bool isDelegated = false,
            string conditionsJson = null,
            int priority = 0)
        {
            var status = new StatusGenericHandler<RoleAssignment>();
            if (string.IsNullOrWhiteSpace(userId))
                return status.AddError("User id is required.");
            if (scopeTenant == null)
                return status.AddError("A scope tenant is required.");

            var role = context.Find<RoleToPermissions>(roleName);
            if (role == null)
                return status.AddError($"I could not find the Role '{roleName}'.");

            if (validFrom.HasValue && validTo.HasValue && validTo < validFrom)
                return status.AddError("ValidTo cannot be earlier than ValidFrom.");

            return status.SetResult(new RoleAssignment(
                userId, role, scopeTenant, scopeDepth, resourceReach, effect,
                validFrom, validTo, grantedByUserId, isDelegated, conditionsJson, priority));
        }
    }
}
