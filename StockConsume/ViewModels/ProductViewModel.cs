using Microsoft.AspNetCore.Mvc.Rendering;
using StockConsume.Models;
using System.ComponentModel.DataAnnotations;

namespace StockConsume.ViewModels
{
    public class ProductViewModel
    {
        public List<ProductModel> Products { get; set; } = new List<ProductModel>();
        public List<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Suppliers { get; set; } = new List<SelectListItem>();
        
        // Filter properties
        public int? CategoryFilter { get; set; }
        public int? SupplierFilter { get; set; }
        public decimal? MinPrice { get; set; }
        public decimal? MaxPrice { get; set; }
        public string? SearchTerm { get; set; }
        
        // Sorting
        public string SortBy { get; set; } = "ProductName";
        public string SortOrder { get; set; } = "asc";
        
        // Pagination
        public int CurrentPage { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public int TotalItems { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalItems / PageSize);
        
        public bool HasPreviousPage => CurrentPage > 1;
        public bool HasNextPage => CurrentPage < TotalPages;
    }

    public class ProductEditViewModel
    {
        public ProductModel Product { get; set; } = new ProductModel();
        public List<SelectListItem> Categories { get; set; } = new List<SelectListItem>();
        public List<SelectListItem> Suppliers { get; set; } = new List<SelectListItem>();
        
        [Display(Name = "Product Name")]
        [Required(ErrorMessage = "Product name is required")]
        public string ProductName { get; set; } = string.Empty;
        
        [Display(Name = "Category")]
        [Required(ErrorMessage = "Please select a category")]
        public int? CategoryId { get; set; }
        
        [Display(Name = "Supplier")]
        [Required(ErrorMessage = "Please select a supplier")]
        public int? SupplierId { get; set; }
        
        [Display(Name = "Unit")]
        public string? Unit { get; set; }
        
        [Display(Name = "Cost Price")]
        [Range(0, double.MaxValue, ErrorMessage = "Cost price must be a positive number")]
        public decimal? CostPrice { get; set; }
        
        [Display(Name = "Selling Price")]
        [Range(0, double.MaxValue, ErrorMessage = "Selling price must be a positive number")]
        public decimal? SellingPrice { get; set; }
        
        [Display(Name = "Stock Level")]
        [Range(0, int.MaxValue, ErrorMessage = "Stock level must be a positive number")]
        public int? StockLevel { get; set; }
    }
}
