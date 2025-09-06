using System.ComponentModel.DataAnnotations;

namespace StockConsume.Models
{
    public class QueryDTO
    {
        [Required]
        public int ProductId { get; set; }
        
        public int? CategoryId { get; set; }
        public int? SupplierId { get; set; }
        
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }
        public string? Message { get; set; }
        
        public DateTime QueryDate { get; set; } = DateTime.Now;
    }
}
