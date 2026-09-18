// Copyright (c) 2019 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using System.Security.Claims;
using DataKeyParts;

namespace ScopeAuthorize
{
    public static class DataScopeClaimBuilder
    {
        public static List<Claim> BuildClaims(UserDataScope scope)
        {
            var claims = new List<Claim>
            {
                new Claim(DataAuthConstants.HierarchicalKeyClaimName, scope.DataKey ?? string.Empty),
                new Claim(DataAuthConstants.BypassTenantFilterClaimName, scope.BypassTenantFilter ? "true" : "false"),
                new Claim(DataAuthConstants.AllowedTenantKeysClaimName, Join(scope.AllowedTenantDataKeys)),
                new Claim(DataAuthConstants.AllowedOwnedDataKeysClaimName, Join(scope.AllowedOwnedDataKeys))
            };
            return claims;
        }

        private static string Join(IEnumerable<string> keys)
        {
            if (keys == null)
                return string.Empty;
            return string.Join(",", keys);
        }
    }
}
