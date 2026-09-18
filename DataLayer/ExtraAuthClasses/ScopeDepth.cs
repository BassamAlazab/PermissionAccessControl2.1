// Copyright (c) 2019 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace DataLayer.ExtraAuthClasses
{
    /// <summary>
    /// How far a scoped role assignment reaches in the tenant hierarchy.
    /// </summary>
    public enum ScopeDepth
    {
        /// <summary>The assigned node only.</summary>
        ThisOnly = 0,

        /// <summary>Immediate children only — not the assigned node and not grandchildren.</summary>
        DirectChildren = 1,

        /// <summary>Every descendant under the assigned node, not including the node itself.</summary>
        EntireSubtree = 2,

        /// <summary>The assigned node plus its immediate children.</summary>
        ThisAndDirectChildren = 3,

        /// <summary>The assigned node plus every descendant (legacy DataKey StartsWith behaviour).</summary>
        ThisAndEntireSubtree = 4
    }
}
