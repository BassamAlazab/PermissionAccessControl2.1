// Copyright (c) 2019 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;
using DataKeyParts;

namespace Test.FakesAndMocks
{
    public class FakeGetClaimsProvider : IGetClaimsProvider
    {
        public FakeGetClaimsProvider(string dataKey,
            bool bypassTenantFilter = false,
            IReadOnlyList<string> allowedTenantDataKeys = null,
            IReadOnlyList<string> allowedOwnedDataKeys = null,
            string userId = null)
        {
            DataKey = dataKey;
            BypassTenantFilter = bypassTenantFilter;
            AllowedTenantDataKeys = allowedTenantDataKeys;
            AllowedOwnedDataKeys = allowedOwnedDataKeys;
            UserId = userId;
        }

        public string DataKey { get; }
        public string UserId { get; }
        public bool BypassTenantFilter { get; }
        public IReadOnlyList<string> AllowedTenantDataKeys { get; }
        public IReadOnlyList<string> AllowedOwnedDataKeys { get; }
    }
}
