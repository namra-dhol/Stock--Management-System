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
    public class InvoiceController : Controller
    {
        private readonly IApiService _apiService;
        private readonly ILogger<InvoiceController> _logger;

        public InvoiceController(IApiService apiService, ILogger<InvoiceController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        // List all invoices
        public async Task<IActionResult> InvoiceList()
        {
            try
            {
                var invoices = await _apiService.GetInvoicesAsync();
                return View(invoices);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching invoices");
                TempData["Error"] = "Failed to load invoices. Please try again.";
                return View(new List<InvoiceModel>());
            }
        }

        // Generate invoice from sale
        public async Task<IActionResult> GenerateFromSale(int saleId)
        {
            try
            {
                // For now, we'll use a placeholder implementation since the API service doesn't have this method yet
                // In a real scenario, you'd add this method to the IApiService interface
                TempData["Info"] = "Invoice generation feature will be implemented soon.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while generating invoice for sale {saleId}");
                TempData["Error"] = "An error occurred while generating invoice.";
            }
            
            return RedirectToAction("InvoiceList");
        }

        // GET: Add/Edit Invoice
        public async Task<IActionResult> AddEdit(int? id)
        {
            InvoiceModel invoice;

            if (id == null)
            {
                invoice = new InvoiceModel
                {
                    InvoiceDate = DateTime.Now,
                    DueDate = DateTime.Now.AddDays(30),
                    Status = "Pending"
                };
            }
            else
            {
                invoice = await _apiService.GetInvoiceByIdAsync(id.Value);
                if (invoice == null)
                {
                    TempData["Error"] = "Invoice not found.";
                    return RedirectToAction("InvoiceList");
                }
            }

            // Populate dropdowns
            await PopulateDropdowns(invoice);

            return View(invoice);
        }

        [HttpPost]
        public async Task<IActionResult> AddEdit(InvoiceModel invoice)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(invoice);
                return View(invoice);
            }

            // Calculate totals
            CalculateInvoiceTotals(invoice);

            try
            {
                if (invoice.InvoiceId > 0)
                {
                    // Update existing invoice
                    var result = await _apiService.UpdateInvoiceAsync(invoice.InvoiceId, invoice);
                    if (result)
                    {
                        TempData["Success"] = "Invoice updated successfully!";
                    }
                    else
                    {
                        TempData["Error"] = "Failed to update invoice.";
                    }
                }
                else
                {
                    // Create new invoice
                    var result = await _apiService.CreateInvoiceAsync(invoice);
                    if (result != null)
                    {
                        TempData["Success"] = "Invoice created successfully!";
                    }
                    else
                    {
                        TempData["Error"] = "Failed to create invoice.";
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while saving the invoice.";
            }

            return RedirectToAction("InvoiceList");
        }

        // Delete invoice
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                // For now, we'll use a placeholder since delete isn't in the API service yet
                TempData["Info"] = "Invoice deletion feature will be implemented soon.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while deleting invoice with ID {id}");
                TempData["Error"] = "An error occurred while deleting the invoice.";
            }
            
            return RedirectToAction("InvoiceList");
        }

        // Print invoice
        public async Task<IActionResult> Print(int id)
        {
            try
            {
                var invoice = await _apiService.GetInvoiceByIdAsync(id);
                
                if (invoice == null)
                {
                    TempData["Error"] = "Invoice not found.";
                    return RedirectToAction("InvoiceList");
                }

                return View(invoice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while loading invoice for printing with ID {id}");
                TempData["Error"] = "An error occurred while loading the invoice.";
                return RedirectToAction("InvoiceList");
            }
        }

        // Helper method to calculate invoice totals
        private void CalculateInvoiceTotals(InvoiceModel invoice)
        {
            if (invoice.SubTotal.HasValue)
            {
                // Calculate discount amount
                if (invoice.DiscountPercentage.HasValue && invoice.DiscountPercentage > 0)
                {
                    invoice.DiscountAmount = invoice.SubTotal * (invoice.DiscountPercentage / 100);
                }

                var afterDiscount = invoice.SubTotal - (invoice.DiscountAmount ?? 0);

                // Calculate tax amount
                if (invoice.TaxPercentage.HasValue && invoice.TaxPercentage > 0)
                {
                    invoice.TaxAmount = afterDiscount * (invoice.TaxPercentage / 100);
                }

                // Calculate total
                invoice.TotalAmount = afterDiscount + (invoice.TaxAmount ?? 0);
            }
        }

        // Helper method to populate dropdowns
        private async Task PopulateDropdowns(InvoiceModel model)
        {
            try
            {
                // Get sales
                var sales = await _apiService.GetSalesAsync();
                model.SaleList = sales.Select(s => new SelectListItem
                {
                    Value = s.SaleId.ToString(),
                    Text = $"Sale #{s.SaleId} - {s.SaleDate?.ToString("dd/MM/yyyy")}"
                }).ToList();

                // Status list
                model.StatusList = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Pending", Text = "Pending" },
                    new SelectListItem { Value = "Paid", Text = "Paid" },
                    new SelectListItem { Value = "Overdue", Text = "Overdue" },
                    new SelectListItem { Value = "Cancelled", Text = "Cancelled" }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while populating dropdowns");
                // Set empty lists if dropdowns fail to load
                model.SaleList = new List<SelectListItem>();
                model.StatusList = new List<SelectListItem>();
            }
        }
    }
}
