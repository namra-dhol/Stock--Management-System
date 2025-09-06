using System.Text;
using StockConsume.Models;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using StockConsume.Helper;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Authorization;
using StockConsume.Services;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace StockConsume.Controllers
{
    [Authorize(Roles = "Admin")]
    public class SupplierController : Controller
    {
        private readonly IApiService _apiService;
        private readonly ILogger<SupplierController> _logger;

        public SupplierController(IApiService apiService, ILogger<SupplierController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        // List all suppliers
        public async Task<IActionResult> SupplierList()
        {
            try
            {
                var suppliers = await _apiService.GetAllSuppliersAsync();
                return View(suppliers);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching suppliers");
                TempData["Error"] = "Failed to load suppliers. Please try again.";
                return View(new List<SupplierModel>());
            }
        }

        // Delete supplier by ID
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _apiService.DeleteSupplierAsync(id);
                if (success)
                {
                    TempData["Success"] = "Supplier deleted successfully!";
                }
                else
                {
                    TempData["Error"] = "Failed to delete supplier.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while deleting supplier with ID {id}");
                TempData["Error"] = "An error occurred while deleting the supplier.";
            }
            
            return RedirectToAction("SupplierList");
        }

        // GET: Add/Edit Supplier
        public async Task<IActionResult> AddEdit(int? id)
        {
            SupplierModel supplier;

            if (id == null)
            {
                supplier = new SupplierModel();
            }
            else
            {
                supplier = await _apiService.GetSupplierByIdAsync(id.Value);
                if (supplier == null)
                {
                    return NotFound();
                }
            }

            // Populate dropdowns
            await PopulateDropdowns(supplier);

            return View(supplier);
        }

        [HttpPost]
        public async Task<IActionResult> AddEdit(SupplierModel supplier)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(supplier);
                return View(supplier);
            }

            try
            {
                bool success;
                if (supplier.SupplierId > 0)
                {
                    // Update existing supplier
                    success = await _apiService.UpdateSupplierAsync(supplier);
                    if (success)
                    {
                        TempData["Success"] = "Supplier updated successfully!";
                    }
                    else
                    {
                        TempData["Error"] = "Failed to update supplier.";
                    }
                }
                else
                {
                    // Create new supplier
                    success = await _apiService.CreateSupplierAsync(supplier);
                    if (success)
                    {
                        TempData["Success"] = "Supplier created successfully!";
                    }
                    else
                    {
                        TempData["Error"] = "Failed to create supplier.";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while saving supplier");
                TempData["Error"] = "An error occurred while saving the supplier.";
            }

            return RedirectToAction("SupplierList");
        }

        // Helper method to populate dropdowns
        private async Task PopulateDropdowns(SupplierModel model)
        {
            try
            {
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
                model.UserList = new List<SelectListItem>();
            }
        }
    }
}
