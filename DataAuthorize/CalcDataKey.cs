// Copyright (c) 2019 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using DataLayer.EfCode;
using ScopeAuthorize;

namespace DataAuthorize
{
    public class CalcDataKey
    {
        private readonly ExtraAuthorizeDbContext _context;

        public CalcDataKey(ExtraAuthorizeDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// This looks for a DataKey for the current user, which can be missing
        /// </summary>
        /// <param name="userId"></param>
        /// <returns>The found data key, or random guid string to stop it matching anything</returns>
        public string CalcDataKeyForUser(string userId)
        {
            return CalcDataScopeForUser(userId).DataKey
                   ?? Guid.NewGuid().ToString("N");
        }

        public UserDataScope CalcDataScopeForUser(string userId)
        {
            return DataScopeCalculator.Calculate(_context, userId);
        }
    }
}
