// Copyright (c) 2019 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace ScopeAuthorize
{
    /// <summary>
    /// Precomputed data-scope snapshot used for EF filters and cookies.
    /// </summary>
    public class UserDataScope
    {
        public string DataKey { get; set; }
        public bool BypassTenantFilter { get; set; }
        public List<string> AllowedTenantDataKeys { get; set; } = new List<string>();
        public List<string> AllowedOwnedDataKeys { get; set; } = new List<string>();
    }
}
