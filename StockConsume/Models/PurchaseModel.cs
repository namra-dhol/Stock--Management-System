using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace StockConsume.Models
{
    public class PurchaseModel
    {
        public int PurchaseId { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public int? SupplierId { get; set; }
        public int? UserId { get; set; }
        public decimal? TotalAmount { get; set; }
        public string? SupplierName { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
        
        // For dropdowns
        public List<SelectListItem>? SupplierList { get; set; }
        public List<SelectListItem>? UserList { get; set; }
    }
}
