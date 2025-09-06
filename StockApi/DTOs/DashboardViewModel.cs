using System.Collections.Generic;

namespace StockApi.DTOs
{
    public class DashboardViewModel
    {
        public int TotalProducts { get; set; }
        public int TotalCategories { get; set; }
        public int TotalSuppliers { get; set; }
        public int TotalUsers { get; set; }
        public int TotalPurchases { get; set; }
        public int TotalSales { get; set; }
        public decimal TotalPurchaseAmount { get; set; }
        public decimal TotalSaleAmount { get; set; }
        public decimal NetProfit { get; set; }
        
        public Dictionary<string, int> ProductsByCategory { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> ProductsPerDay { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> SalesPerDay { get; set; } = new Dictionary<string, int>();
        public Dictionary<string, int> PurchasesPerDay { get; set; } = new Dictionary<string, int>();
        
        public List<TopProduct> TopProducts { get; set; } = new List<TopProduct>();
        public List<TopSupplier> TopSuppliers { get; set; } = new List<TopSupplier>();
        public List<RecentActivity> RecentActivities { get; set; } = new List<RecentActivity>();
    }

    public class TopProduct
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int StockQuantity { get; set; }
        public decimal Price { get; set; }
        public string CategoryName { get; set; } = string.Empty;
    }

    public class TopSupplier
    {
        public int SupplierId { get; set; }
        public string SupplierName { get; set; } = string.Empty;
        public int PurchaseCount { get; set; }
        public decimal TotalAmount { get; set; }
    }

    public class RecentActivity
    {
        public string Type { get; set; } = string.Empty; // "Purchase", "Sale", "Product"
        public string Description { get; set; } = string.Empty;
        public DateTime Date { get; set; }
        public decimal? Amount { get; set; }
    }
}
