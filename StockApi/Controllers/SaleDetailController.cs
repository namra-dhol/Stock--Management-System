using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockApi.Models;
using Microsoft.AspNetCore.Authorization;

namespace StockApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize] // Require authentication for all endpoints
    public class SaleDetailController : ControllerBase
    {
        private readonly StockContext context;

        public SaleDetailController(StockContext context)
        {
            this.context = context;
        }

        #region GetAllSaleDetails

        [HttpGet]
        public async Task<ActionResult<IEnumerable<SaleDetail>>> GetSaleDetails()
        {
            var saleDetails = await context.SaleDetails
                .Include(sd => sd.Product)
                .Include(sd => sd.Sale)
                    .ThenInclude(s => s.User)
                .OrderByDescending(sd => sd.SaleDetailId)
                .ToListAsync();
            return Ok(saleDetails);
        }
        #endregion 

        #region GetSaleDetailById
        [HttpGet("{id}")]
        public async Task<ActionResult<SaleDetail>> GetSaleDetailById(int id)
        {
            var saleDetail = await context.SaleDetails
                .Include(sd => sd.Product)
                .Include(sd => sd.Sale)
                    .ThenInclude(s => s.User)
                .FirstOrDefaultAsync(sd => sd.SaleDetailId == id);
            if (saleDetail == null)
            {
                return NotFound();
            }
            return Ok(saleDetail);
        }
        #endregion

        #region InsertSaleDetail
        [HttpPost]
        public IActionResult InsertSaleDetail(SaleDetail saleDetail)
        {
            try
            {
                // Calculate subtotal
                saleDetail.SubTotal = saleDetail.Quantity * (saleDetail.UnitPrice ?? 0);

                context.SaleDetails.Add(saleDetail);
                context.SaveChanges();

                // Update sale total amount
                UpdateSaleTotalAmount(saleDetail.SaleId);

                // Update product stock level
                var product = context.Products.Find(saleDetail.ProductId);
                if (product != null)
                {
                    // Check if sufficient stock is available
                    if ((product.StockLevel ?? 0) < saleDetail.Quantity)
                    {
                        return BadRequest($"Insufficient stock for product {product.ProductName}. Available: {product.StockLevel}, Required: {saleDetail.Quantity}");
                    }

                    product.StockLevel = (product.StockLevel ?? 0) - saleDetail.Quantity;
                    context.Products.Update(product);
                    context.SaveChanges();
                }

                return CreatedAtAction(nameof(GetSaleDetailById), new { id = saleDetail.SaleDetailId }, saleDetail);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error creating sale detail: {ex.Message}");
            }
        }

        #endregion

        #region DeleteSaleDetailById
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteSaleDetailById(int id)
        {
            var saleDetail = context.SaleDetails.Find(id);
            if (saleDetail == null)
            {
                return NotFound();
            }

            var saleId = saleDetail.SaleId;
            var productId = saleDetail.ProductId;
            var quantity = saleDetail.Quantity;

            context.SaleDetails.Remove(saleDetail);
            context.SaveChanges();

            // Update sale total amount
            UpdateSaleTotalAmount(saleId);

            // Adjust product stock level
            var product = context.Products.Find(productId);
            if (product != null)
            {
                product.StockLevel = (product.StockLevel ?? 0) + quantity;
                context.Products.Update(product);
                context.SaveChanges();
            }

            return NoContent();
        }
        #endregion

        #region UpdateSaleDetailById

        [HttpPut("{id}")]
        public IActionResult UpdateSaleDetail(int id, SaleDetail saleDetail)
        {
            try
            {
                if (id != saleDetail.SaleDetailId)
                {
                    return BadRequest("ID mismatch");
                }

                var existingSaleDetail = context.SaleDetails.Find(id);
                if (existingSaleDetail == null)
                {
                    return NotFound("Sale detail not found");
                }

                // Store old values for stock adjustment
                var oldQuantity = existingSaleDetail.Quantity;
                var oldProductId = existingSaleDetail.ProductId;

                // Update fields
                existingSaleDetail.SaleId = saleDetail.SaleId;
                existingSaleDetail.ProductId = saleDetail.ProductId;
                existingSaleDetail.Quantity = saleDetail.Quantity;
                existingSaleDetail.UnitPrice = saleDetail.UnitPrice;
                existingSaleDetail.SubTotal = saleDetail.Quantity * (saleDetail.UnitPrice ?? 0);

                context.SaleDetails.Update(existingSaleDetail);
                context.SaveChanges();

                // Update sale total amount
                UpdateSaleTotalAmount(saleDetail.SaleId);

                // Adjust stock levels
                if (oldProductId != saleDetail.ProductId)
                {
                    // Add back to old product
                    var oldProduct = context.Products.Find(oldProductId);
                    if (oldProduct != null)
                    {
                        oldProduct.StockLevel = (oldProduct.StockLevel ?? 0) + oldQuantity;
                        context.Products.Update(oldProduct);
                    }

                    // Remove from new product
                    var newProduct = context.Products.Find(saleDetail.ProductId);
                    if (newProduct != null)
                    {
                        // Check if sufficient stock is available
                        if ((newProduct.StockLevel ?? 0) < saleDetail.Quantity)
                        {
                            return BadRequest($"Insufficient stock for product {newProduct.ProductName}. Available: {newProduct.StockLevel}, Required: {saleDetail.Quantity}");
                        }

                        newProduct.StockLevel = (newProduct.StockLevel ?? 0) - saleDetail.Quantity;
                        context.Products.Update(newProduct);
                    }
                }
                else
                {
                    // Same product, adjust quantity difference
                    var product = context.Products.Find(saleDetail.ProductId);
                    if (product != null)
                    {
                        var quantityDifference = saleDetail.Quantity - oldQuantity;
                        
                        // Check if sufficient stock is available for increase
                        if (quantityDifference > 0 && (product.StockLevel ?? 0) < quantityDifference)
                        {
                            return BadRequest($"Insufficient stock for product {product.ProductName}. Available: {product.StockLevel}, Required: {quantityDifference}");
                        }

                        product.StockLevel = (product.StockLevel ?? 0) - quantityDifference;
                        context.Products.Update(product);
                    }
                }

                context.SaveChanges();
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest($"Error updating sale detail: {ex.Message}");
            }
        }

        #endregion

        #region select Top n record

        [HttpGet("top")]
        public async Task<ActionResult<IEnumerable<SaleDetail>>> GetTopNSaleDetails([FromQuery] int n = 5)
        {
            var saleDetails = await context.SaleDetails
                .Include(sd => sd.Product)
                .Include(sd => sd.Sale)
                    .ThenInclude(s => s.User)
                .OrderByDescending(sd => sd.SaleDetailId)
                .Take(n)
                .ToListAsync();
            return Ok(saleDetails);
        }
        #endregion

        #region Filter
        [HttpGet("Filter")]
        public async Task<ActionResult<IEnumerable<SaleDetail>>> FilterSaleDetail(
            [FromQuery] int? saleDetailId,
            [FromQuery] int? saleId,
            [FromQuery] int? productId,
            [FromQuery] int? minQuantity,
            [FromQuery] int? maxQuantity)
        {
            var query = context.SaleDetails
                .Include(sd => sd.Product)
                .Include(sd => sd.Sale)
                    .ThenInclude(s => s.User)
                .AsQueryable();

            if (saleDetailId.HasValue)
                query = query.Where(sd => sd.SaleDetailId == saleDetailId);

            if (saleId.HasValue)
                query = query.Where(sd => sd.SaleId == saleId);

            if (productId.HasValue)
                query = query.Where(sd => sd.ProductId == productId);

            if (minQuantity.HasValue)
                query = query.Where(sd => sd.Quantity >= minQuantity);

            if (maxQuantity.HasValue)
                query = query.Where(sd => sd.Quantity <= maxQuantity);

            return await query.OrderByDescending(sd => sd.SaleDetailId).ToListAsync();
        }
        #endregion

        #region GetSaleDetailsBySale
        [HttpGet("by-sale/{saleId}")]
        public async Task<ActionResult<IEnumerable<SaleDetail>>> GetSaleDetailsBySale(int saleId)
        {
            var saleDetails = await context.SaleDetails
                .Include(sd => sd.Product)
                .Include(sd => sd.Sale)
                    .ThenInclude(s => s.User)
                .Where(sd => sd.SaleId == saleId)
                .OrderBy(sd => sd.SaleDetailId)
                .ToListAsync();

            if (saleDetails == null || saleDetails.Count == 0)
                return NotFound($"No sale details found for SaleId = {saleId}");

            return Ok(saleDetails);
        }
        #endregion

        #region GetSaleDetailsByProduct
        [HttpGet("by-product/{productId}")]
        public async Task<ActionResult<IEnumerable<SaleDetail>>> GetSaleDetailsByProduct(int productId)
        {
            var saleDetails = await context.SaleDetails
                .Include(sd => sd.Product)
                .Include(sd => sd.Sale)
                    .ThenInclude(s => s.User)
                .Where(sd => sd.ProductId == productId)
                .OrderByDescending(sd => sd.SaleDetailId)
                .ToListAsync();

            if (saleDetails == null || saleDetails.Count == 0)
                return NotFound($"No sale details found for ProductId = {productId}");

            return Ok(saleDetails);
        }
        #endregion

        #region GetSaleDetailSummary
        [HttpGet("summary")]
        public async Task<ActionResult<object>> GetSaleDetailSummary(
            [FromQuery] int? saleId,
            [FromQuery] int? productId)
        {
            var query = context.SaleDetails.AsQueryable();

            if (saleId.HasValue)
                query = query.Where(sd => sd.SaleId == saleId);

            if (productId.HasValue)
                query = query.Where(sd => sd.ProductId == productId);

            var summary = new
            {
                TotalItems = await query.CountAsync(),
                TotalQuantity = await query.SumAsync(sd => sd.Quantity),
                TotalAmount = await query.SumAsync(sd => sd.SubTotal ?? 0),
                AverageUnitPrice = await query.AverageAsync(sd => sd.UnitPrice ?? 0),
                SaleId = saleId,
                ProductId = productId
            };

            return Ok(summary);
        }
        #endregion

        #region Dropdown APIs

        // Get all Products
        [HttpGet("dropdown/products")]
        public async Task<ActionResult<IEnumerable<object>>> GetProducts()
        {
            return await context.Products
                .Select(p => new { p.ProductId, p.ProductName })
                .ToListAsync();
        }

        // Get all Sales
        [HttpGet("dropdown/sales")]
        public async Task<ActionResult<IEnumerable<object>>> GetSales()
        {
            return await context.Sales
                .Select(s => new { s.SaleId, s.SaleDate, s.TotalAmount })
                .ToListAsync();
        }

        #endregion

        #region Helper Method to Update Sale Total Amount
        private void UpdateSaleTotalAmount(int? saleId)
        {
            if (saleId.HasValue)
            {
                var sale = context.Sales.Find(saleId);
                if (sale != null)
                {
                    sale.TotalAmount = context.SaleDetails
                        .Where(sd => sd.SaleId == saleId)
                        .Sum(sd => sd.SubTotal ?? 0);
                    
                    // Recalculate net amount
                    sale.NetAmount = (sale.TotalAmount ?? 0) - (sale.Discount ?? 0) + (sale.Tax ?? 0);
                    
                    context.Sales.Update(sale);
                    context.SaveChanges();
                }
            }
        }
        #endregion
    }
}
