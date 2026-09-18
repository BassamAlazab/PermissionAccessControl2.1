// Copyright (c) 2019 Jon P Smith, GitHub: JonPSmith, web: http://www.thereformedprogrammer.net/
// Licensed under MIT license. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DataKeyParts;
using DataLayer.EfCode;
using DataLayer.ExtraAuthClasses;
using DataLayer.MultiTenantClasses;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PermissionAccessControl2.SeedDemo.Internal;
using PermissionParts;
using ScopeAuthorize;
using ServiceLayer.UserServices;
using ServiceLayer.UserServices.Concrete;
using Test.FakesAndMocks;
using Xunit;
using Xunit.Extensions.AssertExtensions;

namespace Test.UnitTests.ScopeAuthorizeTests
{
    public class TestAuthorizationEngine : IDisposable
    {
        private readonly SqliteConnection _connection;
        private readonly DbContextOptions<ExtraAuthorizeDbContext> _extraOptions;
        private readonly DbContextOptions<CompanyDbContext> _companyOptions;

        public TestAuthorizationEngine()
        {
            _connection = new SqliteConnection("DataSource=:memory:");
            _connection.Open();
            _extraOptions = new DbContextOptionsBuilder<ExtraAuthorizeDbContext>().UseSqlite(_connection).Options;
            _companyOptions = new DbContextOptionsBuilder<CompanyDbContext>().UseSqlite(_connection).Options;
            var combinedOptions = new DbContextOptionsBuilder<CombinedDbContext>().UseSqlite(_connection).Options;
            using var combined = new CombinedDbContext(combinedOptions);
            combined.Database.EnsureCreated();
        }

        public void Dispose() => _connection.Dispose();

        [Fact]
        public void ThisOnlyDoesNotLeakChildren()
        {
            using var extra = new ExtraAuthorizeDbContext(_extraOptions, null);
            using var company = new CompanyDbContext(_companyOptions, new FakeGetClaimsProvider("x"));
            var (companyNode, westCoast, shop) = SeedHierarchy(company, extra);

            var userId = Assign(extra, "viewer", Permissions.EmployeeRead, Reload(extra, companyNode), ScopeDepth.ThisOnly, ResourceReach.ContainerOnly);
            var engine = new AuthorizationEngine(extra);

            engine.Can(userId, Permissions.EmployeeRead, Reload(extra, companyNode)).ShouldBeTrue();
            engine.Can(userId, Permissions.EmployeeRead, Reload(extra, westCoast)).ShouldBeFalse();
            engine.Can(userId, Permissions.EmployeeRead, Reload(extra, shop)).ShouldBeFalse();
            engine.GetAllowedTenantDataKeys(userId, Permissions.EmployeeRead).Single().ShouldEqual(companyNode.DataKey);
        }

        [Fact]
        public void DirectChildrenDoesNotLeakGrandchildren()
        {
            using var extra = new ExtraAuthorizeDbContext(_extraOptions, null);
            using var company = new CompanyDbContext(_companyOptions, new FakeGetClaimsProvider("x"));
            var (companyNode, westCoast, shop) = SeedHierarchy(company, extra);

            var userId = Assign(extra, "region", Permissions.EmployeeRead, Reload(extra, companyNode), ScopeDepth.DirectChildren, ResourceReach.ContainerOnly);
            var engine = new AuthorizationEngine(extra);

            engine.Can(userId, Permissions.EmployeeRead, Reload(extra, companyNode)).ShouldBeFalse();
            engine.Can(userId, Permissions.EmployeeRead, Reload(extra, westCoast)).ShouldBeTrue();
            engine.Can(userId, Permissions.EmployeeRead, Reload(extra, shop)).ShouldBeFalse();
        }

        [Fact]
        public void ThisAndEntireSubtreeMatchesLegacyStartsWith()
        {
            using var extra = new ExtraAuthorizeDbContext(_extraOptions, null);
            using var company = new CompanyDbContext(_companyOptions, new FakeGetClaimsProvider("x"));
            var (companyNode, westCoast, shop) = SeedHierarchy(company, extra);

            var userId = Assign(extra, "director", Permissions.EmployeeRead, Reload(extra, companyNode), ScopeDepth.ThisAndEntireSubtree, ResourceReach.OwnedData);
            var engine = new AuthorizationEngine(extra);

            engine.Can(userId, Permissions.EmployeeRead, Reload(extra, companyNode)).ShouldBeTrue();
            engine.Can(userId, Permissions.EmployeeRead, Reload(extra, westCoast)).ShouldBeTrue();
            engine.Can(userId, Permissions.EmployeeRead, Reload(extra, shop)).ShouldBeTrue();
            engine.Can(userId, Permissions.StockRead, Reload(extra, shop)).ShouldBeFalse();
        }

        [Fact]
        public void ContainerOnlyDoesNotReturnShopStock()
        {
            using var extra = new ExtraAuthorizeDbContext(_extraOptions, null);
            using var company = new CompanyDbContext(_companyOptions, new FakeGetClaimsProvider("x"));
            var (companyNode, _, shop) = SeedHierarchy(company, extra);
            var stock = AddStock(company, shop);

            var userId = Assign(extra, "structure", Permissions.StockRead, Reload(extra, companyNode), ScopeDepth.ThisAndEntireSubtree, ResourceReach.ContainerOnly);
            var engine = new AuthorizationEngine(extra);

            engine.Can(userId, Permissions.StockRead, Reload(extra, companyNode)).ShouldBeTrue();
            engine.Can(userId, Permissions.StockRead, stock).ShouldBeFalse();
            engine.GetAllowedOwnedDataKeys(userId, Permissions.StockRead).Count.ShouldEqual(0);
        }

        [Fact]
        public void OwnedDataOnDirectChildrenOfShopParentReturnsShopStock()
        {
            using var extra = new ExtraAuthorizeDbContext(_extraOptions, null);
            using var company = new CompanyDbContext(_companyOptions, new FakeGetClaimsProvider("x"));
            var (companyNode, westCoast, shop) = SeedHierarchy(company, extra);
            var stock = AddStock(company, shop);
            var sanFran = company.Tenants.IgnoreQueryFilters().Single(x => x.Name == "San Fran");

            var userId = Assign(extra, "sf", Permissions.StockRead, Reload(extra, sanFran), ScopeDepth.DirectChildren, ResourceReach.OwnedData);
            var engine = new AuthorizationEngine(extra);

            engine.Can(userId, Permissions.StockRead, stock).ShouldBeTrue();
            engine.GetAllowedOwnedDataKeys(userId, Permissions.StockRead).ShouldContain(shop.DataKey);
        }

        [Fact]
        public void DenyOverridesInheritedAllow()
        {
            using var extra = new ExtraAuthorizeDbContext(_extraOptions, null);
            using var company = new CompanyDbContext(_companyOptions, new FakeGetClaimsProvider("x"));
            var (companyNode, _, shop) = SeedHierarchy(company, extra);
            var stock = AddStock(company, shop);

            var userId = "denied";
            CreateRole(extra, "reader", Permissions.StockRead);
            extra.Add(RoleAssignment.Create(userId, "reader", Reload(extra, companyNode), ScopeDepth.ThisAndEntireSubtree, ResourceReach.OwnedData, extra).Result);
            extra.Add(RoleAssignment.Create(userId, "reader", Reload(extra, shop), ScopeDepth.ThisOnly, ResourceReach.OwnedData, extra, AssignmentEffect.Deny).Result);
            extra.SaveChanges();

            var engine = new AuthorizationEngine(extra);
            engine.Can(userId, Permissions.StockRead, stock).ShouldBeFalse();
            engine.Explain(userId, Permissions.StockRead, stock).MatchingDenies.Any().ShouldBeTrue();
        }

        [Fact]
        public void SuperAdminBypassesTenantScope()
        {
            using var extra = new ExtraAuthorizeDbContext(_extraOptions, null);
            using var company = new CompanyDbContext(_companyOptions, new FakeGetClaimsProvider("x"));
            var (companyNode, _, shop) = SeedHierarchy(company, extra);

            CreateRole(extra, ExtraAuthConstants.SuperAdminRoleName, Permissions.AccessAll);
            extra.Add(new UserToRole("admin", extra.Find<RoleToPermissions>(ExtraAuthConstants.SuperAdminRoleName)));
            extra.SaveChanges();

            var engine = new AuthorizationEngine(extra);
            engine.IsSuperAdmin("admin").ShouldBeTrue();
            engine.Can("admin", Permissions.StockRead, Reload(extra, shop)).ShouldBeTrue();

            var snapshot = DataScopeCalculator.Calculate(extra, "admin");
            snapshot.BypassTenantFilter.ShouldBeTrue();

            using var filtered = new CompanyDbContext(_companyOptions,
                new FakeGetClaimsProvider(null, bypassTenantFilter: true, allowedTenantDataKeys: Array.Empty<string>(), allowedOwnedDataKeys: Array.Empty<string>()));
            filtered.Tenants.Count().ShouldEqual(company.Tenants.IgnoreQueryFilters().Count());
        }

        [Fact]
        public void UserWithNoAssignmentSeesNoData()
        {
            using var extra = new ExtraAuthorizeDbContext(_extraOptions, null);
            using var company = new CompanyDbContext(_companyOptions, new FakeGetClaimsProvider("x"));
            SeedHierarchy(company, extra);

            var snapshot = DataScopeCalculator.Calculate(extra, "nobody");
            snapshot.BypassTenantFilter.ShouldBeFalse();
            snapshot.AllowedTenantDataKeys.Any().ShouldBeFalse();

            using var filtered = new CompanyDbContext(_companyOptions,
                new FakeGetClaimsProvider(snapshot.DataKey, allowedTenantDataKeys: snapshot.AllowedTenantDataKeys, allowedOwnedDataKeys: snapshot.AllowedOwnedDataKeys));
            filtered.Tenants.Count().ShouldEqual(0);
            filtered.ShopStocks.Count().ShouldEqual(0);
        }

        [Fact]
        public void ExclusionRemovesDescendant()
        {
            using var extra = new ExtraAuthorizeDbContext(_extraOptions, null);
            using var company = new CompanyDbContext(_companyOptions, new FakeGetClaimsProvider("x"));
            var (companyNode, _, shop) = SeedHierarchy(company, extra);

            var userId = "excluded";
            CreateRole(extra, "reader", Permissions.EmployeeRead);
            var assignment = RoleAssignment.Create(userId, "reader", Reload(extra, companyNode), ScopeDepth.ThisAndEntireSubtree, ResourceReach.ContainerOnly, extra).Result;
            assignment.AddExclusion(Reload(extra, shop));
            extra.Add(assignment);
            extra.SaveChanges();

            var engine = new AuthorizationEngine(extra);
            engine.Can(userId, Permissions.EmployeeRead, companyNode).ShouldBeTrue();
            engine.Can(userId, Permissions.EmployeeRead, shop).ShouldBeFalse();
        }

        [Fact]
        public void ExpiredJitAssignmentIsIgnored()
        {
            using var extra = new ExtraAuthorizeDbContext(_extraOptions, null);
            using var company = new CompanyDbContext(_companyOptions, new FakeGetClaimsProvider("x"));
            var (companyNode, _, _) = SeedHierarchy(company, extra);

            CreateRole(extra, "reader", Permissions.EmployeeRead);
            extra.Add(RoleAssignment.Create("temp", "reader", Reload(extra, companyNode), ScopeDepth.ThisOnly, ResourceReach.ContainerOnly, extra,
                validFrom: DateTime.UtcNow.AddDays(-2), validTo: DateTime.UtcNow.AddDays(-1)).Result);
            extra.SaveChanges();

            var engine = new AuthorizationEngine(extra);
            engine.Can("temp", Permissions.EmployeeRead, Reload(extra, companyNode)).ShouldBeFalse();
        }

        [Fact]
        public void AbacTimeWindowCanReject()
        {
            using var extra = new ExtraAuthorizeDbContext(_extraOptions, null);
            using var company = new CompanyDbContext(_companyOptions, new FakeGetClaimsProvider("x"));
            var (companyNode, _, _) = SeedHierarchy(company, extra);

            CreateRole(extra, "reader", Permissions.EmployeeRead);
            extra.Add(RoleAssignment.Create("timed", "reader", Reload(extra, companyNode), ScopeDepth.ThisOnly, ResourceReach.ContainerOnly, extra,
                conditionsJson: "{\"fromTimeUtc\":\"00:00\",\"toTimeUtc\":\"00:01\"}").Result);
            extra.SaveChanges();

            var now = DateTime.UtcNow.TimeOfDay;
            if (now >= TimeSpan.Zero && now <= TimeSpan.FromMinutes(1))
                return; // skip the rare one-minute window

            var engine = new AuthorizationEngine(extra);
            engine.Can("timed", Permissions.EmployeeRead, Reload(extra, companyNode)).ShouldBeFalse();
        }

        [Fact]
        public void RoleHierarchyIncludesChildPermissions()
        {
            using var extra = new ExtraAuthorizeDbContext(_extraOptions, null);
            using var company = new CompanyDbContext(_companyOptions, new FakeGetClaimsProvider("x"));
            var (companyNode, _, shop) = SeedHierarchy(company, extra);

            CreateRole(extra, "parent", Permissions.EmployeeRead);
            CreateRole(extra, "child", Permissions.StockRead);
            extra.Add(new RoleHierarchy(extra.Find<RoleToPermissions>("parent"), extra.Find<RoleToPermissions>("child")));
            extra.Add(RoleAssignment.Create("hier", "parent", Reload(extra, companyNode), ScopeDepth.ThisAndEntireSubtree, ResourceReach.OwnedData, extra).Result);
            extra.SaveChanges();

            var engine = new AuthorizationEngine(extra);
            engine.Can("hier", Permissions.StockRead, Reload(extra, shop)).ShouldBeTrue();
        }

        [Fact]
        public void SeparationOfDutiesRejectsOverlappingRoles()
        {
            using var extra = new ExtraAuthorizeDbContext(_extraOptions, null);
            using var company = new CompanyDbContext(_companyOptions, new FakeGetClaimsProvider("x"));
            var (companyNode, _, _) = SeedHierarchy(company, extra);

            CreateRole(extra, "StoreManager", Permissions.SalesSell);
            CreateRole(extra, "UserAdmin", Permissions.UserChange);
            extra.Add(new RoleConflict(extra.Find<RoleToPermissions>("StoreManager"), extra.Find<RoleToPermissions>("UserAdmin")));
            extra.SaveChanges();

            var engine = new AuthorizationEngine(extra);
            var service = new RoleAssignmentService(extra, engine);
            var first = service.CreateAssignment(new CreateAssignmentRequest
            {
                UserId = "sod",
                RoleName = "StoreManager",
                ScopeTenantId = companyNode.TenantItemId
            });
            first.IsValid.ShouldBeTrue(first.GetAllErrors());

            var second = service.CreateAssignment(new CreateAssignmentRequest
            {
                UserId = "sod",
                RoleName = "UserAdmin",
                ScopeTenantId = companyNode.TenantItemId
            });
            second.IsValid.ShouldBeFalse();
        }

        [Fact]
        public void DelegationCannotExceedGranterScope()
        {
            using var extra = new ExtraAuthorizeDbContext(_extraOptions, null);
            using var company = new CompanyDbContext(_companyOptions, new FakeGetClaimsProvider("x"));
            var (companyNode, westCoast, shop) = SeedHierarchy(company, extra);

            CreateRole(extra, "UserAdmin", Permissions.UserChange, Permissions.AssignmentDelegate);
            extra.Add(RoleAssignment.Create("granter", "UserAdmin", Reload(extra, westCoast), ScopeDepth.ThisAndEntireSubtree, ResourceReach.OwnedData, extra).Result);
            extra.SaveChanges();

            var engine = new AuthorizationEngine(extra);
            var service = new RoleAssignmentService(extra, engine);
            var tooWide = service.DelegateAssignment("granter", new CreateAssignmentRequest
            {
                UserId = "delegatee",
                RoleName = "UserAdmin",
                ScopeTenantId = companyNode.TenantItemId,
                ScopeDepth = ScopeDepth.ThisAndEntireSubtree
            });
            tooWide.IsValid.ShouldBeFalse();

            var ok = service.DelegateAssignment("granter", new CreateAssignmentRequest
            {
                UserId = "delegatee",
                RoleName = "UserAdmin",
                ScopeTenantId = shop.TenantItemId,
                ScopeDepth = ScopeDepth.ThisOnly
            });
            ok.IsValid.ShouldBeTrue(ok.GetAllErrors());
            ok.Result.IsDelegated.ShouldBeTrue();
        }

        [Fact]
        public void FineGrainedCompanyFilterUsesExactKeys()
        {
            using var extra = new ExtraAuthorizeDbContext(_extraOptions, null);
            using var company = new CompanyDbContext(_companyOptions, new FakeGetClaimsProvider("x"));
            var (companyNode, westCoast, shop) = SeedHierarchy(company, extra);

            using var filtered = new CompanyDbContext(_companyOptions,
                new FakeGetClaimsProvider(companyNode.DataKey,
                    allowedTenantDataKeys: new[] { companyNode.DataKey },
                    allowedOwnedDataKeys: Array.Empty<string>()));

            var tenants = filtered.Tenants.Select(x => x.Name).ToList();
            tenants.ShouldEqual(new List<string> { companyNode.Name });
            filtered.ShopStocks.Count().ShouldEqual(0);
        }

        [Fact]
        public async Task TestCalcPermissionsIncludesScopedAssignments()
        {
            using var extra = new ExtraAuthorizeDbContext(_extraOptions, null);
            using var company = new CompanyDbContext(_companyOptions, new FakeGetClaimsProvider("x"));
            var (companyNode, _, _) = SeedHierarchy(company, extra);
            Assign(extra, "scoped", Permissions.StockRead, Reload(extra, companyNode), ScopeDepth.ThisOnly, ResourceReach.ContainerOnly);

            var calc = new FeatureAuthorize.CalcAllowedPermissions(extra);
            var packed = await calc.CalcPermissionsForUserAsync("scoped");
            packed.UnpackPermissionsFromString().ShouldContain(Permissions.StockRead);
        }

        private static TenantBase Reload(ExtraAuthorizeDbContext extra, TenantBase tenant)
        {
            return extra.Tenants.Single(x => x.TenantItemId == tenant.TenantItemId);
        }

        private static (Company company, TenantBase westCoast, RetailOutlet shop) SeedHierarchy(
            CompanyDbContext companyContext, ExtraAuthorizeDbContext extra)
        {
            companyContext.AddCompanyAndChildrenInDatabase(
                "4U Inc.|West Coast|San Fran|SF Dress4U");
            var company = companyContext.Tenants.IgnoreQueryFilters().OfType<Company>().Single();
            var westCoast = companyContext.Tenants.IgnoreQueryFilters().Single(x => x.Name == "West Coast");
            var shop = companyContext.Tenants.IgnoreQueryFilters().OfType<RetailOutlet>().Single();
            return (company, westCoast, shop);
        }

        private static ShopStock AddStock(CompanyDbContext companyContext, RetailOutlet shop)
        {
            var stock = new ShopStock { Name = "dress", NumInStock = 4, RetailPrice = 10, Shop = shop };
            stock.SetShopLevelDataKey(shop.DataKey);
            companyContext.Add(stock);
            companyContext.SaveChanges();
            return stock;
        }

        private static void CreateRole(ExtraAuthorizeDbContext extra, string name, params Permissions[] permissions)
        {
            if (extra.Find<RoleToPermissions>(name) != null)
                return;
            var status = RoleToPermissions.CreateRoleWithPermissions(name, name, permissions.ToList(), extra);
            extra.Add(status.Result);
            extra.SaveChanges();
        }

        private static string Assign(ExtraAuthorizeDbContext extra, string userId, Permissions permission,
            TenantBase scope, ScopeDepth depth, ResourceReach reach)
        {
            var roleName = permission.ToString() + depth;
            CreateRole(extra, roleName, permission);
            extra.Add(RoleAssignment.Create(userId, roleName, scope, depth, reach, extra).Result);
            extra.SaveChanges();
            return userId;
        }
    }
}
