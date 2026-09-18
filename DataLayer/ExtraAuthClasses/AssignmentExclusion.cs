// Copyright (c) 2019 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations.Schema;
using DataLayer.ExtraAuthClasses.Support;
using DataLayer.MultiTenantClasses;

namespace DataLayer.ExtraAuthClasses
{
    /// <summary>
    /// Removes a descendant node (and its own descendants) from an inherited assignment.
    /// </summary>
    public class AssignmentExclusion : IAddRemoveEffectsUser, IChangeEffectsUser
    {
        private AssignmentExclusion() { }

        public AssignmentExclusion(RoleAssignment assignment, TenantBase excludedTenant)
        {
            RoleAssignment = assignment ?? throw new ArgumentNullException(nameof(assignment));
            ExcludedTenant = excludedTenant ?? throw new ArgumentNullException(nameof(excludedTenant));
            ExcludedTenantId = excludedTenant.TenantItemId;
        }

        public int AssignmentExclusionId { get; private set; }

        public int RoleAssignmentId { get; private set; }

        [ForeignKey(nameof(RoleAssignmentId))]
        public RoleAssignment RoleAssignment { get; private set; }

        public int ExcludedTenantId { get; private set; }

        [ForeignKey(nameof(ExcludedTenantId))]
        public TenantBase ExcludedTenant { get; private set; }
    }
}
