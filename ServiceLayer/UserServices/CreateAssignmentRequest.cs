// Copyright (c) 2019 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using DataLayer.ExtraAuthClasses;

namespace ServiceLayer.UserServices
{
    public class CreateAssignmentRequest
    {
        public string UserId { get; set; }
        public string RoleName { get; set; }
        public int ScopeTenantId { get; set; }
        public ScopeDepth ScopeDepth { get; set; } = ScopeDepth.ThisAndEntireSubtree;
        public ResourceReach ResourceReach { get; set; } = ResourceReach.OwnedData;
        public AssignmentEffect Effect { get; set; } = AssignmentEffect.Allow;
        public DateTime? ValidFrom { get; set; }
        public DateTime? ValidTo { get; set; }
        public string ConditionsJson { get; set; }
        public int? ExcludeTenantId { get; set; }
        public string GrantedByUserId { get; set; }
        public bool IsDelegated { get; set; }
    }
}
