using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace StockConsume.Models
{
    public class SaleModel
    {
        public int SaleId { get; set; }
        public DateTime? SaleDate { get; set; }
        public int? UserId { get; set; }
        public decimal? TotalAmount { get; set; }
        public decimal? Discount { get; set; }
        public decimal? Tax { get; set; }
        public decimal? NetAmount { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
        
        // For dropdowns
        public List<SelectListItem>? UserList { get; set; }
    }
}
