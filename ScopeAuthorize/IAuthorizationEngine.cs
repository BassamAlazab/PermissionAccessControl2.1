// Copyright (c) 2019 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Linq;
using DataKeyParts;
using DataLayer.MultiTenantClasses;
using PermissionParts;

namespace ScopeAuthorize
{
    public interface IAuthorizationEngine
    {
        bool IsSuperAdmin(string userId);

        bool Can(string userId, Permissions permission, TenantBase resource);

        bool Can(string userId, Permissions permission, IDataKey data);

        EffectiveAccess Explain(string userId, Permissions permission, TenantBase resource);

        EffectiveAccess Explain(string userId, Permissions permission, IDataKey data);

        IQueryable<T> WhereAllowed<T>(IQueryable<T> query, string userId, Permissions permission) where T : class, IDataKey;

        IReadOnlyCollection<string> GetAllowedTenantDataKeys(string userId, Permissions permission);

        IReadOnlyCollection<string> GetAllowedOwnedDataKeys(string userId, Permissions permission);

        UserDataScope CalculateDataScope(string userId);
    }
}
