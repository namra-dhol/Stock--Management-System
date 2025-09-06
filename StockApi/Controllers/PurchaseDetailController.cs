using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockApi.Models;
using Microsoft.AspNetCore.Authorization;

namespace StockApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize] // Require authentication for all endpoints
    public class PurchaseDetailController : ControllerBase
    {
        private readonly StockContext context;

        public PurchaseDetailController(StockContext context)
        {
            this.context = context;
        }

        #region GetAllPurchaseDetails

        [HttpGet]
        public async Task<ActionResult<IEnumerable<PurchaseDetail>>> GetPurchaseDetails()
        {
            var purchaseDetails = await context.PurchaseDetails
                .Include(pd => pd.Product)
                .Include(pd => pd.Purchase)
                    .ThenInclude(p => p.Supplier)
                .OrderByDescending(pd => pd.PurchaseDetailId)
                .ToListAsync();
            return Ok(purchaseDetails);
        }
        #endregion 

        #region GetPurchaseDetailById
        [HttpGet("{id}")]
        public async Task<ActionResult<PurchaseDetail>> GetPurchaseDetailById(int id)
        {
            var purchaseDetail = await context.PurchaseDetails
                .Include(pd => pd.Product)
                .Include(pd => pd.Purchase)
                    .ThenInclude(p => p.Supplier)
                .FirstOrDefaultAsync(pd => pd.PurchaseDetailId == id);
            if (purchaseDetail == null)
            {
                return NotFound();
            }
            return Ok(purchaseDetail);
        }
        #endregion

        #region InsertPurchaseDetail
        [HttpPost]
        public IActionResult InsertPurchaseDetail(PurchaseDetail purchaseDetail)
        {
            try
            {
                // Calculate subtotal
                purchaseDetail.SubTotal = purchaseDetail.Quantity * (purchaseDetail.UnitCost ?? 0);

                context.PurchaseDetails.Add(purchaseDetail);
                context.SaveChanges();

                // Update purchase total amount
                UpdatePurchaseTotalAmount(purchaseDetail.PurchaseId);

                // Update product stock level
                var product = context.Products.Find(purchaseDetail.ProductId);
                if (product != null)
                {
                    product.StockLevel = (product.StockLevel ?? 0) + purchaseDetail.Quantity;
                    context.Products.Update(product);
                    context.SaveChanges();
                }

                return CreatedAtAction(nameof(GetPurchaseDetailById), new { id = purchaseDetail.PurchaseDetailId }, purchaseDetail);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error creating purchase detail: {ex.Message}");
            }
        }

        #endregion

        #region DeletePurchaseDetailById
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public IActionResult DeletePurchaseDetailById(int id)
        {
            var purchaseDetail = context.PurchaseDetails.Find(id);
            if (purchaseDetail == null)
            {
                return NotFound();
            }

            var purchaseId = purchaseDetail.PurchaseId;
            var productId = purchaseDetail.ProductId;
            var quantity = purchaseDetail.Quantity;

            context.PurchaseDetails.Remove(purchaseDetail);
            context.SaveChanges();

            // Update purchase total amount
            UpdatePurchaseTotalAmount(purchaseId);

            // Adjust product stock level
            var product = context.Products.Find(productId);
            if (product != null)
            {
                product.StockLevel = Math.Max(0, (product.StockLevel ?? 0) - quantity);
                context.Products.Update(product);
                context.SaveChanges();
            }

            return NoContent();
        }
        #endregion

        #region UpdatePurchaseDetailById

        [HttpPut("{id}")]
        public IActionResult UpdatePurchaseDetail(int id, PurchaseDetail purchaseDetail)
        {
            try
            {
                if (id != purchaseDetail.PurchaseDetailId)
                {
                    return BadRequest("ID mismatch");
                }

                var existingPurchaseDetail = context.PurchaseDetails.Find(id);
                if (existingPurchaseDetail == null)
                {
                    return NotFound("Purchase detail not found");
                }

                // Store old values for stock adjustment
                var oldQuantity = existingPurchaseDetail.Quantity;
                var oldProductId = existingPurchaseDetail.ProductId;

                // Update fields
                existingPurchaseDetail.PurchaseId = purchaseDetail.PurchaseId;
                existingPurchaseDetail.ProductId = purchaseDetail.ProductId;
                existingPurchaseDetail.Quantity = purchaseDetail.Quantity;
                existingPurchaseDetail.UnitCost = purchaseDetail.UnitCost;
                existingPurchaseDetail.SubTotal = purchaseDetail.Quantity * (purchaseDetail.UnitCost ?? 0);

                context.PurchaseDetails.Update(existingPurchaseDetail);
                context.SaveChanges();

                // Update purchase total amount
                UpdatePurchaseTotalAmount(purchaseDetail.PurchaseId);

                // Adjust stock levels
                if (oldProductId != purchaseDetail.ProductId)
                {
                    // Remove from old product
                    var oldProduct = context.Products.Find(oldProductId);
                    if (oldProduct != null)
                    {
                        oldProduct.StockLevel = Math.Max(0, (oldProduct.StockLevel ?? 0) - oldQuantity);
                        context.Products.Update(oldProduct);
                    }

                    // Add to new product
                    var newProduct = context.Products.Find(purchaseDetail.ProductId);
                    if (newProduct != null)
                    {
                        newProduct.StockLevel = (newProduct.StockLevel ?? 0) + purchaseDetail.Quantity;
                        context.Products.Update(newProduct);
                    }
                }
                else
                {
                    // Same product, adjust quantity difference
                    var product = context.Products.Find(purchaseDetail.ProductId);
                    if (product != null)
                    {
                        var quantityDifference = purchaseDetail.Quantity - oldQuantity;
                        product.StockLevel = Math.Max(0, (product.StockLevel ?? 0) + quantityDifference);
                        context.Products.Update(product);
                    }
                }

                context.SaveChanges();
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest($"Error updating purchase detail: {ex.Message}");
            }
        }

        #endregion

        #region select Top n record

        [HttpGet("top")]
        public async Task<ActionResult<IEnumerable<PurchaseDetail>>> GetTopNPurchaseDetails([FromQuery] int n = 5)
        {
            var purchaseDetails = await context.PurchaseDetails
                .Include(pd => pd.Product)
                .Include(pd => pd.Purchase)
                    .ThenInclude(p => p.Supplier)
                .OrderByDescending(pd => pd.PurchaseDetailId)
                .Take(n)
                .ToListAsync();
            return Ok(purchaseDetails);
        }
        #endregion

        #region Filter
        [HttpGet("Filter")]
        public async Task<ActionResult<IEnumerable<PurchaseDetail>>> FilterPurchaseDetail(
            [FromQuery] int? purchaseDetailId,
            [FromQuery] int? purchaseId,
            [FromQuery] int? productId,
            [FromQuery] int? minQuantity,
            [FromQuery] int? maxQuantity)
        {
            var query = context.PurchaseDetails
                .Include(pd => pd.Product)
                .Include(pd => pd.Purchase)
                    .ThenInclude(p => p.Supplier)
                .AsQueryable();

            if (purchaseDetailId.HasValue)
                query = query.Where(pd => pd.PurchaseDetailId == purchaseDetailId);

            if (purchaseId.HasValue)
                query = query.Where(pd => pd.PurchaseId == purchaseId);

            if (productId.HasValue)
                query = query.Where(pd => pd.ProductId == productId);

            if (minQuantity.HasValue)
                query = query.Where(pd => pd.Quantity >= minQuantity);

            if (maxQuantity.HasValue)
                query = query.Where(pd => pd.Quantity <= maxQuantity);

            return await query.OrderByDescending(pd => pd.PurchaseDetailId).ToListAsync();
        }
        #endregion

        #region GetPurchaseDetailsByPurchase
        [HttpGet("by-purchase/{purchaseId}")]
        public async Task<ActionResult<IEnumerable<PurchaseDetail>>> GetPurchaseDetailsByPurchase(int purchaseId)
        {
            var purchaseDetails = await context.PurchaseDetails
                .Include(pd => pd.Product)
                .Include(pd => pd.Purchase)
                    .ThenInclude(p => p.Supplier)
                .Where(pd => pd.PurchaseId == purchaseId)
                .OrderBy(pd => pd.PurchaseDetailId)
                .ToListAsync();

            if (purchaseDetails == null || purchaseDetails.Count == 0)
                return NotFound($"No purchase details found for PurchaseId = {purchaseId}");

            return Ok(purchaseDetails);
        }
        #endregion

        #region GetPurchaseDetailsByProduct
        [HttpGet("by-product/{productId}")]
        public async Task<ActionResult<IEnumerable<PurchaseDetail>>> GetPurchaseDetailsByProduct(int productId)
        {
            var purchaseDetails = await context.PurchaseDetails
                .Include(pd => pd.Product)
                .Include(pd => pd.Purchase)
                    .ThenInclude(p => p.Supplier)
                .Where(pd => pd.ProductId == productId)
                .OrderByDescending(pd => pd.PurchaseDetailId)
                .ToListAsync();

            if (purchaseDetails == null || purchaseDetails.Count == 0)
                return NotFound($"No purchase details found for ProductId = {productId}");

            return Ok(purchaseDetails);
        }
        #endregion

        #region GetPurchaseDetailSummary
        [HttpGet("summary")]
        public async Task<ActionResult<object>> GetPurchaseDetailSummary(
            [FromQuery] int? purchaseId,
            [FromQuery] int? productId)
        {
            var query = context.PurchaseDetails.AsQueryable();

            if (purchaseId.HasValue)
                query = query.Where(pd => pd.PurchaseId == purchaseId);

            if (productId.HasValue)
                query = query.Where(pd => pd.ProductId == productId);

            var summary = new
            {
                TotalItems = await query.CountAsync(),
                TotalQuantity = await query.SumAsync(pd => pd.Quantity),
                TotalAmount = await query.SumAsync(pd => pd.SubTotal ?? 0),
                AverageUnitCost = await query.AverageAsync(pd => pd.UnitCost ?? 0),
                PurchaseId = purchaseId,
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

        // Get all Purchases
        [HttpGet("dropdown/purchases")]
        public async Task<ActionResult<IEnumerable<object>>> GetPurchases()
        {
            return await context.Purchases
                .Select(p => new { p.PurchaseId, p.PurchaseDate, p.TotalAmount })
                .ToListAsync();
        }

        #endregion

        #region Helper Method to Update Purchase Total Amount
        private void UpdatePurchaseTotalAmount(int? purchaseId)
        {
            if (purchaseId.HasValue)
            {
                var purchase = context.Purchases.Find(purchaseId);
                if (purchase != null)
                {
                    purchase.TotalAmount = context.PurchaseDetails
                        .Where(pd => pd.PurchaseId == purchaseId)
                        .Sum(pd => pd.SubTotal ?? 0);
                    
                    context.Purchases.Update(purchase);
                    context.SaveChanges();
                }
            }
        }
        #endregion
    }
}