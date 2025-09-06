using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace StockApi.Models
{
    public partial class Invoice
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

        [JsonIgnore]
        public virtual Sale? Sale { get; set; }
        
        [JsonIgnore]
        public virtual Customer? Customer { get; set; }
    }

    public partial class Customer
    {
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }

        [JsonIgnore]
        public virtual ICollection<Invoice> Invoices { get; set; } = new List<Invoice>();
    }
}
