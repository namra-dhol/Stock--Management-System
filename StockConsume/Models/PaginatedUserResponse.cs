namespace StockConsume.Models
{
    public class PaginatedUserResponse
    {
        public int TotalRecords { get; set; }
        public int PageSize { get; set; }
        public int CurrentPage { get; set; }
        public int TotalPages { get; set; }
        public List<UserModel> Users { get; set; } = new List<UserModel>();
    }
}
