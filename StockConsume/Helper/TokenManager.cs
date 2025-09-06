namespace StockConsume.Helper
{
    public static class TokenManager
    {
        public static string Token { get; set; } = string.Empty;
        public static string Role { get; set; } = string.Empty;
        public static string Email { get; set; } = string.Empty;
        public static string Username { get; set; } = string.Empty;

        public static bool IsAdmin => Role == "Admin";
    }
}
