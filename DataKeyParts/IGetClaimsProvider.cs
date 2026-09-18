using System.Collections.Generic;

namespace DataKeyParts
{
    public interface IGetClaimsProvider
    {
        string DataKey { get; }

        string UserId { get; }

        bool BypassTenantFilter { get; }

        /// <summary>
        /// Null means "legacy DataKey prefix filter". Empty means fail-closed (no tenant rows).
        /// </summary>
        IReadOnlyList<string> AllowedTenantDataKeys { get; }

        /// <summary>
        /// Null means "legacy DataKey prefix filter" for shop data. Empty means no shop rows.
        /// </summary>
        IReadOnlyList<string> AllowedOwnedDataKeys { get; }
    }
}
