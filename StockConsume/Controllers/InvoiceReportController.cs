using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using StockConsume.Models;
using StockConsume.Services;
using StockConsume.ViewModels;
using Microsoft.AspNetCore.Authorization;

namespace StockConsume.Controllers
{
    [Authorize]
    public class InvoiceReportController : Controller
    {
        private readonly IApiService _apiService;
        private readonly ILogger<InvoiceReportController> _logger;

        public InvoiceReportController(IApiService apiService, ILogger<InvoiceReportController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        // GET: Invoice Report
        public async Task<IActionResult> Index(InvoiceReportViewModel? model)
        {
            try
            {
                if (model == null)
                {
                    model = new InvoiceReportViewModel
                    {
                        FromDate = DateTime.Now.AddMonths(-1),
                        ToDate = DateTime.Now
                    };
                }

                // Get invoices based on filters
                var invoices = await _apiService.GetInvoicesAsync(
                    model.FromDate,
                    model.ToDate,
                    model.CustomerId);

                model.Invoices = invoices;

                // Populate customer dropdown
                await PopulateCustomerDropdown(model);

                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading invoice report");
                TempData["Error"] = "Failed to load invoice report. Please try again.";
                return View(new InvoiceReportViewModel());
            }
        }

        // POST: Filter Invoice Report
        [HttpPost]
        public async Task<IActionResult> Filter(InvoiceReportViewModel model)
        {
            try
            {
                // Get invoices based on filters
                var invoices = await _apiService.GetInvoicesAsync(
                    model.FromDate,
                    model.ToDate,
                    model.CustomerId);

                model.Invoices = invoices;

                // Populate customer dropdown
                await PopulateCustomerDropdown(model);

                return View("Index", model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while filtering invoice report");
                TempData["Error"] = "Failed to filter invoice report. Please try again.";
                return View("Index", model);
            }
        }

        // GET: Invoice Details
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var invoice = await _apiService.GetInvoiceByIdAsync(id);
                if (invoice == null)
                {
                    return NotFound();
                }

                return View(invoice);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while loading invoice details for ID {id}");
                TempData["Error"] = "Failed to load invoice details.";
                return RedirectToAction("Index");
            }
        }

        // GET: Create Invoice
        public async Task<IActionResult> Create()
        {
            try
            {
                var model = new InvoiceModel();
                await PopulateInvoiceDropdowns(model);
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while loading create invoice form");
                TempData["Error"] = "Failed to load create invoice form.";
                return RedirectToAction("Index");
            }
        }

        // POST: Create Invoice
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(InvoiceModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    await PopulateInvoiceDropdowns(model);
                    return View(model);
                }

                // Set default values
                model.InvoiceDate = model.InvoiceDate ?? DateTime.Now;
                model.Status = model.Status ?? "Pending";

                // Create invoice via API
                var success = await CreateInvoiceAsync(model);
                if (success)
                {
                    TempData["Success"] = "Invoice created successfully!";
                    return RedirectToAction("Index");
                }
                else
                {
                    TempData["Error"] = "Failed to create invoice.";
                    await PopulateInvoiceDropdowns(model);
                    return View(model);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating invoice");
                TempData["Error"] = "An error occurred while creating the invoice.";
                await PopulateInvoiceDropdowns(model);
                return View(model);
            }
        }

        // Helper method to populate customer dropdown
        private async Task PopulateCustomerDropdown(InvoiceReportViewModel model)
        {
            try
            {
                // For now, we'll create a simple list. In a real application, you'd get this from an API
                model.CustomerList = new List<SelectListItem>
                {
                    new SelectListItem { Value = "", Text = "All Customers" },
                    new SelectListItem { Value = "1", Text = "Customer 1" },
                    new SelectListItem { Value = "2", Text = "Customer 2" },
                    new SelectListItem { Value = "3", Text = "Customer 3" }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while populating customer dropdown");
                model.CustomerList = new List<SelectListItem>();
            }
        }

        // Helper method to populate invoice form dropdowns
        private async Task PopulateInvoiceDropdowns(InvoiceModel model)
        {
            try
            {
                // Status dropdown
                model.StatusList = new List<SelectListItem>
                {
                    new SelectListItem { Value = "Pending", Text = "Pending" },
                    new SelectListItem { Value = "Paid", Text = "Paid" },
                    new SelectListItem { Value = "Overdue", Text = "Overdue" }
                };

                // Customer dropdown
                model.CustomerList = new List<SelectListItem>
                {
                    new SelectListItem { Value = "", Text = "Select Customer" },
                    new SelectListItem { Value = "1", Text = "Customer 1" },
                    new SelectListItem { Value = "2", Text = "Customer 2" },
                    new SelectListItem { Value = "3", Text = "Customer 3" }
                };

                // Sale dropdown (if needed)
                model.SaleList = new List<SelectListItem>
                {
                    new SelectListItem { Value = "", Text = "Select Sale (Optional)" }
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while populating invoice dropdowns");
                model.StatusList = new List<SelectListItem>();
                model.CustomerList = new List<SelectListItem>();
                model.SaleList = new List<SelectListItem>();
            }
        }

        // Helper method to create invoice via API
        private async Task<bool> CreateInvoiceAsync(InvoiceModel model)
        {
            try
            {
                // This would typically call an API endpoint to create the invoice
                // For now, we'll simulate success
                await Task.Delay(100); // Simulate API call
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating invoice via API");
                return false;
            }
        }
    }
}