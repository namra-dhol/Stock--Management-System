using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace StockApi.Models;

public partial class Sale
{
    public int SaleId { get; set; }

    public int? UserId { get; set; }

    public DateTime? SaleDate { get; set; }

    public decimal? TotalAmount { get; set; }

    public decimal? Discount { get; set; }

    public decimal? Tax { get; set; }

    public decimal? NetAmount { get; set; }

    [JsonIgnore]
    public virtual ICollection<SaleDetail> SaleDetails { get; set; } = new List<SaleDetail>();

    [JsonIgnore]
    public virtual User? User { get; set; }
}
