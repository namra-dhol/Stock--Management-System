using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using StockConsume.Helper;
using StockConsume.Models;

namespace StockConsume.Services
{
    public class ApiService : IApiService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ApiService> _logger;
        private const string BaseUrl = "https://localhost:7066/api/";

        public ApiService(HttpClient httpClient, ILogger<ApiService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
            _httpClient.BaseAddress = new Uri(BaseUrl);
        }

        private void SetBearerToken()
        {
            if (!string.IsNullOrWhiteSpace(TokenManager.Token))
            {
                _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", TokenManager.Token);
            }
        }

        public async Task<List<ProductModel>> GetProductsAsync()
        {
            try
            {
                SetBearerToken();
                var response = await _httpClient.GetAsync("Product");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var products = JsonConvert.DeserializeObject<List<ProductModel>>(json);
                    
                    // Ensure navigation properties are populated
                    foreach (var product in products ?? new List<ProductModel>())
                    {
                        if (product.Category == null && product.CategoryId.HasValue)
                        {
                            var categories = await GetCategoriesAsync();
                            product.Category = categories.FirstOrDefault(c => c.CategoryId == product.CategoryId);
                        }
                        
                        if (product.Supplier == null && product.SupplierId.HasValue)
                        {
                            var suppliers = await GetSuppliersAsync();
                            product.Supplier = suppliers.FirstOrDefault(s => s.SupplierId == product.SupplierId);
                        }
                    }
                    
                    return products ?? new List<ProductModel>();
                }
                
                _logger.LogWarning($"Failed to get products. Status: {response.StatusCode}");
                return new List<ProductModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching products");
                return new List<ProductModel>();
            }
        }

        public async Task<ProductModel?> GetProductByIdAsync(int id)
        {
            try
            {
                SetBearerToken();
                var response = await _httpClient.GetAsync($"Product/{id}");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<ProductModel>(json);
                }
                
                _logger.LogWarning($"Product with ID {id} not found. Status: {response.StatusCode}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while fetching product with ID {id}");
                return null;
            }
        }

        public async Task<List<CategoryModel>> GetCategoriesAsync()
        {
            try
            {
                SetBearerToken();
                var response = await _httpClient.GetAsync("Product/dropdown/categories");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var categories = JsonConvert.DeserializeObject<List<dynamic>>(json);
                    
                    return categories?.Select(c => new CategoryModel
                    {
                        CategoryId = (int)c.CategoryId,
                        CategoryName = c.CategoryName?.ToString()
                    }).ToList() ?? new List<CategoryModel>();
                }
                
                _logger.LogWarning($"Failed to get categories. Status: {response.StatusCode}");
                return new List<CategoryModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching categories");
                return new List<CategoryModel>();
            }
        }

        public async Task<List<SupplierModel>> GetSuppliersAsync()
        {
            try
            {
                SetBearerToken();
                var response = await _httpClient.GetAsync("Product/dropdown/suppliers");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var suppliers = JsonConvert.DeserializeObject<List<dynamic>>(json);
                    
                    return suppliers?.Select(s => new SupplierModel
                    {
                        SupplierId = (int)s.SupplierId,
                        SupplierName = s.SupplierName?.ToString()
                    }).ToList() ?? new List<SupplierModel>();
                }
                
                _logger.LogWarning($"Failed to get suppliers. Status: {response.StatusCode}");
                return new List<SupplierModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching suppliers");
                return new List<SupplierModel>();
            }
        }

        public async Task<List<UserModel>> GetUsersAsync()
        {
            try
            {
                SetBearerToken();
                var response = await _httpClient.GetAsync("Supplier/dropdown/users");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var users = JsonConvert.DeserializeObject<List<dynamic>>(json);
                    
                    return users?.Select(u => new UserModel
                    {
                        UserId = (int)u.UserId,
                        UserName = u.UserName?.ToString()
                    }).ToList() ?? new List<UserModel>();
                }
                
                _logger.LogWarning($"Failed to get users. Status: {response.StatusCode}");
                return new List<UserModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching users");
                return new List<UserModel>();
            }
        }

        public async Task<List<InvoiceModel>> GetInvoicesAsync(DateTime? fromDate = null, DateTime? toDate = null, int? customerId = null)
        {
            try
            {
                SetBearerToken();
                var queryParams = new List<string>();
                
                if (fromDate.HasValue)
                    queryParams.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");
                if (toDate.HasValue)
                    queryParams.Add($"toDate={toDate.Value:yyyy-MM-dd}");
                if (customerId.HasValue)
                    queryParams.Add($"customerId={customerId}");
                
                var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
                var response = await _httpClient.GetAsync($"Invoice{queryString}");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<List<InvoiceModel>>(json) ?? new List<InvoiceModel>();
                }
                
                _logger.LogWarning($"Failed to get invoices. Status: {response.StatusCode}");
                return new List<InvoiceModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching invoices");
                return new List<InvoiceModel>();
            }
        }

        public async Task<InvoiceModel?> GetInvoiceByIdAsync(int id)
        {
            try
            {
                SetBearerToken();
                var response = await _httpClient.GetAsync($"Invoice/{id}");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<InvoiceModel>(json);
                }
                
                _logger.LogWarning($"Invoice with ID {id} not found. Status: {response.StatusCode}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while fetching invoice with ID {id}");
                return null;
            }
        }

        public async Task<bool> CreateProductAsync(ProductModel product)
        {
            try
            {
                SetBearerToken();
                var json = JsonConvert.SerializeObject(product);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("Product", content);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Product created successfully: {product.ProductName}");
                    return true;
                }
                
                _logger.LogWarning($"Failed to create product. Status: {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating product");
                return false;
            }
        }

        public async Task<bool> UpdateProductAsync(ProductModel product)
        {
            try
            {
                SetBearerToken();
                var json = JsonConvert.SerializeObject(product);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"Product/{product.ProductId}", content);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Product updated successfully: {product.ProductName}");
                    return true;
                }
                
                _logger.LogWarning($"Failed to update product. Status: {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating product");
                return false;
            }
        }

        public async Task<bool> DeleteProductAsync(int id)
        {
            try
            {
                SetBearerToken();
                var response = await _httpClient.DeleteAsync($"Product/{id}");
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Product deleted successfully: ID {id}");
                    return true;
                }
                
                _logger.LogWarning($"Failed to delete product. Status: {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while deleting product with ID {id}");
                return false;
            }
        }

        public async Task<DashboardViewModel> GetDashboardSummaryAsync(DateTime? start = null, DateTime? end = null)
        {
            try
            {
                SetBearerToken();
                var queryParams = new List<string>();
                
                if (start.HasValue)
                    queryParams.Add($"start={start.Value:yyyy-MM-dd}");
                if (end.HasValue)
                    queryParams.Add($"end={end.Value:yyyy-MM-dd}");
                
                var queryString = queryParams.Count > 0 ? "?" + string.Join("&", queryParams) : "";
                var response = await _httpClient.GetAsync($"Dashboard/summary{queryString}");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<DashboardViewModel>(json) ?? new DashboardViewModel();
                }
                
                _logger.LogWarning($"Failed to get dashboard summary. Status: {response.StatusCode}");
                return new DashboardViewModel();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching dashboard summary");
                return new DashboardViewModel();
            }
        }

        // Category operations
        public async Task<List<CategoryModel>> GetAllCategoriesAsync()
        {
            try
            {
                SetBearerToken();
                var response = await _httpClient.GetAsync("Category");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    var apiResponse = JsonConvert.DeserializeObject<dynamic>(json);
                    
                    // Handle both direct list and paginated response
                    if (apiResponse?.Categories != null)
                    {
                        return JsonConvert.DeserializeObject<List<CategoryModel>>(apiResponse.Categories.ToString()) ?? new List<CategoryModel>();
                    }
                    else
                    {
                        return JsonConvert.DeserializeObject<List<CategoryModel>>(json) ?? new List<CategoryModel>();
                    }
                }
                
                _logger.LogWarning($"Failed to get categories. Status: {response.StatusCode}");
                return new List<CategoryModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching categories");
                return new List<CategoryModel>();
            }
        }

        public async Task<CategoryModel?> GetCategoryByIdAsync(int id)
        {
            try
            {
                SetBearerToken();
                var response = await _httpClient.GetAsync($"Category/{id}");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<CategoryModel>(json);
                }
                
                _logger.LogWarning($"Category with ID {id} not found. Status: {response.StatusCode}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while fetching category with ID {id}");
                return null;
            }
        }

        public async Task<bool> CreateCategoryAsync(CategoryModel category)
        {
            try
            {
                SetBearerToken();
                var json = JsonConvert.SerializeObject(category);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("Category", content);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Category created successfully: {category.CategoryName}");
                    return true;
                }
                
                _logger.LogWarning($"Failed to create category. Status: {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating category");
                return false;
            }
        }

        public async Task<bool> UpdateCategoryAsync(CategoryModel category)
        {
            try
            {
                SetBearerToken();
                var json = JsonConvert.SerializeObject(category);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"Category/{category.CategoryId}", content);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Category updated successfully: {category.CategoryName}");
                    return true;
                }
                
                _logger.LogWarning($"Failed to update category. Status: {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating category");
                return false;
            }
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            try
            {
                SetBearerToken();
                var response = await _httpClient.DeleteAsync($"Category/{id}");
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Category deleted successfully: ID {id}");
                    return true;
                }
                
                _logger.LogWarning($"Failed to delete category. Status: {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while deleting category with ID {id}");
                return false;
            }
        }

        // Supplier operations
        public async Task<List<SupplierModel>> GetAllSuppliersAsync()
        {
            try
            {
                SetBearerToken();
                var response = await _httpClient.GetAsync("Supplier");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<List<SupplierModel>>(json) ?? new List<SupplierModel>();
                }
                
                _logger.LogWarning($"Failed to get suppliers. Status: {response.StatusCode}");
                return new List<SupplierModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching suppliers");
                return new List<SupplierModel>();
            }
        }

        public async Task<SupplierModel?> GetSupplierByIdAsync(int id)
        {
            try
            {
                SetBearerToken();
                var response = await _httpClient.GetAsync($"Supplier/{id}");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<SupplierModel>(json);
                }
                
                _logger.LogWarning($"Supplier with ID {id} not found. Status: {response.StatusCode}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while fetching supplier with ID {id}");
                return null;
            }
        }

        public async Task<bool> CreateSupplierAsync(SupplierModel supplier)
        {
            try
            {
                SetBearerToken();
                var json = JsonConvert.SerializeObject(supplier);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("Supplier", content);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Supplier created successfully: {supplier.SupplierName}");
                    return true;
                }
                
                _logger.LogWarning($"Failed to create supplier. Status: {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating supplier");
                return false;
            }
        }

        public async Task<bool> UpdateSupplierAsync(SupplierModel supplier)
        {
            try
            {
                SetBearerToken();
                var json = JsonConvert.SerializeObject(supplier);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"Supplier/{supplier.SupplierId}", content);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Supplier updated successfully: {supplier.SupplierName}");
                    return true;
                }
                
                _logger.LogWarning($"Failed to update supplier. Status: {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating supplier");
                return false;
            }
        }

        public async Task<bool> DeleteSupplierAsync(int id)
        {
            try
            {
                SetBearerToken();
                var response = await _httpClient.DeleteAsync($"Supplier/{id}");
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Supplier deleted successfully: ID {id}");
                    return true;
                }
                
                _logger.LogWarning($"Failed to delete supplier. Status: {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while deleting supplier with ID {id}");
                return false;
            }
        }

        // User operations
        public async Task<PaginatedUserResponse> GetPaginatedUsersAsync(int page = 1, int pageSize = 5)
        {
            try
            {
                SetBearerToken();
                var response = await _httpClient.GetAsync($"user?pageNumber={page}&pageSize={pageSize}");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<PaginatedUserResponse>(json) ?? new PaginatedUserResponse();
                }
                
                _logger.LogWarning($"Failed to get paginated users. Status: {response.StatusCode}");
                return new PaginatedUserResponse();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching paginated users");
                return new PaginatedUserResponse();
            }
        }

        public async Task<UserModel?> GetUserByIdAsync(int id)
        {
            try
            {
                SetBearerToken();
                var response = await _httpClient.GetAsync($"User/{id}");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<UserModel>(json);
                }
                
                _logger.LogWarning($"User with ID {id} not found. Status: {response.StatusCode}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while fetching user with ID {id}");
                return null;
            }
        }

        public async Task<bool> CreateUserAsync(UserModel user)
        {
            try
            {
                SetBearerToken();
                var json = JsonConvert.SerializeObject(user);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("User", content);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"User created successfully: {user.UserName}");
                    return true;
                }
                
                _logger.LogWarning($"Failed to create user. Status: {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating user");
                return false;
            }
        }

        public async Task<bool> UpdateUserAsync(UserModel user)
        {
            try
            {
                SetBearerToken();
                var json = JsonConvert.SerializeObject(user);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"User/{user.UserId}", content);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"User updated successfully: {user.UserName}");
                    return true;
                }
                
                _logger.LogWarning($"Failed to update user. Status: {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating user");
                return false;
            }
        }

        public async Task<bool> DeleteUserAsync(int id)
        {
            try
            {
                SetBearerToken();
                var response = await _httpClient.DeleteAsync($"User/{id}");
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"User deleted successfully: ID {id}");
                    return true;
                }
                
                _logger.LogWarning($"Failed to delete user. Status: {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while deleting user with ID {id}");
                return false;
            }
        }

        // Purchase operations
        public async Task<List<PurchaseModel>> GetPurchasesAsync()
        {
            try
            {
                SetBearerToken();
                var response = await _httpClient.GetAsync("Purchase");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<List<PurchaseModel>>(json) ?? new List<PurchaseModel>();
                }
                
                _logger.LogWarning($"Failed to get purchases. Status: {response.StatusCode}");
                return new List<PurchaseModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching purchases");
                return new List<PurchaseModel>();
            }
        }

        public async Task<PurchaseModel?> GetPurchaseByIdAsync(int id)
        {
            try
            {
                SetBearerToken();
                var response = await _httpClient.GetAsync($"Purchase/{id}");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<PurchaseModel>(json);
                }
                
                _logger.LogWarning($"Purchase with ID {id} not found. Status: {response.StatusCode}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while fetching purchase with ID {id}");
                return null;
            }
        }

        public async Task<bool> CreatePurchaseAsync(PurchaseModel purchase)
        {
            try
            {
                SetBearerToken();
                var json = JsonConvert.SerializeObject(purchase);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("Purchase", content);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Purchase created successfully: ID {purchase.PurchaseId}");
                    return true;
                }
                
                _logger.LogWarning($"Failed to create purchase. Status: {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating purchase");
                return false;
            }
        }

        public async Task<bool> UpdatePurchaseAsync(PurchaseModel purchase)
        {
            try
            {
                SetBearerToken();
                var json = JsonConvert.SerializeObject(purchase);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"Purchase/{purchase.PurchaseId}", content);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Purchase updated successfully: ID {purchase.PurchaseId}");
                    return true;
                }
                
                _logger.LogWarning($"Failed to update purchase. Status: {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating purchase");
                return false;
            }
        }

        public async Task<bool> DeletePurchaseAsync(int id)
        {
            try
            {
                SetBearerToken();
                var response = await _httpClient.DeleteAsync($"Purchase/{id}");
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Purchase deleted successfully: ID {id}");
                    return true;
                }
                
                _logger.LogWarning($"Failed to delete purchase. Status: {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while deleting purchase with ID {id}");
                return false;
            }
        }

        // Sale operations
        public async Task<List<SaleModel>> GetSalesAsync()
        {
            try
            {
                SetBearerToken();
                var response = await _httpClient.GetAsync("Sale");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<List<SaleModel>>(json) ?? new List<SaleModel>();
                }
                
                _logger.LogWarning($"Failed to get sales. Status: {response.StatusCode}");
                return new List<SaleModel>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while fetching sales");
                return new List<SaleModel>();
            }
        }

        public async Task<SaleModel?> GetSaleByIdAsync(int id)
        {
            try
            {
                SetBearerToken();
                var response = await _httpClient.GetAsync($"Sale/{id}");
                
                if (response.IsSuccessStatusCode)
                {
                    var json = await response.Content.ReadAsStringAsync();
                    return JsonConvert.DeserializeObject<SaleModel>(json);
                }
                
                _logger.LogWarning($"Sale with ID {id} not found. Status: {response.StatusCode}");
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while fetching sale with ID {id}");
                return null;
            }
        }

        public async Task<bool> CreateSaleAsync(SaleModel sale)
        {
            try
            {
                SetBearerToken();
                var json = JsonConvert.SerializeObject(sale);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PostAsync("Sale", content);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Sale created successfully: ID {sale.SaleId}");
                    return true;
                }
                
                _logger.LogWarning($"Failed to create sale. Status: {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while creating sale");
                return false;
            }
        }

        public async Task<bool> UpdateSaleAsync(SaleModel sale)
        {
            try
            {
                SetBearerToken();
                var json = JsonConvert.SerializeObject(sale);
                var content = new StringContent(json, Encoding.UTF8, "application/json");
                var response = await _httpClient.PutAsync($"Sale/{sale.SaleId}", content);
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Sale updated successfully: ID {sale.SaleId}");
                    return true;
                }
                
                _logger.LogWarning($"Failed to update sale. Status: {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred while updating sale");
                return false;
            }
        }

        public async Task<bool> DeleteSaleAsync(int id)
        {
            try
            {
                SetBearerToken();
                var response = await _httpClient.DeleteAsync($"Sale/{id}");
                
                if (response.IsSuccessStatusCode)
                {
                    _logger.LogInformation($"Sale deleted successfully: ID {id}");
                    return true;
                }
                
                _logger.LogWarning($"Failed to delete sale. Status: {response.StatusCode}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while deleting sale with ID {id}");
                return false;
            }
        }
    }
}
