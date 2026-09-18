// Copyright (c) 2019 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

namespace DataLayer.ExtraAuthClasses
{
    /// <summary>
    /// Allow or explicit Deny. Deny matching the same resource/permission wins.
    /// </summary>
    public enum AssignmentEffect
    {
        Allow = 0,
        Deny = 1
    }
}
