using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace StockApi.Models;

public partial class Product
{
    public int ProductId { get; set; }

    public string ProductName { get; set; } = null!;

    public int? CategoryId { get; set; }

    public int? SupplierId { get; set; }

    public string? Unit { get; set; }

    public decimal? CostPrice { get; set; }

    public decimal? SellingPrice { get; set; }

    public int? StockLevel { get; set; }

    public virtual Category? Category { get; set; }

    [JsonIgnore]
    public virtual ICollection<PurchaseDetail> PurchaseDetails { get; set; } = new List<PurchaseDetail>();

    [JsonIgnore]
    public virtual ICollection<SaleDetail> SaleDetails { get; set; } = new List<SaleDetail>();

    public virtual Supplier? Supplier { get; set; }
}
