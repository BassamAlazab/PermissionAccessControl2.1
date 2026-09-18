// Copyright (c) 2019 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;

namespace DataKeyParts
{
    public class GetClaimsFromUser : IGetClaimsProvider
    {
        public GetClaimsFromUser(IHttpContextAccessor accessor)
        {
            var claims = accessor.HttpContext?.User?.Claims?.ToList();
            DataKey = claims?.SingleOrDefault(x => x.Type == DataAuthConstants.HierarchicalKeyClaimName)?.Value;
            UserId = claims?.SingleOrDefault(x => x.Type == ClaimTypes.NameIdentifier)?.Value;
            BypassTenantFilter = claims?.SingleOrDefault(x => x.Type == DataAuthConstants.BypassTenantFilterClaimName)?.Value == "true";

            var tenantClaim = claims?.SingleOrDefault(x => x.Type == DataAuthConstants.AllowedTenantKeysClaimName);
            AllowedTenantDataKeys = tenantClaim == null
                ? null
                : SplitKeys(tenantClaim.Value);

            var ownedClaim = claims?.SingleOrDefault(x => x.Type == DataAuthConstants.AllowedOwnedDataKeysClaimName);
            AllowedOwnedDataKeys = ownedClaim == null
                ? null
                : SplitKeys(ownedClaim.Value);
        }

        public string DataKey { get; }
        public string UserId { get; }
        public bool BypassTenantFilter { get; }
        public IReadOnlyList<string> AllowedTenantDataKeys { get; }
        public IReadOnlyList<string> AllowedOwnedDataKeys { get; }

        private static IReadOnlyList<string> SplitKeys(string value)
        {
            if (string.IsNullOrEmpty(value))
                return Array.Empty<string>();
            return value.Split(',', StringSplitOptions.RemoveEmptyEntries);
        }
    }
}
