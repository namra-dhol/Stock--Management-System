using System.Text;
using StockConsume.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using StockConsume.Helper;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;

namespace StockConsume.Controllers
{
    [Authorize(Roles = "Admin")]
    public class QueryController : Controller
    {
        private readonly HttpClient _client;

        public QueryController(IHttpClientFactory httpClientFactory)
        {
            _client = httpClientFactory.CreateClient();
            _client.BaseAddress = new Uri("https://localhost:7066/api/");
        }

        private void SetBearerToken()
        {
            if (!string.IsNullOrWhiteSpace(TokenManager.Token))
            {
                _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenManager.Token);
            }
        }

        // Test API connection
        public async Task<IActionResult> Test()
        {
            try
            {
                SetBearerToken();
                var response = await _client.GetAsync("Query/test");
                
                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Failed to connect to Query API.";
                    return View();
                }
                
                var json = await response.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<dynamic>(json);
                
                ViewBag.TestResult = result;
                return View();
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while testing the Query API.";
                return View();
            }
        }

        // Send Query
        public async Task<IActionResult> SendQuery(int? productId)
        {
            var query = new QueryDTO();
            if (productId.HasValue)
            {
                query.ProductId = productId.Value;
            }

            // Populate dropdowns
            await PopulateDropdowns(query);

            return View(query);
        }

        [HttpPost]
        public async Task<IActionResult> SendQuery(QueryDTO query)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(query);
                return View(query);
            }

            var content = new StringContent(JsonConvert.SerializeObject(query), Encoding.UTF8, "application/json");
            SetBearerToken();

            try
            {
                var response = await _client.PostAsync("Query/send", content);
                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Query sent successfully!";
                    return RedirectToAction("Test");
                }
                else
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    TempData["Error"] = $"Failed to send query: {errorContent}";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while sending the query.";
            }

            await PopulateDropdowns(query);
            return View(query);
        }

        // Get Product by ID for query
        public async Task<IActionResult> GetProduct(int productId)
        {
            try
            {
                SetBearerToken();
                var response = await _client.GetAsync($"Query/product/{productId}");
                
                if (!response.IsSuccessStatusCode)
                {
                    return Json(new { success = false, message = "Product not found." });
                }
                
                var json = await response.Content.ReadAsStringAsync();
                var product = JsonConvert.DeserializeObject<ProductModel>(json);
                
                return Json(new { success = true, product = product });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "An error occurred while fetching product details." });
            }
        }

        // Helper method to populate dropdowns
        private async Task PopulateDropdowns(QueryDTO model)
        {
            try
            {
                SetBearerToken();
                
                // Get products
                var productsResponse = await _client.GetAsync("Product");
                if (productsResponse.IsSuccessStatusCode)
                {
                    var productsJson = await productsResponse.Content.ReadAsStringAsync();
                    var products = JsonConvert.DeserializeObject<List<ProductModel>>(productsJson);
                    ViewBag.ProductList = products?.Select((Func<ProductModel, SelectListItem>)(p => new SelectListItem
                    {
                        Value = p.ProductId.ToString(),
                        Text = p.ProductName,
                        Selected = p.ProductId == model.ProductId
                    })).ToList() ?? new List<SelectListItem>();
                }

                // Get categories
                var categoriesResponse = await _client.GetAsync("Category");
                if (categoriesResponse.IsSuccessStatusCode)
                {
                    var categoriesJson = await categoriesResponse.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<dynamic>(categoriesJson);
                    
                    if (apiResponse?.Categories != null)
                    {
                        var categories = JsonConvert.DeserializeObject<List<CategoryModel>>(apiResponse.Categories.ToString());
                        ViewBag.CategoryList = categories?.Select((Func<CategoryModel, SelectListItem>)(c => new SelectListItem
                        {
                            Value = c.CategoryId.ToString(),
                            Text = c.CategoryName,
                            Selected = c.CategoryId == model.CategoryId
                        })).ToList() ?? new List<SelectListItem>();
                    }
                }

                // Get suppliers
                var suppliersResponse = await _client.GetAsync("Supplier");
                if (suppliersResponse.IsSuccessStatusCode)
                {
                    var suppliersJson = await suppliersResponse.Content.ReadAsStringAsync();
                    var suppliers = JsonConvert.DeserializeObject<List<SupplierModel>>(suppliersJson);
                    ViewBag.SupplierList = suppliers?.Select((Func<SupplierModel, SelectListItem>)(s => new SelectListItem
                    {
                        Value = s.SupplierId.ToString(),
                        Text = s.SupplierName,
                        Selected = s.SupplierId == model.SupplierId
                    })).ToList() ?? new List<SelectListItem>();
                }
            }
            catch (Exception ex)
            {
                // Set empty lists if dropdowns fail to load
                ViewBag.ProductList = new List<SelectListItem>();
                ViewBag.CategoryList = new List<SelectListItem>();
                ViewBag.SupplierList = new List<SelectListItem>();
            }
        }
    }
}
