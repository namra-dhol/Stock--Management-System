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
    public class SaleDetailController : Controller
    {
        private readonly HttpClient _client;

        public SaleDetailController(IHttpClientFactory httpClientFactory)
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

        // List all sale details
        public async Task<IActionResult> SaleDetailList()
        {
            try
            {
                SetBearerToken();
                var response = await _client.GetAsync("SaleDetail");
                
                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Failed to load sale details. Please try again.";
                    return View(new List<SaleDetailModel>());
                }
                
                var json = await response.Content.ReadAsStringAsync();
                var list = JsonConvert.DeserializeObject<List<SaleDetailModel>>(json);

                return View(list ?? new List<SaleDetailModel>());
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while loading sale details.";
                return View(new List<SaleDetailModel>());
            }
        }

        // Delete sale detail by ID
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                SetBearerToken();
                var response = await _client.DeleteAsync($"SaleDetail/{id}");
                
                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Sale detail deleted successfully!";
                }
                else
                {
                    TempData["Error"] = "Failed to delete sale detail.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while deleting the sale detail.";
            }
            
            return RedirectToAction("SaleDetailList");
        }

        // GET: Add/Edit Sale Detail
        public async Task<IActionResult> AddEdit(int? id)
        {
            SaleDetailModel saleDetail;

            if (id == null)
            {
                saleDetail = new SaleDetailModel();
            }
            else
            {
                try
                {
                    SetBearerToken();
                    var response = await _client.GetAsync($"SaleDetail/{id}");
                    if (!response.IsSuccessStatusCode)
                    {
                        TempData["Error"] = "Sale detail not found.";
                        return RedirectToAction("SaleDetailList");
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    saleDetail = JsonConvert.DeserializeObject<SaleDetailModel>(json);
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "An error occurred while loading the sale detail.";
                    return RedirectToAction("SaleDetailList");
                }
            }

            // Populate dropdowns
            await PopulateDropdowns(saleDetail);

            return View(saleDetail);
        }

        [HttpPost]
        public async Task<IActionResult> AddEdit(SaleDetailModel saleDetail)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(saleDetail);
                return View(saleDetail);
            }

            var content = new StringContent(JsonConvert.SerializeObject(saleDetail), Encoding.UTF8, "application/json");
            SetBearerToken();

            try
            {
                if (saleDetail.SaleDetailId > 0)
                {
                    // Update existing sale detail
                    var response = await _client.PutAsync($"SaleDetail/{saleDetail.SaleDetailId}", content);
                    if (response.IsSuccessStatusCode)
                    {
                        TempData["Success"] = "Sale detail updated successfully!";
                    }
                    else
                    {
                        TempData["Error"] = "Failed to update sale detail.";
                    }
                }
                else
                {
                    // Create new sale detail
                    var response = await _client.PostAsync("SaleDetail", content);
                    if (response.IsSuccessStatusCode)
                    {
                        TempData["Success"] = "Sale detail created successfully!";
                    }
                    else
                    {
                        TempData["Error"] = "Failed to create sale detail.";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while saving the sale detail.";
            }

            return RedirectToAction("SaleDetailList");
        }

        // Helper method to populate dropdowns
        private async Task PopulateDropdowns(SaleDetailModel model)
        {
            try
            {
                SetBearerToken();
                
                // Get sales
                var salesResponse = await _client.GetAsync("SaleDetail/dropdown/sales");
                if (salesResponse.IsSuccessStatusCode)
                {
                    var salesJson = await salesResponse.Content.ReadAsStringAsync();
                    var sales = JsonConvert.DeserializeObject<List<dynamic>>(salesJson);
                    ViewBag.SaleList = sales?.Select((Func<dynamic, SelectListItem>)(s => new SelectListItem
                    {
                        Value = s.SaleId.ToString(),
                        Text = $"Sale #{s.SaleId}"
                    })).ToList() ?? new List<SelectListItem>();
                }

                // Get products
                var productsResponse = await _client.GetAsync("SaleDetail/dropdown/products");
                if (productsResponse.IsSuccessStatusCode)
                {
                    var productsJson = await productsResponse.Content.ReadAsStringAsync();
                    var products = JsonConvert.DeserializeObject<List<dynamic>>(productsJson);
                    ViewBag.ProductList = products?.Select((Func<dynamic, SelectListItem>)(p => new SelectListItem
                    {
                        Value = p.ProductId.ToString(),
                        Text = p.ProductName.ToString()
                    })).ToList() ?? new List<SelectListItem>();
                }
            }
            catch (Exception ex)
            {
                // Set empty lists if dropdowns fail to load
                ViewBag.SaleList = new List<SelectListItem>();
                ViewBag.ProductList = new List<SelectListItem>();
            }
        }
    }
}
