// Copyright (c) 2019 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using DataLayer.EfCode;
using DataLayer.ExtraAuthClasses;
using DataLayer.MultiTenantClasses;
using Microsoft.EntityFrameworkCore;
using PermissionParts;

[assembly: InternalsVisibleTo("Test")]

namespace ServiceLayer.UserServices
{
    /// <summary>
    /// These contain the individual methods to add/update the database, BUT you should call SaveChanges to update the database after using
    /// (This is different to AspNetUserExtension, where the userManger updates the database immediately)
    /// </summary>
    public class ExtraAuthUsersSetup
    {
        private readonly ExtraAuthorizeDbContext _context;

        public ExtraAuthUsersSetup(ExtraAuthorizeDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// This adds a role if not present, or updates a role if is present.
        /// </summary>
        /// <param name="roleName"></param>
        /// <param name="description"></param>
        /// <param name="permissions"></param>
        public void AddUpdateRoleToPermissions(string roleName, string description, ICollection<Permissions> permissions)
        {
            var status = RoleToPermissions.CreateRoleWithPermissions(roleName, description, permissions, _context);
            if (status.IsValid)
                //Note that CreateRoleWithPermissions will return a invalid status if the role is already present.
                _context.Add(status.Result);
            else
            {
                UpdateRole(roleName, description, permissions);
            }
        }

        /// <summary>
        /// This will update a role
        /// </summary>
        /// <param name="roleName"></param>
        /// <param name="description"></param>
        /// <param name="permissions"></param>
        public void UpdateRole(string roleName, string description, ICollection<Permissions> permissions)
        {
            var existingRole = _context.Find<RoleToPermissions>(roleName);
            if (existingRole == null)
                throw new KeyNotFoundException($"Could not find the role {roleName} to update.");
            existingRole.Update(description, permissions);
        }

        /// <summary>
        /// This ensures there is a UserToRole linking the userId to the given roleName
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="roleName"></param>
        public void CheckAddRoleToUser(string userId, string roleName)
        {
            var status = UserToRole.AddRoleToUser(userId, roleName, _context);
            if (status.IsValid)
                //we assume there is already a link to the role is the status wasn't valid
                _context.Add(status.Result);
        }

        /// <summary>
        /// Adds a scoped role assignment if an identical one is not already present.
        /// </summary>
        public RoleAssignment CheckAddRoleAssignment(string userId, string roleName, TenantBase scopeTenant,
            ScopeDepth scopeDepth = ScopeDepth.ThisAndEntireSubtree,
            ResourceReach resourceReach = ResourceReach.OwnedData,
            AssignmentEffect effect = AssignmentEffect.Allow,
            string excludeTenantName = null)
        {
            var existing = _context.RoleAssignments
                .FirstOrDefault(x => x.UserId == userId
                                     && x.RoleName == roleName
                                     && x.ScopeTenantId == scopeTenant.TenantItemId
                                     && x.ScopeDepth == scopeDepth
                                     && x.ResourceReach == resourceReach
                                     && x.Effect == effect);
            if (existing != null)
                return existing;

            var status = RoleAssignment.Create(userId, roleName, scopeTenant, scopeDepth, resourceReach, _context, effect);
            if (!status.IsValid)
                throw new InvalidOperationException(status.GetAllErrors());

            if (!string.IsNullOrEmpty(excludeTenantName))
            {
                var excluded = _context.Tenants.IgnoreQueryFilters().SingleOrDefault(x => x.Name == excludeTenantName);
                if (excluded == null)
                    throw new InvalidOperationException($"Could not find tenant '{excludeTenantName}' to exclude.");
                status.Result.AddExclusion(excluded);
            }

            _context.Add(status.Result);
            return status.Result;
        }

        public void CheckAddRoleHierarchy(string parentRoleName, string childRoleName)
        {
            var existing = _context.Find<RoleHierarchy>(parentRoleName, childRoleName);
            if (existing != null)
                return;
            var parent = _context.Find<RoleToPermissions>(parentRoleName);
            var child = _context.Find<RoleToPermissions>(childRoleName);
            if (parent == null || child == null)
                throw new InvalidOperationException($"Could not find roles '{parentRoleName}' and/or '{childRoleName}'.");
            _context.Add(new RoleHierarchy(parent, child));
        }

        public void CheckAddRoleConflict(string roleNameA, string roleNameB)
        {
            var roleA = _context.Find<RoleToPermissions>(roleNameA);
            var roleB = _context.Find<RoleToPermissions>(roleNameB);
            if (roleA == null || roleB == null)
                throw new InvalidOperationException($"Could not find roles '{roleNameA}' and/or '{roleNameB}'.");
            var conflict = new RoleConflict(roleA, roleB);
            if (_context.Find<RoleConflict>(conflict.RoleNameA, conflict.RoleNameB) != null)
                return;
            _context.Add(conflict);
        }

        /// <summary>
        /// This adds a UserDataHierarchical if not present, or updates the linked tenant if is present.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="linkedTenant"></param>
        public void AddUpdateDataAccessHierarchical(string userId, TenantBase linkedTenant)
        {
            if (linkedTenant == null) throw new ArgumentNullException(nameof(linkedTenant));

            var dataLink = _context.Find<UserDataHierarchical>(userId);
            if (dataLink == null)
            {
                dataLink = new UserDataHierarchical(userId, linkedTenant);
                _context.Add(dataLink);
            }
            else
            {
                dataLink.Update(linkedTenant);
            }
        }

        /// <summary>
        /// This adds if not present a ModulesForUser for a user, using the user's Company's Modules settings.
        /// </summary>
        /// <param name="userId"></param>
        /// <param name="linkToTenant"></param>
        public void CheckAddModules(string userId, TenantBase linkToTenant)
        {
            if (_context.Find<ModulesForUser>(userId) == null)
            {
                var company = _context.Find<Company>(linkToTenant.ExtractCompanyId());
                if (company == null)
                    throw new NullReferenceException($"Could not find the company with primary key of {linkToTenant.ExtractCompanyId()}.");
                var dataAccess = new ModulesForUser(userId, company.AllowedPaidForModules);
                _context.Add(dataAccess);
            }
        }


    }
}