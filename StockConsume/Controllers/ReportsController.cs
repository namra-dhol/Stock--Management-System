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
    public class ReportsController : Controller
    {
        private readonly HttpClient _client;

        public ReportsController(IHttpClientFactory httpClientFactory)
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

        // Reports Dashboard
        public async Task<IActionResult> Index()
        {
            var filter = new ReportFilterModel
            {
                StartDate = DateTime.Now.AddMonths(-1),
                EndDate = DateTime.Now
            };

            await PopulateDropdowns(filter);
            return View(filter);
        }

        // Inventory Report
        public async Task<IActionResult> InventoryReport(ReportFilterModel filter)
        {
            try
            {
                SetBearerToken();
                var queryParams = BuildQueryParams(filter);
                var response = await _client.GetAsync($"Reports/inventory{queryParams}");
                
                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Failed to generate inventory report.";
                    return View(new List<InventoryReportModel>());
                }
                
                var json = await response.Content.ReadAsStringAsync();
                var report = JsonConvert.DeserializeObject<List<InventoryReportModel>>(json);

                ViewBag.Filter = filter;
                return View(report ?? new List<InventoryReportModel>());
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while generating inventory report.";
                return View(new List<InventoryReportModel>());
            }
        }

        // Sales Report
        public async Task<IActionResult> SalesReport(ReportFilterModel filter)
        {
            try
            {
                SetBearerToken();
                var queryParams = BuildQueryParams(filter);
                var response = await _client.GetAsync($"Reports/sales{queryParams}");
                
                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Failed to generate sales report.";
                    return View(new List<SalesReportModel>());
                }
                
                var json = await response.Content.ReadAsStringAsync();
                var report = JsonConvert.DeserializeObject<List<SalesReportModel>>(json);

                ViewBag.Filter = filter;
                return View(report ?? new List<SalesReportModel>());
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while generating sales report.";
                return View(new List<SalesReportModel>());
            }
        }

        // Purchase Report
        public async Task<IActionResult> PurchaseReport(ReportFilterModel filter)
        {
            try
            {
                SetBearerToken();
                var queryParams = BuildQueryParams(filter);
                var response = await _client.GetAsync($"Reports/purchase{queryParams}");
                
                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Failed to generate purchase report.";
                    return View(new List<PurchaseReportModel>());
                }
                
                var json = await response.Content.ReadAsStringAsync();
                var report = JsonConvert.DeserializeObject<List<PurchaseReportModel>>(json);

                ViewBag.Filter = filter;
                return View(report ?? new List<PurchaseReportModel>());
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while generating purchase report.";
                return View(new List<PurchaseReportModel>());
            }
        }

        // Profit & Loss Report
        public async Task<IActionResult> ProfitLossReport(ReportFilterModel filter)
        {
            try
            {
                SetBearerToken();
                var queryParams = BuildQueryParams(filter);
                var response = await _client.GetAsync($"Reports/profit-loss{queryParams}");
                
                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Failed to generate profit & loss report.";
                    return View(new ProfitLossReportModel());
                }
                
                var json = await response.Content.ReadAsStringAsync();
                var report = JsonConvert.DeserializeObject<ProfitLossReportModel>(json);

                ViewBag.Filter = filter;
                return View(report ?? new ProfitLossReportModel());
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while generating profit & loss report.";
                return View(new ProfitLossReportModel());
            }
        }

        // Export Report
        public async Task<IActionResult> ExportReport(string reportType, string format, ReportFilterModel filter)
        {
            try
            {
                SetBearerToken();
                var queryParams = BuildQueryParams(filter);
                queryParams += $"&format={format}";
                
                var response = await _client.GetAsync($"Reports/export/{reportType}{queryParams}");
                
                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Failed to export report.";
                    return RedirectToAction("Index");
                }

                var content = await response.Content.ReadAsByteArrayAsync();
                var contentType = GetContentType(format);
                var fileName = $"{reportType}_report_{DateTime.Now:yyyyMMdd}.{format.ToLower()}";

                return File(content, contentType, fileName);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while exporting report.";
                return RedirectToAction("Index");
            }
        }

        // Goods Receipt Management
        public async Task<IActionResult> GoodsReceiptList()
        {
            try
            {
                SetBearerToken();
                var response = await _client.GetAsync("GoodsReceipt");
                
                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Failed to load goods receipts.";
                    return View(new List<GoodsReceiptModel>());
                }
                
                var json = await response.Content.ReadAsStringAsync();
                var list = JsonConvert.DeserializeObject<List<GoodsReceiptModel>>(json);

                return View(list ?? new List<GoodsReceiptModel>());
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while loading goods receipts.";
                return View(new List<GoodsReceiptModel>());
            }
        }

        // Process Goods Receipt
        public async Task<IActionResult> ProcessGoodsReceipt(int purchaseId)
        {
            try
            {
                SetBearerToken();
                var response = await _client.GetAsync($"GoodsReceipt/from-purchase/{purchaseId}");
                
                if (!response.IsSuccessStatusCode)
                {
                    TempData["Error"] = "Failed to load purchase details for goods receipt.";
                    return RedirectToAction("GoodsReceiptList");
                }
                
                var json = await response.Content.ReadAsStringAsync();
                var receipt = JsonConvert.DeserializeObject<GoodsReceiptModel>(json);

                return View(receipt);
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while processing goods receipt.";
                return RedirectToAction("GoodsReceiptList");
            }
        }

        [HttpPost]
        public async Task<IActionResult> ProcessGoodsReceipt(GoodsReceiptModel receipt)
        {
            try
            {
                SetBearerToken();
                var content = new StringContent(JsonConvert.SerializeObject(receipt), Encoding.UTF8, "application/json");
                var response = await _client.PostAsync("GoodsReceipt/process", content);
                
                if (response.IsSuccessStatusCode)
                {
                    TempData["Success"] = "Goods receipt processed successfully! Stock levels updated.";
                }
                else
                {
                    TempData["Error"] = "Failed to process goods receipt.";
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "An error occurred while processing goods receipt.";
            }
            
            return RedirectToAction("GoodsReceiptList");
        }

        // Helper methods
        private string BuildQueryParams(ReportFilterModel filter)
        {
            var queryParams = new List<string>();
            
            if (filter.StartDate.HasValue)
                queryParams.Add($"startDate={filter.StartDate.Value:yyyy-MM-dd}");
            if (filter.EndDate.HasValue)
                queryParams.Add($"endDate={filter.EndDate.Value:yyyy-MM-dd}");
            if (filter.SupplierId.HasValue)
                queryParams.Add($"supplierId={filter.SupplierId}");
            if (filter.CategoryId.HasValue)
                queryParams.Add($"categoryId={filter.CategoryId}");
            if (filter.ProductId.HasValue)
                queryParams.Add($"productId={filter.ProductId}");
            
            return queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
        }

        private string GetContentType(string format)
        {
            return format.ToLower() switch
            {
                "csv" => "text/csv",
                "excel" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "pdf" => "application/pdf",
                _ => "application/octet-stream"
            };
        }

        private async Task PopulateDropdowns(ReportFilterModel model)
        {
            try
            {
                SetBearerToken();
                
                // Get suppliers
                var suppliersResponse = await _client.GetAsync("Product/dropdown/suppliers");
                if (suppliersResponse.IsSuccessStatusCode)
                {
                    var suppliersJson = await suppliersResponse.Content.ReadAsStringAsync();
                    var suppliers = JsonConvert.DeserializeObject<List<dynamic>>(suppliersJson);
                    model.SupplierList = suppliers?.Select((Func<dynamic, SelectListItem>)(s => new SelectListItem
                    {
                        Value = s.SupplierId.ToString(),
                        Text = s.SupplierName.ToString()
                    })).ToList() ?? new List<SelectListItem>();
                }

                // Get categories
                var categoriesResponse = await _client.GetAsync("Product/dropdown/categories");
                if (categoriesResponse.IsSuccessStatusCode)
                {
                    var categoriesJson = await categoriesResponse.Content.ReadAsStringAsync();
                    var categories = JsonConvert.DeserializeObject<List<dynamic>>(categoriesJson);
                    model.CategoryList = categories?.Select((Func<dynamic, SelectListItem>)(c => new SelectListItem
                    {
                        Value = c.CategoryId.ToString(),
                        Text = c.CategoryName.ToString()
                    })).ToList() ?? new List<SelectListItem>();
                }

                // Get products
                var productsResponse = await _client.GetAsync("Product");
                if (productsResponse.IsSuccessStatusCode)
                {
                    var productsJson = await productsResponse.Content.ReadAsStringAsync();
                    var products = JsonConvert.DeserializeObject<List<ProductModel>>(productsJson);
                    model.ProductList = products?.Select((Func<ProductModel, SelectListItem>)(p => new SelectListItem
                    {
                        Value = p.ProductId.ToString(),
                        Text = p.ProductName
                    })).ToList() ?? new List<SelectListItem>();
                }

                // Report types
                model.ReportTypeList = new List<SelectListItem>
                {
                    new SelectListItem { Value = "inventory", Text = "Inventory Report" },
                    new SelectListItem { Value = "sales", Text = "Sales Report" },
                    new SelectListItem { Value = "purchase", Text = "Purchase Report" },
                    new SelectListItem { Value = "profit-loss", Text = "Profit & Loss Report" }
                };

                // Export formats
                model.ExportFormatList = new List<SelectListItem>
                {
                    new SelectListItem { Value = "csv", Text = "CSV" },
                    new SelectListItem { Value = "excel", Text = "Excel" },
                    new SelectListItem { Value = "pdf", Text = "PDF" }
                };
            }
            catch (Exception ex)
            {
                // Set empty lists if dropdowns fail to load
                model.SupplierList = new List<SelectListItem>();
                model.CategoryList = new List<SelectListItem>();
                model.ProductList = new List<SelectListItem>();
                model.ReportTypeList = new List<SelectListItem>();
                model.ExportFormatList = new List<SelectListItem>();
            }
        }
    }
}
