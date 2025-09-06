using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using StockConsume.Services;
using StockConsume.ViewModels;
using StockConsume.Models;

namespace StockConsume.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProductControllerNew : Controller
    {
        private readonly IApiService _apiService;
        private readonly ILogger<ProductControllerNew> _logger;

        public ProductControllerNew(IApiService apiService, ILogger<ProductControllerNew> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        // GET: Product List with filtering and pagination
        [HttpGet]
        public async Task<IActionResult> ProductList(ProductViewModel model)
        {
            try
            {
                // Get all products from API
                var products = await _apiService.GetProductsAsync();
                
                if (products == null || !products.Any())
                {
                    TempData["Warning"] = "No products found. Please check your API connection.";
                    await PopulateDropdownsAsync(model);
                    return View(model);
                }

                // Apply filters
                var filteredProducts = ApplyFilters(products, model);

                // Apply sorting
                filteredProducts = ApplySorting(filteredProducts, model.SortBy, model.SortOrder);

                // Set pagination info
                model.TotalItems = filteredProducts.Count;
                
                // Apply pagination
                var startIndex = (model.CurrentPage - 1) * model.PageSize;
                model.Products = filteredProducts.Skip(startIndex).Take(model.PageSize).ToList();

                // Populate dropdowns for filters
                await PopulateDropdownsAsync(model);

                _logger.LogInformation($"Retrieved {model.Products.Count} products for page {model.CurrentPage}");
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching products");
                TempData["Error"] = "An error occurred while loading products. Please try again.";
                
                await PopulateDropdownsAsync(model);
                return View(model);
            }
        }

        // GET: Add/Edit Product
        [HttpGet]
        public async Task<IActionResult> AddEdit(int? id)
        {
            var viewModel = new ProductEditViewModel();

            try
            {
                if (id.HasValue)
                {
                    var product = await _apiService.GetProductByIdAsync(id.Value);
                    if (product == null)
                    {
                        TempData["Error"] = "Product not found.";
                        return RedirectToAction(nameof(ProductList));
                    }

                    viewModel.Product = product;
                    viewModel.ProductName = product.ProductName ?? "";
                    viewModel.CategoryId = product.CategoryId;
                    viewModel.SupplierId = product.SupplierId;
                    viewModel.Unit = product.Unit;
                    viewModel.CostPrice = product.CostPrice;
                    viewModel.SellingPrice = product.SellingPrice;
                    viewModel.StockLevel = product.StockLevel;
                }

                await PopulateEditDropdownsAsync(viewModel);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while loading product for edit. ID: {id}");
                TempData["Error"] = "An error occurred while loading the product. Please try again.";
                return RedirectToAction(nameof(ProductList));
            }
        }

        // POST: Add/Edit Product
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddEdit(ProductEditViewModel viewModel)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    await PopulateEditDropdownsAsync(viewModel);
                    return View(viewModel);
                }

                // Map ViewModel to Model
                var product = viewModel.Product;
                product.ProductName = viewModel.ProductName;
                product.CategoryId = viewModel.CategoryId;
                product.SupplierId = viewModel.SupplierId;
                product.Unit = viewModel.Unit;
                product.CostPrice = viewModel.CostPrice;
                product.SellingPrice = viewModel.SellingPrice;
                product.StockLevel = viewModel.StockLevel;

                bool success;
                if (product.ProductId > 0)
                {
                    success = await _apiService.UpdateProductAsync(product);
                    if (success)
                    {
                        TempData["Success"] = "Product updated successfully!";
                        _logger.LogInformation($"Product updated: {product.ProductName} (ID: {product.ProductId})");
                    }
                    else
                    {
                        TempData["Error"] = "Failed to update product. Please try again.";
                    }
                }
                else
                {
                    success = await _apiService.CreateProductAsync(product);
                    if (success)
                    {
                        TempData["Success"] = "Product created successfully!";
                        _logger.LogInformation($"Product created: {product.ProductName}");
                    }
                    else
                    {
                        TempData["Error"] = "Failed to create product. Please try again.";
                    }
                }

                if (success)
                {
                    return RedirectToAction(nameof(ProductList));
                }

                await PopulateEditDropdownsAsync(viewModel);
                return View(viewModel);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while saving product");
                TempData["Error"] = "An error occurred while saving the product. Please try again.";
                
                await PopulateEditDropdownsAsync(viewModel);
                return View(viewModel);
            }
        }

        // POST: Delete Product
        [HttpPost]
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _apiService.DeleteProductAsync(id);
                if (success)
                {
                    TempData["Success"] = "Product deleted successfully!";
                    _logger.LogInformation($"Product deleted: ID {id}");
                }
                else
                {
                    TempData["Error"] = "Failed to delete product. Please try again.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while deleting product. ID: {id}");
                TempData["Error"] = "An error occurred while deleting the product. Please try again.";
            }

            return RedirectToAction(nameof(ProductList));
        }

        // Helper Methods
        private List<ProductModel> ApplyFilters(List<ProductModel> products, ProductViewModel model)
        {
            var filtered = products.AsQueryable();

            if (model.CategoryFilter.HasValue)
                filtered = filtered.Where(p => p.CategoryId == model.CategoryFilter);

            if (model.SupplierFilter.HasValue)
                filtered = filtered.Where(p => p.SupplierId == model.SupplierFilter);

            if (model.MinPrice.HasValue)
                filtered = filtered.Where(p => p.SellingPrice >= model.MinPrice);

            if (model.MaxPrice.HasValue)
                filtered = filtered.Where(p => p.SellingPrice <= model.MaxPrice);

            if (!string.IsNullOrEmpty(model.SearchTerm))
                filtered = filtered.Where(p => p.ProductName != null && 
                    p.ProductName.Contains(model.SearchTerm, StringComparison.OrdinalIgnoreCase));

            return filtered.ToList();
        }

        private List<ProductModel> ApplySorting(List<ProductModel> products, string sortBy, string sortOrder)
        {
            return sortBy?.ToLower() switch
            {
                "productname" => sortOrder == "desc" 
                    ? products.OrderByDescending(p => p.ProductName).ToList()
                    : products.OrderBy(p => p.ProductName).ToList(),
                "costprice" => sortOrder == "desc"
                    ? products.OrderByDescending(p => p.CostPrice).ToList()
                    : products.OrderBy(p => p.CostPrice).ToList(),
                "sellingprice" => sortOrder == "desc"
                    ? products.OrderByDescending(p => p.SellingPrice).ToList()
                    : products.OrderBy(p => p.SellingPrice).ToList(),
                "stocklevel" => sortOrder == "desc"
                    ? products.OrderByDescending(p => p.StockLevel).ToList()
                    : products.OrderBy(p => p.StockLevel).ToList(),
                "category" => sortOrder == "desc"
                    ? products.OrderByDescending(p => p.Category?.CategoryName).ToList()
                    : products.OrderBy(p => p.Category?.CategoryName).ToList(),
                _ => products.OrderBy(p => p.ProductName).ToList()
            };
        }

        private async Task PopulateDropdownsAsync(ProductViewModel model)
        {
            try
            {
                var categories = await _apiService.GetCategoriesAsync();
                model.Categories = categories.Select(c => new SelectListItem
                {
                    Value = c.CategoryId.ToString(),
                    Text = c.CategoryName ?? "Unknown Category",
                    Selected = c.CategoryId == model.CategoryFilter
                }).ToList();
                model.Categories.Insert(0, new SelectListItem { Value = "", Text = "-- All Categories --" });

                var suppliers = await _apiService.GetSuppliersAsync();
                model.Suppliers = suppliers.Select(s => new SelectListItem
                {
                    Value = s.SupplierId.ToString(),
                    Text = s.SupplierName ?? "Unknown Supplier",
                    Selected = s.SupplierId == model.SupplierFilter
                }).ToList();
                model.Suppliers.Insert(0, new SelectListItem { Value = "", Text = "-- All Suppliers --" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while populating dropdowns");
                model.Categories = new List<SelectListItem> 
                { 
                    new SelectListItem { Value = "", Text = "-- Categories unavailable --" } 
                };
                model.Suppliers = new List<SelectListItem> 
                { 
                    new SelectListItem { Value = "", Text = "-- Suppliers unavailable --" } 
                };
            }
        }

        private async Task PopulateEditDropdownsAsync(ProductEditViewModel model)
        {
            try
            {
                var categories = await _apiService.GetCategoriesAsync();
                model.Categories = categories.Select(c => new SelectListItem
                {
                    Value = c.CategoryId.ToString(),
                    Text = c.CategoryName ?? "Unknown Category",
                    Selected = c.CategoryId == model.CategoryId
                }).ToList();
                model.Categories.Insert(0, new SelectListItem { Value = "", Text = "-- Select Category --" });

                var suppliers = await _apiService.GetSuppliersAsync();
                model.Suppliers = suppliers.Select(s => new SelectListItem
                {
                    Value = s.SupplierId.ToString(),
                    Text = s.SupplierName ?? "Unknown Supplier",
                    Selected = s.SupplierId == model.SupplierId
                }).ToList();
                model.Suppliers.Insert(0, new SelectListItem { Value = "", Text = "-- Select Supplier --" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while populating edit dropdowns");
                model.Categories = new List<SelectListItem> 
                { 
                    new SelectListItem { Value = "", Text = "-- Categories unavailable --" } 
                };
                model.Suppliers = new List<SelectListItem> 
                { 
                    new SelectListItem { Value = "", Text = "-- Suppliers unavailable --" } 
                };
            }
        }
    }
}
