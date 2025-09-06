using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockApi.Models;
using Microsoft.AspNetCore.Authorization;

namespace StockApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    //[Authorize] // Require authentication for all endpoints
    public class InvoiceController : ControllerBase
    {
        private readonly StockContext context;

        public InvoiceController(StockContext context)
        {
            this.context = context;
        }

        #region GetAllInvoices

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Invoice>>> GetInvoices(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null,
            [FromQuery] int? customerId = null,
            [FromQuery] string? status = null)
        {
            var query = context.Invoices
                .Include(i => i.Sale)
                .Include(i => i.Customer)
                .AsQueryable();

            if (fromDate.HasValue)
                query = query.Where(i => i.InvoiceDate >= fromDate.Value);

            if (toDate.HasValue)
                query = query.Where(i => i.InvoiceDate <= toDate.Value);

            if (customerId.HasValue)
                query = query.Where(i => i.CustomerId == customerId);

            if (!string.IsNullOrEmpty(status))
                query = query.Where(i => i.Status == status);

            var invoices = await query.OrderByDescending(i => i.InvoiceDate).ToListAsync();
            return Ok(invoices);
        }

        #endregion

        #region GetInvoiceById

        [HttpGet("{id}")]
        public async Task<ActionResult<Invoice>> GetInvoiceById(int id)
        {
            var invoice = await context.Invoices
                .Include(i => i.Sale)
                .Include(i => i.Customer)
                .FirstOrDefaultAsync(i => i.InvoiceId == id);

            if (invoice == null)
                return NotFound();

            return Ok(invoice);
        }

        #endregion

        #region CreateInvoice

        [HttpPost]
        public async Task<IActionResult> CreateInvoice([FromBody] Invoice invoice)
        {
            try
            {
                // Generate invoice number if not provided
                if (string.IsNullOrEmpty(invoice.InvoiceNumber))
                {
                    invoice.InvoiceNumber = $"INV-{DateTime.Now:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..8].ToUpper()}";
                }

                // Set default values
                invoice.CreatedAt = DateTime.Now;
                invoice.ModifiedAt = DateTime.Now;

                // Set due date if not provided (default to 30 days from invoice date)
                if (!invoice.DueDate.HasValue && invoice.InvoiceDate.HasValue)
                {
                    invoice.DueDate = invoice.InvoiceDate.Value.AddDays(30);
                }

                // Calculate amounts if not provided
                if (invoice.SubTotal.HasValue)
                {
                    if (invoice.DiscountPercentage.HasValue && invoice.DiscountAmount == null)
                    {
                        invoice.DiscountAmount = invoice.SubTotal * (invoice.DiscountPercentage / 100);
                    }

                    if (invoice.TaxPercentage.HasValue && invoice.TaxAmount == null)
                    {
                        var taxableAmount = invoice.SubTotal - (invoice.DiscountAmount ?? 0);
                        invoice.TaxAmount = taxableAmount * (invoice.TaxPercentage / 100);
                    }

                    if (invoice.TotalAmount == null)
                    {
                        invoice.TotalAmount = invoice.SubTotal - (invoice.DiscountAmount ?? 0) + (invoice.TaxAmount ?? 0);
                    }
                }

                context.Invoices.Add(invoice);
                await context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetInvoiceById), new { id = invoice.InvoiceId }, invoice);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error creating invoice: {ex.Message}");
            }
        }

        #endregion

        #region UpdateInvoice

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateInvoice(int id, [FromBody] Invoice invoice)
        {
            try
            {
                if (id != invoice.InvoiceId)
                    return BadRequest("ID mismatch");

                var existingInvoice = await context.Invoices.FindAsync(id);
                if (existingInvoice == null)
                    return NotFound("Invoice not found");

                // Update fields
                existingInvoice.InvoiceNumber = invoice.InvoiceNumber;
                existingInvoice.SaleId = invoice.SaleId;
                existingInvoice.CustomerId = invoice.CustomerId;
                existingInvoice.InvoiceDate = invoice.InvoiceDate;
                existingInvoice.DueDate = invoice.DueDate;
                existingInvoice.SubTotal = invoice.SubTotal;
                existingInvoice.DiscountPercentage = invoice.DiscountPercentage;
                existingInvoice.DiscountAmount = invoice.DiscountAmount;
                existingInvoice.TaxPercentage = invoice.TaxPercentage;
                existingInvoice.TaxAmount = invoice.TaxAmount;
                existingInvoice.TotalAmount = invoice.TotalAmount;
                existingInvoice.Status = invoice.Status;
                existingInvoice.Notes = invoice.Notes;
                existingInvoice.ModifiedAt = DateTime.Now;

                context.Invoices.Update(existingInvoice);
                await context.SaveChangesAsync();

                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest($"Error updating invoice: {ex.Message}");
            }
        }

        #endregion

        #region DeleteInvoice

        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteInvoice(int id)
        {
            var invoice = await context.Invoices.FindAsync(id);
            if (invoice == null)
                return NotFound();

            context.Invoices.Remove(invoice);
            await context.SaveChangesAsync();

            return NoContent();
        }

        #endregion

        #region GetInvoiceSummary

        [HttpGet("summary")]
        public async Task<IActionResult> GetInvoiceSummary(
            [FromQuery] DateTime? fromDate = null,
            [FromQuery] DateTime? toDate = null)
        {
            try
            {
                var query = context.Invoices.AsQueryable();

                if (fromDate.HasValue)
                    query = query.Where(i => i.InvoiceDate >= fromDate.Value);

                if (toDate.HasValue)
                    query = query.Where(i => i.InvoiceDate <= toDate.Value);

                var summary = new
                {
                    TotalInvoices = await query.CountAsync(),
                    TotalAmount = await query.SumAsync(i => i.TotalAmount ?? 0),
                    PendingInvoices = await query.CountAsync(i => i.Status == "Pending"),
                    PaidInvoices = await query.CountAsync(i => i.Status == "Paid"),
                    OverdueInvoices = await query.CountAsync(i => i.Status == "Overdue"),
                    PendingAmount = await query.Where(i => i.Status == "Pending").SumAsync(i => i.TotalAmount ?? 0),
                    PaidAmount = await query.Where(i => i.Status == "Paid").SumAsync(i => i.TotalAmount ?? 0),
                    OverdueAmount = await query.Where(i => i.Status == "Overdue").SumAsync(i => i.TotalAmount ?? 0)
                };

                return Ok(summary);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        #endregion

        #region GetCustomers

        [HttpGet("customers")]
        public async Task<ActionResult<IEnumerable<Customer>>> GetCustomers()
        {
            var customers = await context.Customers
                .OrderBy(c => c.CustomerName)
                .ToListAsync();

            return Ok(customers);
        }

        #endregion

        #region CreateCustomer

        [HttpPost("customers")]
        public async Task<IActionResult> CreateCustomer([FromBody] Customer customer)
        {
            try
            {
                customer.CreatedAt = DateTime.Now;
                customer.ModifiedAt = DateTime.Now;

                context.Customers.Add(customer);
                await context.SaveChangesAsync();

                return CreatedAtAction(nameof(GetCustomers), new { id = customer.CustomerId }, customer);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error creating customer: {ex.Message}");
            }
        }

        #endregion
    }
}
