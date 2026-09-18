using System;
using System.Collections.Generic;
using System.Linq;
using DataKeyParts;
using DataLayer.EfCode;
using FeatureAuthorize.PolicyCode;
using GenericServices;
using GenericServices.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using PermissionParts;
using ScopeAuthorize;
using ServiceLayer.Shop;

namespace PermissionAccessControl2.Controllers
{
    public class ShopController : Controller
    {
        [HttpGet]
        [HasPermission(Permissions.SalesSell)]
        public IActionResult Till([FromServices] ICrudServices<CompanyDbContext> service,
            [FromServices] IAuthorizationEngine engine, [FromServices] IGetClaimsProvider claims)
        {
            var dto = new SellItemDto();
            dto.SetResetDto(FilterOwnedData(service.ReadManyNoTracked<StockSelectDto>(), claims.UserId, Permissions.SalesSell, engine, x => x.DataKey).ToList());
            return View(dto);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [HasPermission(Permissions.SalesSell)]
        public IActionResult Till([FromServices] ICrudServices<CompanyDbContext> service, SellItemDto dto,
            [FromServices] IAuthorizationEngine engine, [FromServices] IGetClaimsProvider claims)
        {
            if (!ModelState.IsValid)
            {
                dto.SetResetDto(FilterOwnedData(service.ReadManyNoTracked<StockSelectDto>(), claims.UserId, Permissions.SalesSell, engine, x => x.DataKey).ToList());
                return View(dto);
            }

            var result = service.CreateAndSave(dto);
            if (service.IsValid)
                return RedirectToAction("BuySuccess", new { message = service.Message, result.ShopSaleId });

            service.CopyErrorsToModelState(ModelState, dto);
            dto.SetResetDto(FilterOwnedData(service.ReadManyNoTracked<StockSelectDto>(), claims.UserId, Permissions.SalesSell, engine, x => x.DataKey).ToList());
            return View(dto);
        }

        public IActionResult BuySuccess([FromServices] ICrudServices<CompanyDbContext> service, string message, int shopSaleId)
        {
            var saleInfo = service.ReadSingle<ListSalesDto>(shopSaleId);
            return View(new Tuple<ListSalesDto, string>(saleInfo, message));
        }

        [HasPermission(Permissions.StockRead)]
        public IActionResult Stock([FromServices] ICrudServices<CompanyDbContext> service,
            [FromServices] IAuthorizationEngine engine, [FromServices] IGetClaimsProvider claims)
        {
            var allStock = FilterOwnedData(service.ReadManyNoTracked<ListStockDto>(), claims.UserId, Permissions.StockRead, engine, x => x.DataKey).ToList();
            var allTheSameShop = allStock.Any() && allStock.All(x => x.ShopName == allStock.First().ShopName);
            return View(new Tuple<List<ListStockDto>, bool>(allStock, allTheSameShop));
        }

        [HasPermission(Permissions.SalesRead)]
        public IActionResult Sales([FromServices] ICrudServices<CompanyDbContext> service,
            [FromServices] IAuthorizationEngine engine, [FromServices] IGetClaimsProvider claims)
        {
            var allSales = FilterOwnedData(service.ReadManyNoTracked<ListSalesDto>(), claims.UserId, Permissions.SalesRead, engine, x => x.DataKey).ToList();
            var allTheSameShop = allSales.Any() && allSales.All(x => x.StockItemShopName == allSales.First().StockItemShopName);
            return View(new Tuple<List<ListSalesDto>, bool>(allSales, allTheSameShop));
        }

        private static IReadOnlyList<T> FilterOwnedData<T>(IEnumerable<T> rows, string userId, Permissions permission,
            IAuthorizationEngine engine, Func<T, string> dataKeySelector)
        {
            var list = rows.ToList();
            if (string.IsNullOrEmpty(userId) || engine.IsSuperAdmin(userId))
                return list;
            var keys = engine.GetAllowedOwnedDataKeys(userId, permission);
            return list.Where(x => keys.Contains(dataKeySelector(x))).ToList();
        }
    }
}
