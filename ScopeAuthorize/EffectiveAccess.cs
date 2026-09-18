// Copyright (c) 2019 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Collections.Generic;

namespace ScopeAuthorize
{
    public class EffectiveAccess
    {
        public bool IsAllowed { get; set; }
        public string Reason { get; set; }
        public bool SuperAdminBypass { get; set; }
        public List<string> MatchingAllows { get; } = new List<string>();
        public List<string> MatchingDenies { get; } = new List<string>();
    }
}
