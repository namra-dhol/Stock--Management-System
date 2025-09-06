namespace StockApi.DTOs
{
    public class EmailConfig
    {
        public string SmtpServer { get; set; } = string.Empty;
        public int SmtpPort { get; set; }
        public string Email { get; set; } = string.Empty;
        public string AppPassword { get; set; } = string.Empty;
    }
}
