using System.ComponentModel.DataAnnotations;

namespace StockConsume.Models
{
    public class PurchaseDetailModel
    {
        public int PurchaseDetailId { get; set; }
        public int? PurchaseId { get; set; }
        public int? ProductId { get; set; }
        public int? Quantity { get; set; }
        public decimal? UnitPrice { get; set; }
        public decimal? TotalPrice { get; set; }
        public string? ProductName { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
    }
}
