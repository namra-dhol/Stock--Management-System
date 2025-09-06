using StockConsume.Services;

namespace StockConsume
{
    public static class ServiceConfiguration
    {
        public static void ConfigureServices(this IServiceCollection services, IConfiguration configuration)
        {
            // Add HttpClient for API calls
            services.AddHttpClient<IApiService, ApiService>(client =>
            {
                client.BaseAddress = new Uri("https://localhost:7066/api/");
                client.Timeout = TimeSpan.FromSeconds(30);
            });

            // Register API Service
            services.AddScoped<IApiService, ApiService>();

            // Add logging
            services.AddLogging(builder =>
            {
                builder.AddConsole();
                builder.AddDebug();
            });

            // Add memory cache for performance
            services.AddMemoryCache();

            // Add session support
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromMinutes(30);
                options.Cookie.HttpOnly = true;
                options.Cookie.IsEssential = true;
            });

            // Add authentication
            services.AddAuthentication("CookieAuth")
                .AddCookie("CookieAuth", options =>
                {
                    options.LoginPath = "/Login";
                    options.LogoutPath = "/Login/Logout";
                    options.ExpireTimeSpan = TimeSpan.FromHours(8);
                    options.SlidingExpiration = true;
                });

            // Add authorization
            services.AddAuthorization();

            // Add MVC with options
            services.AddControllersWithViews(options =>
            {
                // Add global filters if needed
                // options.Filters.Add<GlobalExceptionFilter>();
            });

            // Add model validation - moved to MVC builder
            // services.AddDataAnnotationsLocalization();
        }
    }
}
