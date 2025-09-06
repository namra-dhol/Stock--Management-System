using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using StockApi.Models;
using StockApi.DTOs;
using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Options;

namespace StockApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class QueryController : ControllerBase
    {
        private readonly StockContext _context;
        private readonly EmailConfig _emailConfig;

        public QueryController(StockContext context, IOptions<EmailConfig> emailConfig)
        {
            _context = context;
            _emailConfig = emailConfig.Value;
        }

        [HttpGet("test")]
        public IActionResult Test()
        {
            return Ok(new { message = "Query API is working!", timestamp = DateTime.Now });
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendQuery([FromBody] QueryDTO queryDto)
        {
            try
            {
                // Log the incoming request
                Console.WriteLine($"Query request received: ProductId={queryDto.ProductId}, CategoryId={queryDto.CategoryId}, SupplierId={queryDto.SupplierId}");

                // Fetch the required data from database
                var product = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Supplier)
                    .FirstOrDefaultAsync(p => p.ProductId == queryDto.ProductId);

                if (product == null)
                {
                    var errorMsg = $"Product not found with ID: {queryDto.ProductId}";
                    Console.WriteLine(errorMsg);
                    return BadRequest(errorMsg);
                }

                Console.WriteLine($"Found product: {product.ProductName}");

                // Build email body with predefined and dynamic content
                var emailBody = $@"
                    <html>
                    <body style='font-family: Arial, sans-serif; line-height: 1.6; color: #333;'>
                        <div style='max-width: 600px; margin: 0 auto; padding: 20px; border: 1px solid #ddd; border-radius: 8px;'>
                            <h2 style='color: #2563eb; border-bottom: 2px solid #2563eb; padding-bottom: 10px;'>
                                📦 Stock Management System - Product Inquiry
                            </h2>
                            
                            <p><strong>Hi,</strong></p>
                            
                            <p>A customer has shown interest in a product from our Stock Management System. Here are the details:</p>
                            
                            <div style='background-color: #f8f9fa; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                                <h4 style='color: #2563eb; margin-top: 0;'>📦 Product Details:</h4>
                                <ul style='list-style: none; padding-left: 0;'>
                                    <li style='margin-bottom: 8px;'><strong>🏷️ Product Name:</strong> {product.ProductName}</li>
                                    <li style='margin-bottom: 8px;'><strong>📋 Category:</strong> {product.Category?.CategoryName ?? "Not specified"}</li>
                                    <li style='margin-bottom: 8px;'><strong>🏢 Supplier:</strong> {product.Supplier?.SupplierName ?? "Not specified"}</li>
                                    <li style='margin-bottom: 8px;'><strong>💰 Price:</strong> ₹{product.Price?.ToString("N2") ?? "0.00"}</li>
                                    <li style='margin-bottom: 8px;'><strong>📦 Stock Level:</strong> {product.StockLevel ?? 0} units</li>
                                    <li style='margin-bottom: 8px;'><strong>📝 Description:</strong> {product.Description ?? "No description available"}</li>
                                </ul>
                            </div>";

                // Add customer details if provided
                if (!string.IsNullOrEmpty(queryDto.CustomerName) || !string.IsNullOrEmpty(queryDto.CustomerEmail) || !string.IsNullOrEmpty(queryDto.CustomerPhone))
                {
                    emailBody += @"
                            <div style='background-color: #e3f2fd; padding: 15px; border-radius: 5px; margin: 20px 0;'>
                                <h4 style='color: #1976d2; margin-top: 0;'>👤 Customer Details:</h4>
                                <ul style='list-style: none; padding-left: 0;'>";
                    
                    if (!string.IsNullOrEmpty(queryDto.CustomerName))
                        emailBody += $"<li style='margin-bottom: 8px;'><strong>👤 Name:</strong> {queryDto.CustomerName}</li>";
                    
                    if (!string.IsNullOrEmpty(queryDto.CustomerEmail))
                        emailBody += $"<li style='margin-bottom: 8px;'><strong>📧 Email:</strong> {queryDto.CustomerEmail}</li>";
                    
                    if (!string.IsNullOrEmpty(queryDto.CustomerPhone))
                        emailBody += $"<li style='margin-bottom: 8px;'><strong>📞 Phone:</strong> {queryDto.CustomerPhone}</li>";
                    
                    emailBody += @"
                                </ul>
                            </div>";
                }

                // Add custom message if provided
                if (!string.IsNullOrEmpty(queryDto.Message))
                {
                    emailBody += $@"
                            <p><strong>Customer Message:</strong></p>
                            <p style='font-style: italic; background-color: #fff3cd; padding: 10px; border-left: 4px solid #ffc107; border-radius: 3px;'>
                                ""{queryDto.Message}""
                            </p>";
                }
                else
                {
                    emailBody += @"
                            <p><strong>Customer Message:</strong></p>
                            <p style='font-style: italic; background-color: #fff3cd; padding: 10px; border-left: 4px solid #ffc107; border-radius: 3px;'>
                                ""I am interested in this product. Please contact me regarding availability, pricing, and delivery options.""
                            </p>";
                }

                emailBody += $@"
                            <div style='margin-top: 30px; padding-top: 20px; border-top: 1px solid #ddd;'>
                                <p style='margin-bottom: 5px;'><strong>Best regards,</strong></p>
                                <p style='margin-bottom: 5px;'>Stock Management System Support</p>
                                <p style='font-size: 12px; color: #666; margin-top: 20px;'>
                                    This is an automated inquiry from the Stock Management System. 
                                    Please respond to the customer as soon as possible.
                                    <br><br>
                                    <strong>Query Details:</strong><br>
                                    Product ID: {queryDto.ProductId}<br>
                                    Query Date: {queryDto.QueryDate:yyyy-MM-dd HH:mm:ss}<br>
                                    Customer IP: {HttpContext.Connection.RemoteIpAddress}
                                </p>
                            </div>
                        </div>
                    </body>
                    </html>";

                // Send email using configuration
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("Stock Management System", _emailConfig.Email));
                message.To.Add(new MailboxAddress("Admin", _emailConfig.Email));
                message.Subject = $"Product Inquiry - {product.ProductName}";

                message.Body = new TextPart("html")
                {
                    Text = emailBody
                };

                using (var client = new SmtpClient())
                {
                    await client.ConnectAsync(_emailConfig.SmtpServer, _emailConfig.SmtpPort, SecureSocketOptions.StartTls);
                    await client.AuthenticateAsync(_emailConfig.Email, _emailConfig.AppPassword);
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }

                Console.WriteLine("Email sent successfully");
                return Ok(new { success = true, message = "Query sent successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in SendQuery: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                return StatusCode(500, new { success = false, message = $"Error sending query: {ex.Message}" });
            }
        }

        [HttpGet("product/{productId}")]
        public async Task<ActionResult<object>> GetProductForQuery(int productId)
        {
            try
            {
                var product = await _context.Products
                    .Include(p => p.Category)
                    .Include(p => p.Supplier)
                    .FirstOrDefaultAsync(p => p.ProductId == productId);

                if (product == null)
                {
                    return NotFound(new { message = "Product not found" });
                }

                var productInfo = new
                {
                    ProductId = product.ProductId,
                    ProductName = product.ProductName,
                    Description = product.Description,
                    Price = product.Price,
                    StockLevel = product.StockLevel,
                    CategoryName = product.Category?.CategoryName,
                    SupplierName = product.Supplier?.SupplierName,
                    ProductImg = product.ProductImg
                };

                return Ok(productInfo);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = $"Error retrieving product: {ex.Message}" });
            }
        }
    }
}
