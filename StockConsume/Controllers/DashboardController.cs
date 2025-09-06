using System.Text;
using StockConsume.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using StockConsume.Helper;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;
using StockConsume.Services;

namespace StockConsume.Controllers
{
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly IApiService _apiService;
        private readonly ILogger<DashboardController> _logger;

        public DashboardController(IApiService apiService, ILogger<DashboardController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        // Dashboard summary
        public async Task<IActionResult> Index(DateTime? start, DateTime? end)
        {
            try
            {
                var dashboard = await _apiService.GetDashboardSummaryAsync(start, end);
                return View(dashboard);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading dashboard data");
                TempData["Error"] = "An error occurred while loading dashboard data.";
                return View(new DashboardViewModel());
            }
        }

        // Quick Stats
        public async Task<IActionResult> QuickStats()
        {
            try
            {
                // For now, return basic stats. In a real application, you'd have a specific API endpoint for this
                var dashboard = await _apiService.GetDashboardSummaryAsync();
                return Json(new { success = true, data = dashboard });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading quick stats");
                return Json(new { success = false, message = "An error occurred while loading quick stats." });
            }
        }
    }
}
