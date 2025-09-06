using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace StockConsume.Models
{
    public class ReportFilterModel
    {
        public DateTime? StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public int? SupplierId { get; set; }
        public int? CategoryId { get; set; }
        public int? ProductId { get; set; }
        public string? ReportType { get; set; }
        public string? ExportFormat { get; set; } // CSV, Excel, PDF
        
        // For dropdowns
        public List<SelectListItem>? SupplierList { get; set; }
        public List<SelectListItem>? CategoryList { get; set; }
        public List<SelectListItem>? ProductList { get; set; }
        public List<SelectListItem>? ReportTypeList { get; set; }
        public List<SelectListItem>? ExportFormatList { get; set; }
    }

    public class InventoryReportModel
    {
        public int ProductId { get; set; }
        public string? ProductName { get; set; }
        public string? CategoryName { get; set; }
        public string? SupplierName { get; set; }
        public int? CurrentStock { get; set; }
        public decimal? CostPrice { get; set; }
        public decimal? SellingPrice { get; set; }
        public decimal? TotalValue { get; set; }
        public string? Status { get; set; } // In Stock, Low Stock, Out of Stock
    }

    public class SalesReportModel
    {
        public int SaleId { get; set; }
        public DateTime? SaleDate { get; set; }
        public string? CustomerName { get; set; }
        public decimal? TotalAmount { get; set; }
        public string? Status { get; set; }
        public int? TotalItems { get; set; }
        public string? UserName { get; set; }
    }

    public class PurchaseReportModel
    {
        public int PurchaseId { get; set; }
        public DateTime? PurchaseDate { get; set; }
        public string? SupplierName { get; set; }
        public decimal? TotalAmount { get; set; }
        public string? Status { get; set; }
        public int? TotalItems { get; set; }
        public string? UserName { get; set; }
    }

    public class ProfitLossReportModel
    {
        public DateTime? Period { get; set; }
        public decimal? TotalSales { get; set; }
        public decimal? TotalPurchases { get; set; }
        public decimal? GrossProfit { get; set; }
        public decimal? NetProfit { get; set; }
        public decimal? ProfitMargin { get; set; }
        public List<ProductProfitModel>? ProductProfits { get; set; }
    }

    public class ProductProfitModel
    {
        public string? ProductName { get; set; }
        public int? QuantitySold { get; set; }
        public decimal? Revenue { get; set; }
        public decimal? Cost { get; set; }
        public decimal? Profit { get; set; }
        public decimal? ProfitMargin { get; set; }
    }

    public class GoodsReceiptModel
    {
        public int ReceiptId { get; set; }
        public int? PurchaseId { get; set; }
        public string? ReceiptNumber { get; set; }
        public DateTime? ReceiptDate { get; set; }
        public string? SupplierName { get; set; }
        public decimal? TotalAmount { get; set; }
        public string? Status { get; set; } // Pending, Received, Partial
        public string? Notes { get; set; }
        public List<GoodsReceiptDetailModel>? Details { get; set; }
        
        // For dropdowns
        public List<SelectListItem>? PurchaseList { get; set; }
    }

    public class GoodsReceiptDetailModel
    {
        public int ReceiptDetailId { get; set; }
        public int? ReceiptId { get; set; }
        public int? ProductId { get; set; }
        public string? ProductName { get; set; }
        public int? OrderedQuantity { get; set; }
        public int? ReceivedQuantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? TotalPrice { get; set; }
        public string? Condition { get; set; } // Good, Damaged, Missing
        public string? Notes { get; set; }
    }
}
