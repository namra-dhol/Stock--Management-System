using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace StockConsume.Models
{
    public class ProductModel
    {
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public int? CategoryId { get; set; }
        public int? SupplierId { get; set; }
        public string? Unit { get; set; }
        public decimal? CostPrice { get; set; }
        public decimal? SellingPrice { get; set; }
        public int? StockLevel { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
        
        // Navigation properties
        public CategoryModel? Category { get; set; }
        public SupplierModel? Supplier { get; set; }
        
        // For dropdowns
        public List<SelectListItem>? CategoryList { get; set; }
        public List<SelectListItem>? SupplierList { get; set; }
    }
}
