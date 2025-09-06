using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace StockConsume.Models
{
    public class InvoiceModel
    {
        public int InvoiceId { get; set; }
        public string? InvoiceNumber { get; set; }
        public int? SaleId { get; set; }
        public int? CustomerId { get; set; }
        public DateTime? InvoiceDate { get; set; }
        public DateTime? DueDate { get; set; }
        public decimal? SubTotal { get; set; }
        public decimal? DiscountPercentage { get; set; }
        public decimal? DiscountAmount { get; set; }
        public decimal? TaxPercentage { get; set; }
        public decimal? TaxAmount { get; set; }
        public decimal? TotalAmount { get; set; }
        public string? Status { get; set; } // Pending, Paid, Overdue
        public string? Notes { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
        
        // Navigation properties
        public SaleModel? Sale { get; set; }
        public CustomerModel? Customer { get; set; }
        
        // For dropdowns
        public List<SelectListItem>? SaleList { get; set; }
        public List<SelectListItem>? CustomerList { get; set; }
        public List<SelectListItem>? StatusList { get; set; }
    }

    public class CustomerModel
    {
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }
}
