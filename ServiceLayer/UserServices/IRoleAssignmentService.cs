// Copyright (c) 2019 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using DataLayer.ExtraAuthClasses;
using StatusGeneric;

namespace ServiceLayer.UserServices
{
    public interface IRoleAssignmentService
    {
        IReadOnlyList<RoleAssignment> ListAssignments(string userId = null);

        IStatusGeneric<RoleAssignment> CreateAssignment(CreateAssignmentRequest request);

        IStatusGeneric<RoleAssignment> DelegateAssignment(string granterUserId, CreateAssignmentRequest request);

        IStatusGeneric AddExclusion(int assignmentId, int excludedTenantId);

        IStatusGeneric DeleteAssignment(int assignmentId);
    }
}
