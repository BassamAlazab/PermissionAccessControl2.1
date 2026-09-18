// Copyright (c) 2019 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace DataLayer.ExtraAuthClasses
{
    /// <summary>
    /// Whether an assignment controls the organisational node, the operational data under it, or both.
    /// </summary>
    public enum ResourceReach
    {
        /// <summary>Manage the tenant node itself (company/group/shop record) without shop stock/sales.</summary>
        ContainerOnly = 0,

        /// <summary>Includes operational data owned by matching shops (stock, sales).</summary>
        OwnedData = 1
    }
}
