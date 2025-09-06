using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using StockApi.Models;
using StockApi.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace StockApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class DashboardController : ControllerBase
    {
        private readonly StockContext context;

        public DashboardController(StockContext context)
        {
            this.context = context;
        }

        #region Dashboard Summary
        // GET: /api/dashboard/summary?start=2024-01-01&end=2024-12-31
        [HttpGet("summary")]
        public async Task<ActionResult<DashboardViewModel>> GetSummary([FromQuery] DateTime? start, [FromQuery] DateTime? end)
        {
            DateTime startDate = start?.Date ?? DateTime.UtcNow.AddDays(-29).Date;
            DateTime endDate = (end?.Date ?? DateTime.UtcNow.Date);

            if (endDate < startDate)
            {
                return BadRequest("End date must be greater than or equal to start date.");
            }

            var vm = new DashboardViewModel();

            // Totals
            vm.TotalProducts = await context.Products.CountAsync();
            vm.TotalCategories = await context.Categories.CountAsync();
            vm.TotalSuppliers = await context.Suppliers.CountAsync();
            vm.TotalUsers = await context.Users.CountAsync();
            vm.TotalPurchases = await context.Purchases.CountAsync();
            vm.TotalSales = await context.Sales.CountAsync();

            // Financial totals
            vm.TotalPurchaseAmount = await context.Purchases.SumAsync(p => p.TotalAmount ?? 0);
            vm.TotalSaleAmount = await context.Sales.SumAsync(s => s.TotalAmount ?? 0);
            vm.NetProfit = vm.TotalSaleAmount - vm.TotalPurchaseAmount;

            // Products by category
            vm.ProductsByCategory = await context.Products
                .Include(p => p.Category)
                .GroupBy(p => p.Category != null ? p.Category.CategoryName : "Unknown")
                .Select(g => new { Key = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);

            // Products per day within range
            var dailyProducts = await context.Products
                .Where(p => p.CreatedAt >= startDate && p.CreatedAt <= endDate.AddDays(1).AddTicks(-1))
                .GroupBy(p => p.CreatedAt!.Value.Date)
                .Select(g => new { Day = g.Key, Count = g.Count() })
                .ToListAsync();

            // Sales per day within range
            var dailySales = await context.Sales
                .Where(s => s.SaleDate >= startDate && s.SaleDate <= endDate.AddDays(1).AddTicks(-1))
                .GroupBy(s => s.SaleDate!.Value.Date)
                .Select(g => new { Day = g.Key, Count = g.Count() })
                .ToListAsync();

            // Purchases per day within range
            var dailyPurchases = await context.Purchases
                .Where(p => p.PurchaseDate >= startDate && p.PurchaseDate <= endDate.AddDays(1).AddTicks(-1))
                .GroupBy(p => p.PurchaseDate!.Value.Date)
                .Select(g => new { Day = g.Key, Count = g.Count() })
                .ToListAsync();

            // Fill missing days with zero for chart completeness
            var cursor = startDate;
            while (cursor <= endDate)
            {
                var key = cursor.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                vm.ProductsPerDay[key] = 0;
                vm.SalesPerDay[key] = 0;
                vm.PurchasesPerDay[key] = 0;
                cursor = cursor.AddDays(1);
            }

            foreach (var d in dailyProducts)
            {
                var key = d.Day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                vm.ProductsPerDay[key] = d.Count;
            }

            foreach (var d in dailySales)
            {
                var key = d.Day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                vm.SalesPerDay[key] = d.Count;
            }

            foreach (var d in dailyPurchases)
            {
                var key = d.Day.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
                vm.PurchasesPerDay[key] = d.Count;
            }

            // Top products by stock quantity
            vm.TopProducts = await context.Products
                .Include(p => p.Category)
                .OrderByDescending(p => p.StockLevel)
                .Take(10)
                .Select(p => new TopProduct
                {
                    ProductId = p.ProductId,
                    ProductName = p.ProductName ?? "Unknown",
                    StockQuantity = p.StockLevel ?? 0,
                    Price = p.Price ?? 0,
                    CategoryName = p.Category != null ? p.Category.CategoryName : "Unknown"
                })
                .ToListAsync();

            // Top suppliers by purchase count and amount
            vm.TopSuppliers = await context.Purchases
                .Include(p => p.Supplier)
                .GroupBy(p => new { p.SupplierId, SupplierName = p.Supplier != null ? p.Supplier.SupplierName : "Unknown" })
                .Select(g => new TopSupplier
                {
                    SupplierId = g.Key.SupplierId,
                    SupplierName = g.Key.SupplierName,
                    PurchaseCount = g.Count(),
                    TotalAmount = g.Sum(p => p.TotalAmount ?? 0)
                })
                .OrderByDescending(s => s.TotalAmount)
                .Take(10)
                .ToListAsync();

            // Recent activities
            var recentPurchases = await context.Purchases
                .Include(p => p.Supplier)
                .OrderByDescending(p => p.PurchaseDate)
                .Take(5)
                .Select(p => new RecentActivity
                {
                    Type = "Purchase",
                    Description = $"Purchase from {p.Supplier != null ? p.Supplier.SupplierName : "Unknown Supplier"}",
                    Date = p.PurchaseDate ?? DateTime.MinValue,
                    Amount = p.TotalAmount
                })
                .ToListAsync();

            var recentSales = await context.Sales
                .OrderByDescending(s => s.SaleDate)
                .Take(5)
                .Select(s => new RecentActivity
                {
                    Type = "Sale",
                    Description = "New sale recorded",
                    Date = s.SaleDate ?? DateTime.MinValue,
                    Amount = s.TotalAmount
                })
                .ToListAsync();

            var recentProducts = await context.Products
                .Include(p => p.Category)
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .Select(p => new RecentActivity
                {
                    Type = "Product",
                    Description = $"New product added: {p.ProductName}",
                    Date = p.CreatedAt ?? DateTime.MinValue,
                    Amount = p.Price
                })
                .ToListAsync();

            vm.RecentActivities = recentPurchases
                .Concat(recentSales)
                .Concat(recentProducts)
                .OrderByDescending(a => a.Date)
                .Take(10)
                .ToList();

            return Ok(vm);
        }
        #endregion

        #region Quick Stats
        [HttpGet("quick-stats")]
        public async Task<ActionResult<object>> GetQuickStats()
        {
            var today = DateTime.Today;
            var thisMonth = new DateTime(today.Year, today.Month, 1);
            var lastMonth = thisMonth.AddMonths(-1);

            var stats = new
            {
                TodaySales = await context.Sales
                    .Where(s => s.SaleDate >= today && s.SaleDate < today.AddDays(1))
                    .SumAsync(s => s.TotalAmount ?? 0),
                
                TodayPurchases = await context.Purchases
                    .Where(p => p.PurchaseDate >= today && p.PurchaseDate < today.AddDays(1))
                    .SumAsync(p => p.TotalAmount ?? 0),
                
                ThisMonthSales = await context.Sales
                    .Where(s => s.SaleDate >= thisMonth && s.SaleDate < thisMonth.AddMonths(1))
                    .SumAsync(s => s.TotalAmount ?? 0),
                
                ThisMonthPurchases = await context.Purchases
                    .Where(p => p.PurchaseDate >= thisMonth && p.PurchaseDate < thisMonth.AddMonths(1))
                    .SumAsync(p => p.TotalAmount ?? 0),
                
                LowStockProducts = await context.Products
                    .Where(p => p.StockLevel < 10)
                    .CountAsync(),
                
                TotalCategories = await context.Categories.CountAsync(),
                TotalSuppliers = await context.Suppliers.CountAsync(),
                TotalUsers = await context.Users.CountAsync()
            };

            return Ok(stats);
        }
        #endregion
    }
}
