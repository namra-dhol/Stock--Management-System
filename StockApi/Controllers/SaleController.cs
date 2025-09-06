using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockApi.Models;
using Microsoft.AspNetCore.Authorization;

namespace StockApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize] // Require authentication for all endpoints
    public class SaleController : ControllerBase
    {
        private readonly StockContext context;

        public SaleController(StockContext context)
        {
            this.context = context;
        }

        #region GetAllSales

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Sale>>> GetSales()
        {
            var sales = await context.Sales
                .Include(s => s.User)
                .Include(s => s.SaleDetails)
                    .ThenInclude(sd => sd.Product)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();
            return Ok(sales);
        }
        #endregion 

        #region GetSaleById
        [HttpGet("{id}")]
        public async Task<ActionResult<Sale>> GetSaleById(int id)
        {
            var sale = await context.Sales
                .Include(s => s.User)
                .Include(s => s.SaleDetails)
                    .ThenInclude(sd => sd.Product)
                .FirstOrDefaultAsync(s => s.SaleId == id);
            if (sale == null)
            {
                return NotFound();
            }
            return Ok(sale);
        }
        #endregion

        #region InsertSale
        [HttpPost]
        public IActionResult InsertSale(Sale sale)
        {
            try
            {
                // Set default values
                if (sale.SaleDate == null)
                    sale.SaleDate = DateTime.Now;

                if (sale.Discount == null)
                    sale.Discount = 0;

                if (sale.Tax == null)
                    sale.Tax = 0;

                // Calculate total amount from sale details
                if (sale.SaleDetails != null && sale.SaleDetails.Any())
                {
                    sale.TotalAmount = sale.SaleDetails.Sum(sd => sd.SubTotal ?? 0);
                }

                // Calculate net amount
                sale.NetAmount = (sale.TotalAmount ?? 0) - (sale.Discount ?? 0) + (sale.Tax ?? 0);

                context.Sales.Add(sale);
                context.SaveChanges();

                // Update product stock levels
                if (sale.SaleDetails != null)
                {
                    foreach (var detail in sale.SaleDetails)
                    {
                        var product = context.Products.Find(detail.ProductId);
                        if (product != null)
                        {
                            // Check if sufficient stock is available
                            if ((product.StockLevel ?? 0) < detail.Quantity)
                            {
                                return BadRequest($"Insufficient stock for product {product.ProductName}. Available: {product.StockLevel}, Required: {detail.Quantity}");
                            }

                            product.StockLevel = (product.StockLevel ?? 0) - detail.Quantity;
                            context.Products.Update(product);
                        }
                    }
                    context.SaveChanges();
                }

                return CreatedAtAction(nameof(GetSaleById), new { id = sale.SaleId }, sale);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error creating sale: {ex.Message}");
            }
        }

        #endregion

        #region DeleteSaleById
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteSaleById(int id)
        {
            var sale = context.Sales
                .Include(s => s.SaleDetails)
                .FirstOrDefault(s => s.SaleId == id);
            
            if (sale == null)
            {
                return NotFound();
            }

            // Reverse stock updates before deleting
            if (sale.SaleDetails != null)
            {
                foreach (var detail in sale.SaleDetails)
                {
                    var product = context.Products.Find(detail.ProductId);
                    if (product != null)
                    {
                        product.StockLevel = (product.StockLevel ?? 0) + detail.Quantity;
                        context.Products.Update(product);
                    }
                }
            }

            context.Sales.Remove(sale);
            context.SaveChanges();
            return NoContent();
        }
        #endregion

        #region UpdateSaleById

        [HttpPut("{id}")]
        public IActionResult UpdateSale(int id, Sale sale)
        {
            try
            {
                if (id != sale.SaleId)
                {
                    return BadRequest("ID mismatch");
                }

                var existingSale = context.Sales.Find(id);
                if (existingSale == null)
                {
                    return NotFound("Sale not found");
                }

                // Update fields
                existingSale.UserId = sale.UserId;
                existingSale.SaleDate = sale.SaleDate;
                existingSale.TotalAmount = sale.TotalAmount;
                existingSale.Discount = sale.Discount;
                existingSale.Tax = sale.Tax;
                existingSale.NetAmount = (sale.TotalAmount ?? 0) - (sale.Discount ?? 0) + (sale.Tax ?? 0);

                context.Sales.Update(existingSale);
                context.SaveChanges();
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest($"Error updating sale: {ex.Message}");
            }
        }

        #endregion

        #region select Top n record

        [HttpGet("top")]
        public async Task<ActionResult<IEnumerable<Sale>>> GetTopNSales([FromQuery] int n = 5)
        {
            var sales = await context.Sales
                .Include(s => s.User)
                .Include(s => s.SaleDetails)
                    .ThenInclude(sd => sd.Product)
                .OrderByDescending(s => s.SaleDate)
                .Take(n)
                .ToListAsync();
            return Ok(sales);
        }
        #endregion

        #region Filter
        [HttpGet("Filter")]
        public async Task<ActionResult<IEnumerable<Sale>>> FilterSale(
            [FromQuery] int? saleId,
            [FromQuery] int? userId,
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] decimal? minAmount,
            [FromQuery] decimal? maxAmount)
        {
            var query = context.Sales
                .Include(s => s.User)
                .Include(s => s.SaleDetails)
                    .ThenInclude(sd => sd.Product)
                .AsQueryable();

            if (saleId.HasValue)
                query = query.Where(s => s.SaleId == saleId);

            if (userId.HasValue)
                query = query.Where(s => s.UserId == userId);

            if (startDate.HasValue)
                query = query.Where(s => s.SaleDate >= startDate);

            if (endDate.HasValue)
                query = query.Where(s => s.SaleDate <= endDate);

            if (minAmount.HasValue)
                query = query.Where(s => s.TotalAmount >= minAmount);

            if (maxAmount.HasValue)
                query = query.Where(s => s.TotalAmount <= maxAmount);

            return await query.OrderByDescending(s => s.SaleDate).ToListAsync();
        }
        #endregion

        #region GetSalesByUser
        [HttpGet("by-user/{userId}")]
        public async Task<ActionResult<IEnumerable<Sale>>> GetSalesByUser(int userId)
        {
            var sales = await context.Sales
                .Include(s => s.User)
                .Include(s => s.SaleDetails)
                    .ThenInclude(sd => sd.Product)
                .Where(s => s.UserId == userId)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            if (sales == null || sales.Count == 0)
                return NotFound($"No sales found for UserId = {userId}");

            return Ok(sales);
        }
        #endregion

        #region GetRecentSales
        [HttpGet("recent")]
        public async Task<ActionResult<IEnumerable<Sale>>> GetRecentSales([FromQuery] int days = 7)
        {
            var startDate = DateTime.Now.AddDays(-days);
            var sales = await context.Sales
                .Include(s => s.User)
                .Include(s => s.SaleDetails)
                    .ThenInclude(sd => sd.Product)
                .Where(s => s.SaleDate >= startDate)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            return Ok(sales);
        }
        #endregion

        #region GetDailySales
        [HttpGet("daily")]
        public async Task<ActionResult<object>> GetDailySales([FromQuery] DateTime? date = null)
        {
            var targetDate = date ?? DateTime.Today;
            var nextDay = targetDate.AddDays(1);

            var sales = await context.Sales
                .Include(s => s.User)
                .Include(s => s.SaleDetails)
                    .ThenInclude(sd => sd.Product)
                .Where(s => s.SaleDate >= targetDate && s.SaleDate < nextDay)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            var dailySummary = new
            {
                Date = targetDate,
                TotalSales = sales.Count,
                TotalAmount = sales.Sum(s => s.TotalAmount ?? 0),
                TotalNetAmount = sales.Sum(s => s.NetAmount ?? 0),
                Sales = sales
            };

            return Ok(dailySummary);
        }
        #endregion

        #region GetSalesByDateRange
        [HttpGet("by-date-range")]
        public async Task<ActionResult<IEnumerable<Sale>>> GetSalesByDateRange(
            [FromQuery] DateTime startDate,
            [FromQuery] DateTime endDate)
        {
            var sales = await context.Sales
                .Include(s => s.User)
                .Include(s => s.SaleDetails)
                    .ThenInclude(sd => sd.Product)
                .Where(s => s.SaleDate >= startDate && s.SaleDate <= endDate)
                .OrderByDescending(s => s.SaleDate)
                .ToListAsync();

            return Ok(sales);
        }
        #endregion

        #region GetSaleSummary
        [HttpGet("summary")]
        public async Task<ActionResult<object>> GetSaleSummary(
            [FromQuery] DateTime? startDate,
            [FromQuery] DateTime? endDate,
            [FromQuery] int? userId)
        {
            var query = context.Sales.AsQueryable();

            if (startDate.HasValue)
                query = query.Where(s => s.SaleDate >= startDate);

            if (endDate.HasValue)
                query = query.Where(s => s.SaleDate <= endDate);

            if (userId.HasValue)
                query = query.Where(s => s.UserId == userId);

            var summary = new
            {
                TotalSales = await query.CountAsync(),
                TotalAmount = await query.SumAsync(s => s.TotalAmount ?? 0),
                TotalNetAmount = await query.SumAsync(s => s.NetAmount ?? 0),
                TotalDiscount = await query.SumAsync(s => s.Discount ?? 0),
                TotalTax = await query.SumAsync(s => s.Tax ?? 0),
                AverageAmount = await query.AverageAsync(s => s.TotalAmount ?? 0),
                AverageNetAmount = await query.AverageAsync(s => s.NetAmount ?? 0),
                StartDate = startDate,
                EndDate = endDate,
                UserId = userId
            };

            return Ok(summary);
        }
        #endregion

        #region Dropdown APIs

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