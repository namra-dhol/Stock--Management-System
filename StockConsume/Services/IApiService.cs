using StockConsume.Models;

namespace StockConsume.Services
{
    public interface IApiService
    {
        // Product operations
        Task<List<ProductModel>> GetProductsAsync();
        Task<ProductModel?> GetProductByIdAsync(int id);
        Task<bool> CreateProductAsync(ProductModel product);
        Task<bool> UpdateProductAsync(ProductModel product);
        Task<bool> DeleteProductAsync(int id);

        // Category operations
        Task<List<CategoryModel>> GetCategoriesAsync();
        Task<List<CategoryModel>> GetAllCategoriesAsync();
        Task<CategoryModel?> GetCategoryByIdAsync(int id);
        Task<bool> CreateCategoryAsync(CategoryModel category);
        Task<bool> UpdateCategoryAsync(CategoryModel category);
        Task<bool> DeleteCategoryAsync(int id);

        // Supplier operations
        Task<List<SupplierModel>> GetSuppliersAsync();
        Task<List<SupplierModel>> GetAllSuppliersAsync();
        Task<SupplierModel?> GetSupplierByIdAsync(int id);
        Task<bool> CreateSupplierAsync(SupplierModel supplier);
        Task<bool> UpdateSupplierAsync(SupplierModel supplier);
        Task<bool> DeleteSupplierAsync(int id);

        // User operations
        Task<List<UserModel>> GetUsersAsync();
        Task<PaginatedUserResponse> GetPaginatedUsersAsync(int page = 1, int pageSize = 5);
        Task<UserModel?> GetUserByIdAsync(int id);
        Task<bool> CreateUserAsync(UserModel user);
        Task<bool> UpdateUserAsync(UserModel user);
        Task<bool> DeleteUserAsync(int id);

        // Purchase operations
        Task<List<PurchaseModel>> GetPurchasesAsync();
        Task<PurchaseModel?> GetPurchaseByIdAsync(int id);
        Task<bool> CreatePurchaseAsync(PurchaseModel purchase);
        Task<bool> UpdatePurchaseAsync(PurchaseModel purchase);
        Task<bool> DeletePurchaseAsync(int id);

        // Sale operations
        Task<List<SaleModel>> GetSalesAsync();
        Task<SaleModel?> GetSaleByIdAsync(int id);
        Task<bool> CreateSaleAsync(SaleModel sale);
        Task<bool> UpdateSaleAsync(SaleModel sale);
        Task<bool> DeleteSaleAsync(int id);

        // Invoice operations
        Task<List<InvoiceModel>> GetInvoicesAsync(DateTime? fromDate = null, DateTime? toDate = null, int? customerId = null);
        Task<InvoiceModel?> GetInvoiceByIdAsync(int id);

        // Dashboard operations
        Task<DashboardViewModel> GetDashboardSummaryAsync(DateTime? start = null, DateTime? end = null);
        Task<bool> UpdateInvoiceAsync(int invoiceId, InvoiceModel invoice);
        Task CreateInvoiceAsync(InvoiceModel invoice);
    }
}
