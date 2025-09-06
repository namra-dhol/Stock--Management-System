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
    public class PurchaseDetailController : Controller
    {
        private readonly HttpClient _client;

        public PurchaseDetailController(IHttpClientFactory httpClientFactory)
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

        // List all purchase details
        public async Task<IActionResult> PurchaseDetailList()
        {
            try
            {
                SetBearerToken();
                var response = await _client.GetAsync("PurchaseDetail");
                
                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Failed to load purchase details. Please try again.";
                    return View(new List<PurchaseDetailModel>());
                }
                
                var json = await response.Content.ReadAsStringAsync();
                var list = JsonConvert.DeserializeObject<List<PurchaseDetailModel>>(json);

                return View(list ?? new List<PurchaseDetailModel>());
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while loading purchase details.";
                return View(new List<PurchaseDetailModel>());
            }
        }

        // Delete purchase detail by ID
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                SetBearerToken();
                var response = await _client.DeleteAsync($"PurchaseDetail/{id}");
                
                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Purchase detail deleted successfully!";
                }
                else
                {
                    TempData["Error"] = "Failed to delete purchase detail.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while deleting the purchase detail.";
            }
            
            return RedirectToAction("PurchaseDetailList");
        }

        // GET: Add/Edit Purchase Detail
        public async Task<IActionResult> AddEdit(int? id)
        {
            PurchaseDetailModel purchaseDetail;

            if (id == null)
            {
                purchaseDetail = new PurchaseDetailModel();
            }
            else
            {
                try
                {
                    SetBearerToken();
                    var response = await _client.GetAsync($"PurchaseDetail/{id}");
                    if (!response.IsSuccessStatusCode)
                    {
                        TempData["Error"] = "Purchase detail not found.";
                        return RedirectToAction("PurchaseDetailList");
                    }

                    var json = await response.Content.ReadAsStringAsync();
                    purchaseDetail = JsonConvert.DeserializeObject<PurchaseDetailModel>(json);
                }
                catch (Exception ex)
                {
                    TempData["Error"] = "An error occurred while loading the purchase detail.";
                    return RedirectToAction("PurchaseDetailList");
                }
            }

            // Populate dropdowns
            await PopulateDropdowns(purchaseDetail);

            return View(purchaseDetail);
        }

        [HttpPost]
        public async Task<IActionResult> AddEdit(PurchaseDetailModel purchaseDetail)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(purchaseDetail);
                return View(purchaseDetail);
            }

            var content = new StringContent(JsonConvert.SerializeObject(purchaseDetail), Encoding.UTF8, "application/json");
            SetBearerToken();

            try
            {
                if (purchaseDetail.PurchaseDetailId > 0)
                {
                    // Update existing purchase detail
                    var response = await _client.PutAsync($"PurchaseDetail/{purchaseDetail.PurchaseDetailId}", content);
                    if (response.IsSuccessStatusCode)
                    {
                        TempData["Success"] = "Purchase detail updated successfully!";
                    }
                    else
                    {
                        TempData["Error"] = "Failed to update purchase detail.";
                    }
                }
                else
                {
                    // Create new purchase detail
                    var response = await _client.PostAsync("PurchaseDetail", content);
                    if (response.IsSuccessStatusCode)
                    {
                        TempData["Success"] = "Purchase detail created successfully!";
                    }
                    else
                    {
                        TempData["Error"] = "Failed to create purchase detail.";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while saving the purchase detail.";
            }

            return RedirectToAction("PurchaseDetailList");
        }

        // Helper method to populate dropdowns
        private async Task PopulateDropdowns(PurchaseDetailModel model)
        {
            try
            {
                SetBearerToken();
                
                // Get purchases
                var purchasesResponse = await _client.GetAsync("PurchaseDetail/dropdown/purchases");
                if (purchasesResponse.IsSuccessStatusCode)
                {
                    var purchasesJson = await purchasesResponse.Content.ReadAsStringAsync();
                    var purchases = JsonConvert.DeserializeObject<List<dynamic>>(purchasesJson);
                    ViewBag.PurchaseList = purchases?.Select((Func<dynamic, SelectListItem>)(p => new SelectListItem
                    {
                        Value = p.PurchaseId.ToString(),
                        Text = $"Purchase #{p.PurchaseId}"
                    })).ToList() ?? new List<SelectListItem>();
                }

                // Get products
                var productsResponse = await _client.GetAsync("PurchaseDetail/dropdown/products");
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
                ViewBag.PurchaseList = new List<SelectListItem>();
                ViewBag.ProductList = new List<SelectListItem>();
            }
        }
    }
}
