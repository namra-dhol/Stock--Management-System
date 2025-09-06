using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockApi.Models;
using Microsoft.AspNetCore.Authorization;

namespace StockApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize] // Require authentication for all endpoints
    public class SupplierController : ControllerBase
    {
        private readonly StockContext context;

        public SupplierController(StockContext context)
        {
            this.context = context;
        }

        #region GetAllSuppliers

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Supplier>>> GetSuppliers()
        {
            var suppliers = await context.Suppliers
                .Include(s => s.User)
                .ToListAsync();
            return Ok(suppliers);
        }
        #endregion 

        #region GetSupplierById
        [HttpGet("{id}")]
        public async Task<ActionResult<Supplier>> GetSupplierById(int id)
        {
            var supplier = await context.Suppliers
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.SupplierId == id);
            if (supplier == null)
            {
                return NotFound();
            }
            return Ok(supplier);
        }
        #endregion

        #region InsertSupplier
        [HttpPost]
        public IActionResult InsertSupplier(Supplier supplier)
        {
            try
            {
                // Set default values
                supplier.CreatedAt = DateTime.Now;
                supplier.ModifiedAt = DateTime.Now;

                context.Suppliers.Add(supplier);
                context.SaveChanges();
                return CreatedAtAction(nameof(GetSupplierById), new { id = supplier.SupplierId }, supplier);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error creating supplier: {ex.Message}");
            }
        }

        #endregion

        #region DeleteSupplierById
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public IActionResult DeleteSupplierById(int id)
        {
            var supplier = context.Suppliers.Find(id);
            if (supplier == null)
            {
                return NotFound();
            }

            // Check if supplier has products or purchases
            var hasProducts = context.Products.Any(p => p.SupplierId == id);
            var hasPurchases = context.Purchases.Any(p => p.SupplierId == id);
            
            if (hasProducts || hasPurchases)
            {
                return BadRequest("Cannot delete supplier that has associated products or purchases.");
            }

            context.Suppliers.Remove(supplier);
            context.SaveChanges();
            return NoContent();
        }
        #endregion

        #region UpdateSupplierById

        [HttpPut("{id}")]
        public IActionResult UpdateSupplier(int id, Supplier supplier)
        {
            try
            {
                if (id != supplier.SupplierId)
                {
                    return BadRequest("ID mismatch");
                }

                var existingSupplier = context.Suppliers.Find(id);
                if (existingSupplier == null)
                {
                    return NotFound("Supplier not found");
                }

                // Update fields
                existingSupplier.SupplierName = supplier.SupplierName;
                existingSupplier.Contact = supplier.Contact;
                existingSupplier.Address = supplier.Address;
                existingSupplier.UserId = supplier.UserId;
                existingSupplier.ModifiedAt = DateTime.Now;

                context.Suppliers.Update(existingSupplier);
                context.SaveChanges();
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest($"Error updating supplier: {ex.Message}");
            }
        }

        #endregion

        #region select Top n record

        [HttpGet("top")]
        public async Task<ActionResult<IEnumerable<Supplier>>> GetTopNSuppliers([FromQuery] int n = 5)
        {
            var suppliers = await context.Suppliers
                .Include(s => s.User)
                .Take(n)
                .ToListAsync();
            return Ok(suppliers);
        }
        #endregion

        #region Filter
        [HttpGet("Filter")]
        public async Task<ActionResult<IEnumerable<Supplier>>> FilterSupplier(
            [FromQuery] int? supplierId, 
            [FromQuery] string? supplierName,
            [FromQuery] string? contact,
            [FromQuery] int? userId)
        {
            var query = context.Suppliers
                .Include(s => s.User)
                .AsQueryable();

            if (supplierId.HasValue)
                query = query.Where(s => s.SupplierId == supplierId);

            if (!string.IsNullOrEmpty(supplierName))
                query = query.Where(s => s.SupplierName!.Contains(supplierName));

            if (!string.IsNullOrEmpty(contact))
                query = query.Where(s => s.Contact!.Contains(contact));

            if (userId.HasValue)
                query = query.Where(s => s.UserId == userId);

            return await query.ToListAsync();
        }
        #endregion

        #region GetSuppliersByUser
        [HttpGet("by-user/{userId}")]
        public async Task<ActionResult<IEnumerable<Supplier>>> GetSuppliersByUser(int userId)
        {
            var suppliers = await context.Suppliers
                .Include(s => s.User)
                .Where(s => s.UserId == userId)
                .ToListAsync();

            if (suppliers == null || suppliers.Count == 0)
                return NotFound($"No suppliers found for UserId = {userId}");

            return Ok(suppliers);
        }
        #endregion

        #region SearchSuppliers
        [HttpGet("search")]
        public async Task<ActionResult<IEnumerable<Supplier>>> SearchSuppliers([FromQuery] string searchTerm)
        {
            if (string.IsNullOrEmpty(searchTerm))
            {
                return BadRequest("Search term is required.");
            }

            var suppliers = await context.Suppliers
                .Include(s => s.User)
                .Where(s => s.SupplierName!.Contains(searchTerm) || 
                           s.Contact!.Contains(searchTerm) || 
                           s.Address!.Contains(searchTerm))
                .ToListAsync();

            return Ok(suppliers);
        }
        #endregion

        #region GetSuppliersWithCounts
        [HttpGet("with-counts")]
        public async Task<ActionResult<IEnumerable<object>>> GetSuppliersWithCounts()
        {
            var suppliersWithCounts = await context.Suppliers
                .Select(s => new
                {
                    s.SupplierId,
                    s.SupplierName,
                    s.Contact,
                    s.Address,
                    s.CreatedAt,
                    s.ModifiedAt,
                    s.UserId,
                    ProductCount = s.Products.Count,
                    PurchaseCount = s.Purchases.Count
                })
                .ToListAsync();

            return Ok(suppliersWithCounts);
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