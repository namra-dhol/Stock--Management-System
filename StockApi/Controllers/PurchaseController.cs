using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockApi.Models;
using Microsoft.AspNetCore.Authorization;

namespace StockApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize] // Require authentication for all endpoints
    public class PurchaseController : ControllerBase
    {
        private readonly StockContext context;

        public PurchaseController(StockContext context)
        {
            this.context = context;
        }

        #region GetAllPurchases

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Purchase>>> GetPurchases()
        {
            var purchases = await context.Purchases
                .Include(p => p.Supplier)
                .Include(p => p.User)
                .Include(p => p.PurchaseDetails)
                    .ThenInclude(pd => pd.Product)
                .OrderByDescending(p => p.PurchaseDate)
                .ToListAsync();
            return Ok(purchases);
        }
        #endregion 

        #region GetPurchaseById
        [HttpGet("{id}")]
        public async Task<ActionResult<Purchase>> GetPurchaseById(int id)
        {
            var purchase = await context.Purchases
                .Include(p => p.Supplier)
                .Include(p => p.User)
                .Include(p => p.PurchaseDetails)
                    .ThenInclude(pd => pd.Product)
                .FirstOrDefaultAsync(p => p.PurchaseId == id);
            if (purchase == null)
            {
                return NotFound();
            }
            return Ok(purchase);
        }
        #endregion

        #region InsertPurchase
        [HttpPost]
        public IActionResult InsertPurchase(Purchase purchase)
        {
            try
            {
                // Set default values
                if (purchase.PurchaseDate == null)
                    purchase.PurchaseDate = DateTime.Now;

                // Calculate total amount from purchase details
                if (purchase.PurchaseDetails != null && purchase.PurchaseDetails.Any())
                {
                    purchase.TotalAmount = purchase.PurchaseDetails.Sum(pd => pd.SubTotal ?? 0);
                }

                context.Purchases.Add(purchase);
                context.SaveChanges();

                // Update product stock levels
                if (purchase.PurchaseDetails != null)
                {
                    foreach (var detail in purchase.PurchaseDetails)
                    {
                        var product = context.Products.Find(detail.ProductId);
                        if (product != null)
                        {
                            product.StockLevel = (product.StockLevel ?? 0) + detail.Quantity;
                            context.Products.Update(product);
                        }
                    }
                    context.SaveChanges();
                }

                return CreatedAtAction(nameof(GetPurchaseById), new { id = purchase.PurchaseId }, purchase);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error creating purchase: {ex.Message}");
            }
        }

        #endregion

        #region DeletePurchaseById
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public IActionResult DeletePurchaseById(int id)
        {
            var purchase = context.Purchases
                .Include(p => p.PurchaseDetails)
                .FirstOrDefault(p => p.PurchaseId == id);
            
            if (purchase == null)
            {
                return NotFound();
            }

            // Reverse stock updates before deleting
            if (purchase.PurchaseDetails != null)
            {
                foreach (var detail in purchase.PurchaseDetails)
                {
                    var product = context.Products.Find(detail.ProductId);
                    if (product != null)
                    {
                        product.StockLevel = Math.Max(0, (product.StockLevel ?? 0) - detail.Quantity);
                        context.Products.Update(product);
                    }
                }
            }

            context.Purchases.Remove(purchase);
            context.SaveChanges();
            return NoContent();
        }
        #endregion

        #region UpdatePurchaseById

        [HttpPut("{id}")]
        public IActionResult UpdatePurchase(int id, Purchase purchase)
        {
            try
            {
                if (id != purchase.PurchaseId)
                {
                    return BadRequest("ID mismatch");
                }

                var existingPurchase = context.Purchases.Find(id);
                if (existingPurchase == null)
                {
                    return NotFound("Purchase not found");
                }

                // Update fields
                existingPurchase.SupplierId = purchase.SupplierId;
                existingPurchase.UserId = purchase.UserId;
                existingPurchase.PurchaseDate = purchase.PurchaseDate;
                existingPurchase.TotalAmount = purchase.TotalAmount;

                context.Purchases.Update(existingPurchase);
                context.SaveChanges();
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest($"Error updating purchase: {ex.Message}");
            }
        }

        #endregion

        #region select Top n record

        [HttpGet("top")]
        public async Task<ActionResult<IEnumerable<Purchase>>> GetTopNPurchases([FromQuery] int n = 5)
        {
            var purchases = await context.Purchases
                .Include(p => p.Supplier)
                .Include(p => p.User)
                .Include(p => p.PurchaseDetails)
                    .ThenInclude(pd => pd.Product)
                .OrderByDescending(p => p.PurchaseDate)
                .Take(n)
                .ToListAsync();
            return Ok(purchases);
        }
        #endregion

        #region Filter
        [HttpGet("Filter")]
        public async Task<ActionResult<IEnumerable<Purchase>>> FilterPurchase(
            [FromQuery] int? purchaseId,
            [FromQuery] int? supplierId,
            [FromQuery] int? userId,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var query = context.Purchases
                .Include(p => p.Supplier)
                .Include(p => p.User)
                .Include(p => p.PurchaseDetails)
                    .ThenInclude(pd => pd.Product)
                .AsQueryable();

            if (purchaseId.HasValue)
                query = query.Where(p => p.PurchaseId == purchaseId);

            if (supplierId.HasValue)
                query = query.Where(p => p.SupplierId == supplierId);

            if (userId.HasValue)
                query = query.Where(p => p.UserId == userId);

            if (startDate.HasValue)
                query = query.Where(p => p.PurchaseDate >= startDate);

            if (endDate.HasValue)
                query = query.Where(p => p.PurchaseDate <= endDate);

            return await query.OrderByDescending(p => p.PurchaseDate).ToListAsync();
        }
        #endregion

        #region GetPurchasesBySupplier
        [HttpGet("by-supplier/{supplierId}")]
        public async Task<ActionResult<IEnumerable<Purchase>>> GetPurchasesBySupplier(int supplierId)
        {
            var purchases = await context.Purchases
                .Include(p => p.Supplier)
                .Include(p => p.User)
                .Include(p => p.PurchaseDetails)
                    .ThenInclude(pd => pd.Product)
                .Where(p => p.SupplierId == supplierId)
                .OrderByDescending(p => p.PurchaseDate)
                .ToListAsync();

            if (purchases == null || purchases.Count == 0)
                return NotFound($"No purchases found for SupplierId = {supplierId}");

            return Ok(purchases);
        }
        #endregion

        #region GetPurchasesByUser
        [HttpGet("by-user/{userId}")]
        public async Task<ActionResult<IEnumerable<Purchase>>> GetPurchasesByUser(int userId)
        {
            var purchases = await context.Purchases
                .Include(p => p.Supplier)
                .Include(p => p.User)
                .Include(p => p.PurchaseDetails)
                    .ThenInclude(pd => pd.Product)
                .Where(p => p.UserId == userId)
                .OrderByDescending(p => p.PurchaseDate)
                .ToListAsync();

            if (purchases == null || purchases.Count == 0)
                return NotFound($"No purchases found for UserId = {userId}");

            return Ok(purchases);
        }
        #endregion

        #region GetRecentPurchases
        [HttpGet("recent")]
        public async Task<ActionResult<IEnumerable<Purchase>>> GetRecentPurchases([FromQuery] int days = 7)
        {
            var startDate = DateTime.Now.AddDays(-days);
            var purchases = await context.Purchases
                .Include(p => p.Supplier)
                .Include(p => p.User)
                .Include(p => p.PurchaseDetails)
                    .ThenInclude(pd => pd.Product)
                .Where(p => p.PurchaseDate >= startDate)
                .OrderByDescending(p => p.PurchaseDate)
                .ToListAsync();

            return Ok(purchases);
        }
        #endregion

        #region GetPurchaseSummary
        [HttpGet("summary")]
        public async Task<ActionResult<object>> GetPurchaseSummary(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate)
        {
            var query = context.Purchases.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(p => p.PurchaseDate >= startDate);

            if (endDate.HasValue)
                query = query.Where(p => p.PurchaseDate <= endDate);

            var summary = new
            {
                TotalPurchases = await query.CountAsync(),
                TotalAmount = await query.SumAsync(p => p.TotalAmount ?? 0),
                AverageAmount = await query.AverageAsync(p => p.TotalAmount ?? 0),
                StartDate = startDate,
                EndDate = endDate
            };

            return Ok(summary);
        }
        #endregion

        #region Dropdown APIs

        // Get all Suppliers
        [HttpGet("dropdown/suppliers")]
        public async Task<ActionResult<IEnumerable<object>>> GetSuppliers()
        {
            return await context.Suppliers
                .Select(s => new { s.SupplierId, s.SupplierName })
                .ToListAsync();
        }

        // Get all Users
        [HttpGet("dropdown/users")]
        public async Task<ActionResult<IEnumerable<object>>> GetUsers()
        {
            return await context.Users
                .Select(u => new { u.UserId, u.UserName })
                .ToListAsync();
        }

        #endregion
    }
}