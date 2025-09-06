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
    public class SaleController : Controller
    {
        private readonly IApiService _apiService;
        private readonly ILogger<SaleController> _logger;

        public SaleController(IApiService apiService, ILogger<SaleController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        // List all sales
        public async Task<IActionResult> SaleList()
        {
            try
            {
                var sales = await _apiService.GetSalesAsync();
                return View(sales);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching sales");
                TempData["Error"] = "Failed to load sales. Please try again.";
                return View(new List<SaleModel>());
            }
        }

        // Delete sale by ID
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _apiService.DeleteSaleAsync(id);
                if (success)
                {
                    TempData["Success"] = "Sale deleted successfully!";
                }
                else
                {
                    TempData["Error"] = "Failed to delete sale.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while deleting sale with ID {id}");
                TempData["Error"] = "An error occurred while deleting the sale.";
            }
            
            return RedirectToAction("SaleList");
        }

        // GET: Add/Edit Sale
        public async Task<IActionResult> AddEdit(int? id)
        {
            SaleModel sale;

            if (id == null)
            {
                sale = new SaleModel();
            }
            else
            {
                sale = await _apiService.GetSaleByIdAsync(id.Value);
                if (sale == null)
                {
                    return NotFound();
                }
            }

            // Populate dropdowns
            await PopulateDropdowns(sale);

            return View(sale);
        }

        [HttpPost]
        public async Task<IActionResult> AddEdit(SaleModel sale)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(sale);
                return View(sale);
            }

            try
            {
                bool success;
                if (sale.SaleId > 0)
                {
                    // Update existing sale
                    success = await _apiService.UpdateSaleAsync(sale);
                    if (success)
                    {
                        TempData["Success"] = "Sale updated successfully!";
                    }
                    else
                    {
                        TempData["Error"] = "Failed to update sale.";
                    }
                }
                else
                {
                    // Create new sale
                    success = await _apiService.CreateSaleAsync(sale);
                    if (success)
                    {
                        TempData["Success"] = "Sale created successfully!";
                    }
                    else
                    {
                        TempData["Error"] = "Failed to create sale.";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while saving sale");
                TempData["Error"] = "An error occurred while saving the sale.";
            }

            return RedirectToAction("SaleList");
        }

        // Helper method to populate dropdowns
        private async Task PopulateDropdowns(SaleModel model)
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
