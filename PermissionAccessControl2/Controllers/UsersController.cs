// Copyright (c) 2019 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System.Linq;
using DataLayer.EfCode;
using DataLayer.ExtraAuthClasses;
using FeatureAuthorize;
using FeatureAuthorize.PolicyCode;
using GenericServices;
using Microsoft.AspNetCore.Mvc;
using PermissionParts;
using ServiceLayer.UserServices;

namespace PermissionAccessControl2.Controllers
{
    public class UsersController : Controller
    {
        public IActionResult Index()
        {
            return View(HttpContext.User);
        }

        public IActionResult Users([FromServices] IListUsersService service)
        {
            return View(service.ListUserWithRolesAndDataTenant());
        }

        [HasPermission(Permissions.RoleRead)]
        public IActionResult AllRoles([FromServices] ICrudServices<ExtraAuthorizeDbContext> services)
        {
            return View(services.ReadManyNoTracked<RoleToPermissions>().ToList());
        }

        public IActionResult UserPermissions()
        {
            return View(HttpContext.User.Claims.PermissionsFromClaims());
        }
    }
}