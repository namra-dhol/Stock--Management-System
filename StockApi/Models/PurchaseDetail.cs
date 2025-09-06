using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace StockApi.Models;

public partial class PurchaseDetail
{
    public int PurchaseDetailId { get; set; }

    public int? PurchaseId { get; set; }

    public int? ProductId { get; set; }

    public int Quantity { get; set; }

    public decimal? UnitCost { get; set; }

    public decimal? SubTotal { get; set; }

    [JsonIgnore]
    public virtual Product? Product { get; set; }

    [JsonIgnore]
    public virtual Purchase? Purchase { get; set; }
}
