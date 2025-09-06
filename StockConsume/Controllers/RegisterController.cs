using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using StockConsume.Models;
using Microsoft.AspNetCore.Authorization;

[AllowAnonymous]
public class RegisterController : Controller
{
    private readonly HttpClient _client;

    public RegisterController(IHttpClientFactory httpClientFactory)
    {
        _client = httpClientFactory.CreateClient();
        _client.BaseAddress = new Uri("https://localhost:7066/api/");
    }

    public IActionResult Index() => View();

    [HttpPost]
    public async Task<IActionResult> Index(RegisterViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            // Fixed property mapping
            var dto = new RegisterDTO
            {
                Username = model.Username,
                Email = model.Email,
                Phone = model.Phone,
                Password = model.Password,
                Address = model.Address
            };

            var jsonDebug = JsonSerializer.Serialize(dto);
            Console.WriteLine("Sending Register JSON: " + jsonDebug);

            var response = await _client.PostAsJsonAsync("Auth/register", dto);
            var rawResponse = await response.Content.ReadAsStringAsync();

            Console.WriteLine("API Raw Response: " + rawResponse);
            Console.WriteLine("Status Code: " + response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                TempData["Success"] = "Registration successful! Please log in.";
                return RedirectToAction("Index", "Login");
            }
            else
            {
                // Better error handling
                var errorMessage = "Registration failed.";

                if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
                {
                    errorMessage = rawResponse.Contains("Username already exists")
                        ? "Username already exists. Please choose a different username."
                        : $"Registration failed: {rawResponse}";
                }

                TempData["Error"] = errorMessage;
            }
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine("HTTP Exception during registration: " + ex.Message);
            TempData["Error"] = "Unable to connect to the server. Please try again later.";
        }
        catch (Exception ex)
        {
            Console.WriteLine("Exception during registration: " + ex.Message);
            TempData["Error"] = "An unexpected error occurred. Please try again.";
        }

        return View(model);
    }
}
