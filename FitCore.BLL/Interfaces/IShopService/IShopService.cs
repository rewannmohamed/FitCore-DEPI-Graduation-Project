using FitCore.DAL.Data.Models;
using FitCore.Shared.DTOs;
using FitCore.Shared.DTOs.Cart;
using FitCore.Shared.DTOs.Products;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitCore.BLL.Interfaces.IShopService
{
    public interface IShopService
    {
        Task<IEnumerable<AdminProductDTO>> GetAllProductsAsync();
        Task<bool> AddToCartAsync(int userId, AddCartItemDTO cartItemDto);
        Task<IEnumerable<CartItemDTO>> GetUserCartAsync(int userId);
        Task<bool> RemoveFromCartAsync(int cartItemId, int userId);
        Task<bool> UpdateCartItemQuantityAsync(int cartItemId, int quantity, int userId);
        //Task<int> CheckoutAsync(int userId, CheckoutDTO checkoutDto);

        // ---- Admin: Product catalog management ----
        Task<PaginationResponseDto<AdminProductDTO>> GetAdminProductsAsync(int page, int pageSize, string? searchTerm, int? categoryId);
        Task<AdminProductDTO?> GetProductByIdAsync(int productId);
        Task<AdminProductDTO> CreateProductAsync(CreateProductDTO dto);
        Task<AdminProductDTO> UpdateProductAsync(int productId, UpdateProductDTO dto);
        Task DeleteProductAsync(int productId);

        // ---- Admin: Lookups ----
        Task<IEnumerable<CategoryDTO>> GetCategoriesAsync();
        Task<IEnumerable<SupplierDTO>> GetSuppliersAsync();

        // ---- Admin: Inventory ----
        Task<IEnumerable<InventoryDTO>> GetInventoryAsync(int? productId);
        Task<InventoryDTO> AddInventoryAsync(AddInventoryDTO dto);
        public Task<bool> RemoveInventory(Cart cart);
    }
}
