using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockApi.Models;
using Microsoft.AspNetCore.Authorization;

namespace StockApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize] // Require authentication for all endpoints
    public class ProductController : ControllerBase
    {
        private readonly StockContext context;

        public ProductController(StockContext context)
        {
            this.context = context;
        }

        #region GetAllProducts

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts()
        {
            var products = await context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .ToListAsync();
            return Ok(products);
        }
        #endregion 

        #region GetProductById
        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetProductById(int id)
        {
            var product = await context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .FirstOrDefaultAsync(p => p.ProductId == id);
            if (product == null)
            {
                return NotFound();
            }
            return Ok(product);
        }
        #endregion

        #region InsertProduct
        [HttpPost]
        public IActionResult InsertProduct(Product product)
        {
            try
            {
                // Set default stock level if not provided
                if (product.StockLevel == null)
                    product.StockLevel = 0;

                context.Products.Add(product);
                context.SaveChanges();
                return CreatedAtAction(nameof(GetProductById), new { id = product.ProductId }, product);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error creating product: {ex.Message}");
            }
        }

        #endregion

        #region DeleteProductById
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteProductById(int id)
        {
            var product = context.Products.Find(id);
            if (product == null)
            {
                return NotFound();
            }

            // Check if product has purchase or sale details
            var hasPurchaseDetails = context.PurchaseDetails.Any(pd => pd.ProductId == id);
            var hasSaleDetails = context.SaleDetails.Any(sd => sd.ProductId == id);
            
            if (hasPurchaseDetails || hasSaleDetails)
            {
                return BadRequest("Cannot delete product that has associated purchase or sale records.");
            }

            context.Products.Remove(product);
            context.SaveChanges();
            return NoContent();
        }
        #endregion

        #region UpdateProductById

        [HttpPut("{id}")]
        public IActionResult UpdateProduct(int id, Product product)
        {
            try
            {
                if (id != product.ProductId)
                {
                    return BadRequest("ID mismatch");
                }

                var existingProduct = context.Products.Find(id);
                if (existingProduct == null)
                {
                    return NotFound("Product not found");
                }

                // Update fields
                existingProduct.ProductName = product.ProductName;
                existingProduct.CategoryId = product.CategoryId;
                existingProduct.SupplierId = product.SupplierId;
                existingProduct.Unit = product.Unit;
                existingProduct.CostPrice = product.CostPrice;
                existingProduct.SellingPrice = product.SellingPrice;
                existingProduct.StockLevel = product.StockLevel;

                context.Products.Update(existingProduct);
                context.SaveChanges();
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest($"Error updating product: {ex.Message}");
            }
        }

        #endregion

        #region select Top n record

        [HttpGet("top")]
        public async Task<ActionResult<IEnumerable<Product>>> GetTopNProducts([FromQuery] int n = 5)
        {
            var products = await context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .Take(n)
                .ToListAsync();
            return Ok(products);
        }
        #endregion

        #region Filter
        [HttpGet("Filter")]
        public async Task<ActionResult<IEnumerable<Product>>> FilterProduct(
            [FromQuery] int? productId, 
            [FromQuery] string? productName,
            [FromQuery] int? categoryId,
            [FromQuery] int? supplierId)
        {
            var query = context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .AsQueryable();

            if (productId.HasValue)
                query = query.Where(p => p.ProductId == productId);

            if (!string.IsNullOrEmpty(productName))
                query = query.Where(p => p.ProductName!.Contains(productName));

            if (categoryId.HasValue)
                query = query.Where(p => p.CategoryId == categoryId);

            if (supplierId.HasValue)
                query = query.Where(p => p.SupplierId == supplierId);

            return await query.ToListAsync();
        }
        #endregion

        #region GetProductsByCategory
        [HttpGet("by-category/{categoryId}")]
        public async Task<ActionResult<IEnumerable<Product>>> GetProductsByCategory(int categoryId)
        {
            var products = await context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .Where(p => p.CategoryId == categoryId)
                .ToListAsync();

            if (products == null || products.Count == 0)
                return NotFound($"No products found for CategoryId = {categoryId}");

            return Ok(products);
        }
        #endregion

        #region GetProductsBySupplier
        [HttpGet("by-supplier/{supplierId}")]
        public async Task<ActionResult<IEnumerable<Product>>> GetProductsBySupplier(int supplierId)
        {
            var products = await context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .Where(p => p.SupplierId == supplierId)
                .ToListAsync();

            if (products == null || products.Count == 0)
                return NotFound($"No products found for SupplierId = {supplierId}");

            return Ok(products);
        }
        #endregion

        #region GetLowStockProducts
        [HttpGet("low-stock")]
        public async Task<ActionResult<IEnumerable<Product>>> GetLowStockProducts([FromQuery] int threshold = 10)
        {
            var products = await context.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .Where(p => p.StockLevel <= threshold)
                .ToListAsync();

            return Ok(products);
        }
        #endregion

        #region UpdateStockLevel
        [HttpPut("{id}/stock")]
        public IActionResult UpdateStockLevel(int id, [FromBody] int newStockLevel)
        {
            try
            {
                var product = context.Products.Find(id);
                if (product == null)
                    return NotFound();

                product.StockLevel = newStockLevel;
                context.Products.Update(product);
                context.SaveChanges();
                return Ok(product);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error updating stock level: {ex.Message}");
            }
        }
        #endregion

        #region Dropdown APIs

        // Get all Categories
        [HttpGet("dropdown/categories")]
        public async Task<ActionResult<IEnumerable<object>>> GetCategories()
        {
            return await context.Categories
                .Select(c => new { c.CategoryId, c.CategoryName })
                .ToListAsync();
        }

        // Get all Suppliers
        [HttpGet("dropdown/suppliers")]
        public async Task<ActionResult<IEnumerable<object>>> GetSuppliers()
        {
            return await context.Suppliers
                .Select(s => new { s.SupplierId, s.SupplierName })
                .ToListAsync();
        }

        #endregion
    }
}