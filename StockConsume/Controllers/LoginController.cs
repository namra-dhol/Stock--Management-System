using Microsoft.AspNetCore.Mvc;
using StockConsume.Models;
using StockConsume.Helper;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

[AllowAnonymous]
public class LoginController : Controller
{
    private readonly HttpClient _client;

    public LoginController(IHttpClientFactory httpClientFactory)
    {
        _client = httpClientFactory.CreateClient();
        _client.BaseAddress = new Uri("https://localhost:7066/api/");
    }

    public IActionResult Index() => View();

    [HttpPost]
    public async Task<IActionResult> Index(LoginViewModel model)
    {
        if (!ModelState.IsValid)
            return View(model);

        try
        {
            var jsonDebug = JsonSerializer.Serialize(model);
            Console.WriteLine("Sending Login JSON: " + jsonDebug);

            var response = await _client.PostAsJsonAsync("Auth/login", model);

            var rawResponse = await response.Content.ReadAsStringAsync();
            Console.WriteLine("API Raw Response: " + rawResponse);
            Console.WriteLine("Status Code: " + response.StatusCode);

            if (response.IsSuccessStatusCode)
            {
                var result = await response.Content.ReadFromJsonAsync<LoginResponse>() ?? new LoginResponse();
                TokenManager.Token = result.Token;
                TokenManager.Role = result.Role;
                TokenManager.Username = result.Username;

                // Create claims for cookie authentication
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.Name, result.Username ?? string.Empty),
                    new Claim(ClaimTypes.Role, result.Role ?? string.Empty),
                    new Claim("JwtToken", result.Token ?? string.Empty)
                };

                var claimsIdentity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var authProperties = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                };

                await HttpContext.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(claimsIdentity),
                    authProperties);

                TempData["Success"] = "Successfully logged in!";
                return Redirect("/Home/Index");
            }

            TempData["Error"] = "Invalid username or password.";
        }
        catch (Exception ex)
        {
            Console.WriteLine("Error: " + ex.Message);
            TempData["Error"] = "An unexpected error occurred.";
        }

        return View(model);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        TokenManager.Token = string.Empty;
        TokenManager.Role = string.Empty;
        TokenManager.Username = string.Empty;
        return RedirectToAction("Index", "Login");
    }
}
