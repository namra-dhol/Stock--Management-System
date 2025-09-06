using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace StockApi.Models;

public partial class Purchase
{
    public int PurchaseId { get; set; }

    public int? SupplierId { get; set; }

    public int? UserId { get; set; }

    public DateTime? PurchaseDate { get; set; }

    public decimal? TotalAmount { get; set; }

    [JsonIgnore]
    public virtual ICollection<PurchaseDetail> PurchaseDetails { get; set; } = new List<PurchaseDetail>();

    [JsonIgnore]
    public virtual Supplier? Supplier { get; set; }

    [JsonIgnore]
    public virtual User? User { get; set; }
}
