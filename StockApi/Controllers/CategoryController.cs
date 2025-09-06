using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockApi.Models;
using Microsoft.AspNetCore.Authorization;

namespace StockApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize] // Require authentication for all endpoints
    public class CategoryController : ControllerBase
    {
        private readonly StockContext context;

        public CategoryController(StockContext context)
        {
            this.context = context;
        }

        #region Category List with Pagination

        [HttpGet]
        public async Task<IActionResult> GetCategories(int pageNumber = 1, int pageSize = 5)
        {
            try
            {
                var totalRecords = await context.Categories.CountAsync();

                var categories = await context.Categories
                    .OrderBy(c => c.CategoryId)   // Order by Id to maintain consistent paging
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                var response = new
                {
                    TotalRecords = totalRecords,
                    PageSize = pageSize,
                    CurrentPage = pageNumber,
                    TotalPages = (int)Math.Ceiling((double)totalRecords / pageSize),
                    Categories = categories
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        #endregion

        #region GetCategoryById
        [HttpGet("{id}")]
        public async Task<ActionResult<Category>> GetCategoryById(int id)
        {
            var category = await context.Categories.FindAsync(id);
            if (category == null)
                return NotFound();

            return Ok(category);
        }
        #endregion

        #region InsertCategory
        [HttpPost]
        public IActionResult InsertCategory([FromBody] Category category)
        {
            try
            {
                // Set default values
                category.CreatedAt = DateTime.Now;
                category.ModifiedAt = DateTime.Now;

                context.Categories.Add(category);
                context.SaveChanges();
                return CreatedAtAction(nameof(GetCategoryById), new { id = category.CategoryId }, category);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error creating category: {ex.Message}");
            }
        }
        #endregion

        #region UpdateCategoryById
        [HttpPut("{id}")]
        public IActionResult UpdateCategory(int id, [FromBody] Category category)
        {
            try
            {
                if (id != category.CategoryId)
                {
                    return BadRequest("ID mismatch");
                }

                var existingCategory = context.Categories.Find(id);
                if (existingCategory == null)
                {
                    return NotFound("Category not found");
                }

                // Update fields
                existingCategory.CategoryName = category.CategoryName;
                existingCategory.UserId = category.UserId;
                existingCategory.ModifiedAt = DateTime.Now;

                context.Categories.Update(existingCategory);
                context.SaveChanges();
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest($"Error updating category: {ex.Message}");
            }
        }
        #endregion

        #region DeleteCategoryById
        [HttpDelete("{id}")]
        public IActionResult DeleteCategory(int id)
        {
            var category = context.Categories.Find(id);
            if (category == null)
                return NotFound();

            // Check if category has products
            var hasProducts = context.Products.Any(p => p.CategoryId == id);
            if (hasProducts)
            {
                return BadRequest("Cannot delete category that has associated products.");
            }

            context.Categories.Remove(category);
            context.SaveChanges();
            return NoContent();
        }
        #endregion

        #region FilterCategories
        [HttpGet("Filter")]
        //[Authorize(Roles = "Admin")] // Only admins can filter categories
        public async Task<ActionResult<IEnumerable<Category>>> FilterCategories(
            [FromQuery] string? categoryName,
            [FromQuery] int? userId)
        {
            var query = context.Categories.AsQueryable();

            if (!string.IsNullOrEmpty(categoryName))
                query = query.Where(c => c.CategoryName!.Contains(categoryName));

            if (userId.HasValue)
                query = query.Where(c => c.UserId == userId);

            return await query.ToListAsync();
        }
        #endregion

        #region GetTopNCategories
        [HttpGet("top")]
        //[Authorize(Roles = "Admin")] // Only admins can get top categories
        public async Task<ActionResult<IEnumerable<Category>>> GetTopNCategories([FromQuery] int n = 5)
        {
            var categories = await context.Categories.Take(n).ToListAsync();
            return Ok(categories);
        }
        #endregion

        #region GetCategoriesWithProductCount
        [HttpGet("with-product-count")]
        public async Task<IActionResult> GetCategoriesWithProductCount()
        {
            try
            {
                var categoriesWithCount = await context.Categories
                    .Select(c => new
                    {
                        c.CategoryId,
                        c.CategoryName,
                        c.CreatedAt,
                        c.ModifiedAt,
                        c.UserId,
                        ProductCount = c.Products.Count
                    })
                    .ToListAsync();

                return Ok(categoriesWithCount);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
        #endregion
    }
}
