using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace StockConsume.Models
{
    public class SupplierModel
    {
        public int SupplierId { get; set; }
        public string? SupplierName { get; set; }
        public string? Contact { get; set; }
        public string? Address { get; set; }
        public int? UserId { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? ModifiedAt { get; set; }
        
        // Navigation properties
        public UserModel? User { get; set; }
        
        // For dropdowns
        public List<SelectListItem>? UserList { get; set; }
    }
}
