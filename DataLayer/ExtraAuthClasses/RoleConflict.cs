// Copyright (c) 2019 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using DataLayer.ExtraAuthClasses.Support;

namespace DataLayer.ExtraAuthClasses
{
    /// <summary>
    /// Separation of duties: the two roles must not be assigned to the same user on overlapping scopes.
    /// Role names are stored in alphabetical order so each pair is unique.
    /// </summary>
    public class RoleConflict : IAddRemoveEffectsUser, IChangeEffectsUser
    {
        private RoleConflict() { }

        public RoleConflict(RoleToPermissions roleA, RoleToPermissions roleB)
        {
            if (roleA == null) throw new ArgumentNullException(nameof(roleA));
            if (roleB == null) throw new ArgumentNullException(nameof(roleB));
            if (roleA.RoleName == roleB.RoleName)
                throw new InvalidOperationException("A role cannot conflict with itself.");

            if (string.Compare(roleA.RoleName, roleB.RoleName, StringComparison.Ordinal) <= 0)
            {
                RoleA = roleA;
                RoleB = roleB;
            }
            else
            {
                RoleA = roleB;
                RoleB = roleA;
            }

            RoleNameA = RoleA.RoleName;
            RoleNameB = RoleB.RoleName;
        }

        [Required(AllowEmptyStrings = false)]
        [MaxLength(ExtraAuthConstants.RoleNameSize)]
        public string RoleNameA { get; private set; }

        [ForeignKey(nameof(RoleNameA))]
        public RoleToPermissions RoleA { get; private set; }

        [Required(AllowEmptyStrings = false)]
        [MaxLength(ExtraAuthConstants.RoleNameSize)]
        public string RoleNameB { get; private set; }

        [ForeignKey(nameof(RoleNameB))]
        public RoleToPermissions RoleB { get; private set; }
    }
}
