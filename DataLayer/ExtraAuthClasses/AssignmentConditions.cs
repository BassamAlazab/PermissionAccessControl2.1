// Copyright (c) 2019 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using PermissionParts;

namespace DataLayer.ExtraAuthClasses
{
    /// <summary>
    /// Optional ABAC conditions stored as JSON on a <see cref="RoleAssignment"/>.
    /// </summary>
    public class AssignmentConditions
    {
        /// <summary>Inclusive start of an allowed UTC time-of-day window, "HH:mm".</summary>
        public string FromTimeUtc { get; set; }

        /// <summary>Inclusive end of an allowed UTC time-of-day window, "HH:mm".</summary>
        public string ToTimeUtc { get; set; }

        /// <summary>User must have at least these paid modules.</summary>
        public PaidForModules? RequiredModules { get; set; }

        /// <summary>If set, the resource CLR type name must be one of these (e.g. RetailOutlet, ShopStock).</summary>
        public string[] ResourceTypeNames { get; set; }
    }
}
