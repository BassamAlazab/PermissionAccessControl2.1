// Copyright (c) 2019 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using DataLayer.ExtraAuthClasses;
using PermissionParts;

namespace ServiceLayer.UserServices
{
    public class EffectiveNodeAccessDto
    {
        public int TenantItemId { get; set; }
        public string Name { get; set; }
        public string TenantType { get; set; }
        public string DataKey { get; set; }
        public int Indent { get; set; }
        public List<Permissions> AllowedPermissions { get; set; } = new List<Permissions>();
        public List<string> MatchingAssignments { get; set; } = new List<string>();
        public bool SuperAdminBypass { get; set; }
    }
}
