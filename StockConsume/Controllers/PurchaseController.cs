using System.Text;
using StockConsume.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Newtonsoft.Json;
using StockConsume.Helper;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;
using StockConsume.Services;

namespace StockConsume.Controllers
{
    [Authorize(Roles = "Admin")]
    public class PurchaseController : Controller
    {
        private readonly IApiService _apiService;
        private readonly ILogger<PurchaseController> _logger;

        public PurchaseController(IApiService apiService, ILogger<PurchaseController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        // List all purchases
        public async Task<IActionResult> PurchaseList()
        {
            try
            {
                var purchases = await _apiService.GetPurchasesAsync();
                return View(purchases);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching purchases");
                TempData["Error"] = "Failed to load purchases. Please try again.";
                return View(new List<PurchaseModel>());
            }
        }

        // Delete purchase by ID
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _apiService.DeletePurchaseAsync(id);
                if (success)
                {
                    TempData["Success"] = "Purchase deleted successfully!";
                }
                else
                {
                    TempData["Error"] = "Failed to delete purchase.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while deleting purchase with ID {id}");
                TempData["Error"] = "An error occurred while deleting the purchase.";
            }
            
            return RedirectToAction("PurchaseList");
        }

        // GET: Add/Edit Purchase
        public async Task<IActionResult> AddEdit(int? id)
        {
            PurchaseModel purchase;

            if (id == null)
            {
                purchase = new PurchaseModel();
            }
            else
            {
                purchase = await _apiService.GetPurchaseByIdAsync(id.Value);
                if (purchase == null)
                {
                    return NotFound();
                }
            }

            // Populate dropdowns
            await PopulateDropdowns(purchase);

            return View(purchase);
        }

        [HttpPost]
        public async Task<IActionResult> AddEdit(PurchaseModel purchase)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(purchase);
                return View(purchase);
            }

            try
            {
                bool success;
                if (purchase.PurchaseId > 0)
                {
                    // Update existing purchase
                    success = await _apiService.UpdatePurchaseAsync(purchase);
                    if (success)
                    {
                        TempData["Success"] = "Purchase updated successfully!";
                    }
                    else
                    {
                        TempData["Error"] = "Failed to update purchase.";
                    }
                }
                else
                {
                    // Create new purchase
                    success = await _apiService.CreatePurchaseAsync(purchase);
                    if (success)
                    {
                        TempData["Success"] = "Purchase created successfully!";
                    }
                    else
                    {
                        TempData["Error"] = "Failed to create purchase.";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while saving purchase");
                TempData["Error"] = "An error occurred while saving the purchase.";
            }

            return RedirectToAction("PurchaseList");
        }

        // Helper method to populate dropdowns
        private async Task PopulateDropdowns(PurchaseModel model)
        {
            try
            {
                // Get suppliers
                var suppliers = await _apiService.GetSuppliersAsync();
                model.SupplierList = suppliers.Select(s => new SelectListItem
                {
                    Value = s.SupplierId.ToString(),
                    Text = s.SupplierName ?? "Unknown"
                }).ToList();

                // Get users
                var users = await _apiService.GetUsersAsync();
                model.UserList = users.Select(u => new SelectListItem
                {
                    Value = u.UserId.ToString(),
                    Text = u.UserName ?? "Unknown"
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while populating dropdowns");
                // Set empty lists if dropdowns fail to load
                model.SupplierList = new List<SelectListItem>();
                model.UserList = new List<SelectListItem>();
            }
        }
    }
}
