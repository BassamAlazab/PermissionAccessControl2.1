// Copyright (c) 2019 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DataLayer.ExtraAuthClasses.Support;

namespace DataLayer.ExtraAuthClasses
{
    /// <summary>
    /// Parent role includes all permissions of the child role (NIST hierarchical RBAC).
    /// </summary>
    public class RoleHierarchy : IAddRemoveEffectsUser, IChangeEffectsUser
    {
        private RoleHierarchy() { }

        public RoleHierarchy(RoleToPermissions parentRole, RoleToPermissions childRole)
        {
            ParentRole = parentRole ?? throw new ArgumentNullException(nameof(parentRole));
            ChildRole = childRole ?? throw new ArgumentNullException(nameof(childRole));
            ParentRoleName = parentRole.RoleName;
            ChildRoleName = childRole.RoleName;
            if (ParentRoleName == ChildRoleName)
                throw new InvalidOperationException("A role cannot include itself.");
        }

        [Required(AllowEmptyStrings = false)]
        [MaxLength(ExtraAuthConstants.RoleNameSize)]
        public string ParentRoleName { get; private set; }

        [ForeignKey(nameof(ParentRoleName))]
        public RoleToPermissions ParentRole { get; private set; }

        [Required(AllowEmptyStrings = false)]
        [MaxLength(ExtraAuthConstants.RoleNameSize)]
        public string ChildRoleName { get; private set; }

        [ForeignKey(nameof(ChildRoleName))]
        public RoleToPermissions ChildRole { get; private set; }
    }
}
