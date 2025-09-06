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
    public class CategoryController : Controller
    {
        private readonly IApiService _apiService;
        private readonly ILogger<CategoryController> _logger;

        public CategoryController(IApiService apiService, ILogger<CategoryController> logger)
        {
            _apiService = apiService;
            _logger = logger;
        }

        // List all categories
        public async Task<IActionResult> CategoryList()
        {
            try
            {
                var categories = await _apiService.GetAllCategoriesAsync();
                return View(categories);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching categories");
                TempData["Error"] = "Failed to load categories. Please try again.";
                return View(new List<CategoryModel>());
            }
        }

        // Delete category by ID
        public async Task<IActionResult> Delete(int id)
        {
            try
            {
                var success = await _apiService.DeleteCategoryAsync(id);
                if (success)
                {
                    TempData["Success"] = "Category deleted successfully!";
                }
                else
                {
                    TempData["Error"] = "Failed to delete category.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while deleting category with ID {id}");
                TempData["Error"] = "An error occurred while deleting the category.";
            }
            
            return RedirectToAction("CategoryList");
        }

        // GET: Add/Edit Category
        public async Task<IActionResult> AddEdit(int? id)
        {
            CategoryModel category;

            if (id == null)
            {
                category = new CategoryModel();
            }
            else
            {
                category = await _apiService.GetCategoryByIdAsync(id.Value);
                if (category == null)
                {
                    return NotFound();
                }
            }

            // Populate dropdowns
            await PopulateDropdowns(category);

            return View(category);
        }

        [HttpPost]
        public async Task<IActionResult> AddEdit(CategoryModel category)
        {
            if (!ModelState.IsValid)
            {
                await PopulateDropdowns(category);
                return View(category);
            }

            try
            {
                bool success;
                if (category.CategoryId > 0)
                {
                    // Update existing category
                    success = await _apiService.UpdateCategoryAsync(category);
                    if (success)
                    {
                        TempData["Success"] = "Category updated successfully!";
                    }
                    else
                    {
                        TempData["Error"] = "Failed to update category.";
                    }
                }
                else
                {
                    // Create new category
                    success = await _apiService.CreateCategoryAsync(category);
                    if (success)
                    {
                        TempData["Success"] = "Category created successfully!";
                    }
                    else
                    {
                        TempData["Error"] = "Failed to create category.";
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while saving category");
                TempData["Error"] = "An error occurred while saving the category.";
            }

            return RedirectToAction("CategoryList");
        }

        // Helper method to populate dropdowns
        private async Task PopulateDropdowns(CategoryModel model)
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
