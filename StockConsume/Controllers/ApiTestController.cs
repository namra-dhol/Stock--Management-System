using Microsoft.AspNetCore.Mvc;
using StockConsume.Services;
using Microsoft.AspNetCore.Authorization;

namespace StockConsume.Controllers
{
    [Authorize]
    public class ApiTestController : Controller
    {
        private readonly IApiService _apiService;
        private readonly ILogger<ApiTestController> _logger;

        public ApiTestController(IApiService apiService, ILogger<ApiTestController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        public async Task<IActionResult> Index()
        {
            try
            {
                var testResults = new Dictionary<string, object>();

                // Test Products API
                try
                {
                    var products = await _apiService.GetProductsAsync();
                    testResults["Products"] = new { Success = true, Count = products.Count, Message = "Products loaded successfully" };
                }
                catch (Exception ex)
                {
                    testResults["Products"] = new { Success = false, Message = ex.Message };
                }

                // Test Categories API
                try
                {
                    var categories = await _apiService.GetCategoriesAsync();
                    testResults["Categories"] = new { Success = true, Count = categories.Count, Message = "Categories loaded successfully" };
                }
                catch (Exception ex)
                {
                    testResults["Categories"] = new { Success = false, Message = ex.Message };
                }

                // Test Suppliers API
                try
                {
                    var suppliers = await _apiService.GetSuppliersAsync();
                    testResults["Suppliers"] = new { Success = true, Count = suppliers.Count, Message = "Suppliers loaded successfully" };
                }
                catch (Exception ex)
                {
                    testResults["Suppliers"] = new { Success = false, Message = ex.Message };
                }

                // Test Dashboard API
                try
                {
                    var dashboard = await _apiService.GetDashboardSummaryAsync();
                    testResults["Dashboard"] = new { Success = true, Message = "Dashboard data loaded successfully" };
                }
                catch (Exception ex)
                {
                    testResults["Dashboard"] = new { Success = false, Message = ex.Message };
                }

                ViewBag.TestResults = testResults;
                return View();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred during API testing");
                TempData["Error"] = "An error occurred during API testing.";
                return View();
            }
        }
    }
}
