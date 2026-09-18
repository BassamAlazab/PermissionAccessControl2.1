using System.Linq;
using DataLayer.EfCode;
using DataLayer.ExtraAuthClasses;
using FeatureAuthorize.PolicyCode;
using Microsoft.AspNetCore.Mvc;
using PermissionParts;
using ServiceLayer.UserServices;
using StatusGeneric;

namespace PermissionAccessControl2.Controllers
{
    public class AssignmentsController : Controller
    {
        [HasPermission(Permissions.AssignmentRead)]
        public IActionResult Index([FromServices] IRoleAssignmentService service, string userId)
        {
            ViewBag.FilterUserId = userId;
            return View(service.ListAssignments(userId));
        }

        [HttpGet]
        [HasPermission(Permissions.AssignmentChange)]
        public IActionResult Create([FromServices] ExtraAuthorizeDbContext extraContext)
        {
            PopulateLookups(extraContext);
            return View(new CreateAssignmentRequest());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission(Permissions.AssignmentChange)]
        public IActionResult Create(CreateAssignmentRequest request, [FromServices] IRoleAssignmentService service,
            [FromServices] ExtraAuthorizeDbContext extraContext)
        {
            var status = service.CreateAssignment(request);
            if (status.IsValid)
                return RedirectToAction(nameof(Index), new { userId = request.UserId });

            ModelState.AddModelError(string.Empty, status.GetAllErrors());
            PopulateLookups(extraContext);
            return View(request);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission(Permissions.AssignmentDelegate)]
        public IActionResult Delegate(CreateAssignmentRequest request, [FromServices] IRoleAssignmentService service,
            [FromServices] ExtraAuthorizeDbContext extraContext)
        {
            var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
            var status = service.DelegateAssignment(userId, request);
            if (status.IsValid)
                return RedirectToAction(nameof(Index), new { userId = request.UserId });

            ModelState.AddModelError(string.Empty, status.GetAllErrors());
            PopulateLookups(extraContext);
            return View("Create", request);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission(Permissions.AssignmentChange)]
        public IActionResult Delete(int id, [FromServices] IRoleAssignmentService service)
        {
            service.DeleteAssignment(id);
            return RedirectToAction(nameof(Index));
        }

        [HasPermission(Permissions.AssignmentRead)]
        public IActionResult Effective(string userId, [FromServices] IEffectivePermissionsService service)
        {
            ViewBag.UserId = userId;
            return View(service.BuildTree(userId));
        }

        private void PopulateLookups(ExtraAuthorizeDbContext extraContext)
        {
            ViewBag.Roles = extraContext.RolesToPermissions.OrderBy(x => x.RoleName).ToList();
            ViewBag.Tenants = extraContext.Tenants.OrderBy(x => x.Name).ToList();
        }
    }
}
