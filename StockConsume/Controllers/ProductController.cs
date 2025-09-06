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
    public class ProductController : Controller
    {
        private readonly IApiService _apiService;
        private readonly ILogger<ProductController> _logger;

        public ProductController(IApiService apiService, ILogger<ProductController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        // List all products
        public async Task<IActionResult> ProductList()
        {
            try
            {
                var products = await _apiService.GetProductsAsync();
                return View(products);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching products");
                TempData["Error"] = "Failed to load products. Please try again.";
                return View(new List<ProductModel>());
            }
        }

        // Delete product by ID
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _apiService.DeleteProductAsync(id);
                if (success)
                {
                    TempData["Success"] = "Product deleted successfully!";
                }
                else
                {
                    TempData["Error"] = "Failed to delete product.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while deleting product with ID {id}");
                TempData["Error"] = "An error occurred while deleting the product.";
            }
            
            return RedirectToAction("ProductList");
        }

        // GET: Add/Edit Product
        public async Task<IActionResult> AddEdit(int? id)
        {
            ProductModel product;

            if (id == null)
            {
                product = new ProductModel();
            }
            else
            {
                product = await _apiService.GetProductByIdAsync(id.Value);
                if (product == null)
                {
                    return NotFound();
                }
            }

            // Populate dropdowns
            await PopulateDropdowns(product);

            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> AddEdit(ProductModel product)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(product);
                return View(product);
            }

            try
            {
                bool success;
                if (product.ProductId > 0)
                {
                    // Update existing product
                    success = await _apiService.UpdateProductAsync(product);
                    if (success)
                    {
                        TempData["Success"] = "Product updated successfully!";
                    }
                    else
                    {
                        TempData["Error"] = "Failed to update product.";
                    }
                }
                else
                {
                    // Create new product
                    success = await _apiService.CreateProductAsync(product);
                    if (success)
                    {
                        TempData["Success"] = "Product created successfully!";
                    }
                    else
                    {
                        TempData["Error"] = "Failed to create product.";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while saving product");
                TempData["Error"] = "An error occurred while saving the product.";
            }

            return RedirectToAction("ProductList");
        }



        // Helper method to populate dropdowns
        private async Task PopulateDropdowns(ProductModel model)
        {
            try
            {
                // Get categories
                var categories = await _apiService.GetCategoriesAsync();
                model.CategoryList = categories.Select(c => new SelectListItem
                {
                    Value = c.CategoryId.ToString(),
                    Text = c.CategoryName ?? "Unknown"
                }).ToList();

                // Get suppliers
                var suppliers = await _apiService.GetSuppliersAsync();
                model.SupplierList = suppliers.Select(s => new SelectListItem
                {
                    Value = s.SupplierId.ToString(),
                    Text = s.SupplierName ?? "Unknown"
                }).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while populating dropdowns");
                // Set empty lists if dropdowns fail to load
                model.CategoryList = new List<SelectListItem>();
                model.SupplierList = new List<SelectListItem>();
            }
        }
    }
}
