using FitCore.BLL.Exceptions;
using FitCore.BLL.Interfaces;
using FitCore.BLL.Interfaces.IShopService;
using FitCore.DAL.Data.Contexts;
using FitCore.DAL.Data.Models;
using FitCore.Shared.DTOs;
using FitCore.Shared.DTOs.Cart;
using FitCore.Shared.DTOs.Products;
using FitCore.Shared.Enums;
using Microsoft.EntityFrameworkCore;

namespace FitCore.BLL.Services
{
    public class ShopService(FitCoreDbContext DbContext) : IShopService
    {
        public async Task<IEnumerable<AdminProductDTO>> GetAllProductsAsync()
        {
            return await DbContext.Products
                .Include(x => x.Category)
                .Include(x => x.Supplier)
                .Where(p => !p.IsDeleted)
                .Select(p => new AdminProductDTO
                {
                    ProductID = p.ProductID,
                    Name = p.Name,
                    Barcode = p.Barcode,
                    Description = p.Description,
                    CurrentSellPrice = p.CurrentSellPrice,
                    ReorderLevel = p.ReorderLevel,
                    ImageUrl = p.ImageUrl,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category != null ? p.Category.Name : string.Empty,
                    SupplierID = p.SupplierID,
                    SupplierName = p.Supplier != null ? p.Supplier.CompanyName : null,
                    TotalStock = DbContext.Set<Inventory>()
                        .Where(i => i.ProductId == p.ProductID && !i.IsDeleted)
                        .Sum(i => (int?)i.Quantity) ?? 0
                })
                .ToListAsync();
        }

        public async Task<bool> AddToCartAsync(int userId, AddCartItemDTO cartItemDto)
        {
            
            if (userId <= 0) throw new BusinessRuleException("Invalid user ID. Please log in.");

            
            var totalAvailable = await DbContext.Set<Inventory>()
                .Where(i => i.ProductId == cartItemDto.ProductID && !i.IsDeleted)
                .SumAsync(i => i.Quantity);

            if (totalAvailable < cartItemDto.Quantity)
                throw new BusinessRuleException("The requested quantity is currently not available in stock.");

           
            var cart = await DbContext.Set<Cart>()
                .FirstOrDefaultAsync(c => c.UserID == userId && !c.IsDeleted);

            if (cart == null)
            {
                cart = new Cart { UserID = userId };
                await DbContext.Set<Cart>().AddAsync(cart);
                await DbContext.SaveChangesAsync();
            }

            
            var existingItem = await DbContext.Set<CartItem>()
                .FirstOrDefaultAsync(ci => ci.CartID == cart.CartID && ci.ProductID == cartItemDto.ProductID && !ci.IsDeleted);

            if (existingItem != null)
            {
                existingItem.Quantity += cartItemDto.Quantity;
                DbContext.Set<CartItem>().Update(existingItem);
            }
            else
            {
                var cartItem = new CartItem
                {
                    CartID = cart.CartID,
                    ProductID = cartItemDto.ProductID,
                    Quantity = cartItemDto.Quantity
                };
                await DbContext.Set<CartItem>().AddAsync(cartItem);
            }

            return await DbContext.SaveChangesAsync() > 0;
        }

        public async Task<IEnumerable<CartItemDTO>> GetUserCartAsync(int userId)
        {
            return await DbContext.Set<CartItem>()
                .Include(ci => ci.Product)
                .Where(ci => ci.Cart.UserID == userId && !ci.IsDeleted)
                .Select(ci => new CartItemDTO
                {
                    CartItemID = ci.CartItemID,
                    ProductID = ci.ProductID,
                    ProductName = ci.Product.Name,
                    Quantity = ci.Quantity,
                    UnitPrice = ci.Product.CurrentSellPrice,
                    ImageUrl = ci.Product.ImageUrl
                }).ToListAsync();
        }

        public async Task<bool> RemoveFromCartAsync(int cartItemId, int userId)
        {
            var item = await DbContext.Set<CartItem>()
                .Include(ci => ci.Cart)
                .FirstOrDefaultAsync(ci => ci.CartItemID == cartItemId && ci.Cart.UserID == userId && !ci.IsDeleted);

            if (item == null) return false;

            item.IsDeleted = true;
            item.DeletedAt = DateTime.UtcNow;

            return await DbContext.SaveChangesAsync() > 0;
        }

        public async Task<bool> UpdateCartItemQuantityAsync(int cartItemId, int quantity, int userId)
        {
            var item = await DbContext.Set<CartItem>()
                .Include(ci => ci.Cart)
                .FirstOrDefaultAsync(ci => ci.CartItemID == cartItemId && ci.Cart.UserID == userId && !ci.IsDeleted);

            if (item == null || quantity <= 0) return false;

            item.Quantity = quantity;
            return await DbContext.SaveChangesAsync() > 0;
        }

        //public async Task<int> CheckoutAsync(int userId, CheckoutDTO checkoutDto)
        //{
        //    // 1. جلب السلة
        //    var cart = await DbContext.Set<Cart>()
        //        .Include(c => c.CartItems).ThenInclude(ci => ci.Product)
        //        .FirstOrDefaultAsync(c => c.UserID == userId && !c.IsDeleted);

            //if (cart == null || !cart.CartItems.Any()) throw new BusinessRuleException("The Cart is empty");

        //    // 2. إنشاء الفاتورة
        //    var invoice = new Invoice
        //    {
        //        UserID = userId,
        //        IssueDate = DateTime.UtcNow,
        //        InvoiceStatus = InvoiceStatus.Pending, // تأكد من الـ Enum عندك
        //        Description = checkoutDto.Description,
        //        TotalAmount = cart.CartItems.Sum(ci => ci.Quantity * ci.Product.CurrentSellPrice)
        //    };

        //    await DbContext.Set<Invoice>().AddAsync(invoice);
        //    await DbContext.SaveChangesAsync(); // عشان ناخد الـ InvoiceID

        //    // 3. تحويل عناصر السلة لفاتورة
        //    foreach (var item in cart.CartItems)
        //    {
        //        var invoiceItem = new InvoiceItem
        //        {
        //            InvoiceID = invoice.InvoiceID,
        //            ProductID = item.ProductID,
        //            ItemName = item.Product.Name,
        //            Quantity = item.Quantity,
        //            SellPrice = item.Product.CurrentSellPrice,
        //            LineTotal = item.Quantity * item.Product.CurrentSellPrice,
        //            ItemType = InvoiceItemType.Product // حدد النوع المناسب
        //        };
        //        await DbContext.Set<InvoiceItem>().AddAsync(invoiceItem);

        //        // 4. عمل Soft Delete لعناصر السلة بعد التحويل
        //        item.IsDeleted = true;
        //    }

        //    await DbContext.SaveChangesAsync();
        //    return invoice.InvoiceID; // نرجع رقم الفاتورة للفرونت إيند
        //}

        // =========================================================
        // Admin: Product catalog management
        // =========================================================
        public async Task<PaginationResponseDto<AdminProductDTO>> GetAdminProductsAsync(int page, int pageSize, string? searchTerm, int? categoryId)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var query = DbContext.Products
                .Include(p => p.Category)
                .Include(p => p.Supplier)
                .Where(p => !p.IsDeleted)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                query = query.Where(p => p.Name.Contains(searchTerm) || p.Barcode.Contains(searchTerm));
            }

            if (categoryId.HasValue)
            {
                query = query.Where(p => p.CategoryId == categoryId.Value);
            }

            var totalCount = await query.CountAsync();

            var products = await query
                .OrderByDescending(p => p.ProductID)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(p => new AdminProductDTO
                {
                    ProductID = p.ProductID,
                    Name = p.Name,
                    Barcode = p.Barcode,
                    Description = p.Description,
                    CurrentSellPrice = p.CurrentSellPrice,
                    ReorderLevel = p.ReorderLevel,
                    ImageUrl = p.ImageUrl,
                    CategoryId = p.CategoryId,
                    CategoryName = p.Category != null ? p.Category.Name : string.Empty,
                    SupplierID = p.SupplierID,
                    SupplierName = p.Supplier != null ? p.Supplier.CompanyName : null,
                    TotalStock = DbContext.Set<Inventory>()
                        .Where(i => i.ProductId == p.ProductID && !i.IsDeleted)
                        .Sum(i => (int?)i.Quantity) ?? 0
                })
                .ToListAsync();

            return new PaginationResponseDto<AdminProductDTO>
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                Data = products
            };
        }

        public async Task<AdminProductDTO?> GetProductByIdAsync(int productId)
        {
            var p = await DbContext.Products
                .Include(x => x.Category)
                .Include(x => x.Supplier)
                .FirstOrDefaultAsync(x => x.ProductID == productId && !x.IsDeleted);

            if (p == null) return null;

            var totalStock = await DbContext.Set<Inventory>()
                .Where(i => i.ProductId == productId && !i.IsDeleted)
                .SumAsync(i => (int?)i.Quantity) ?? 0;

            return new AdminProductDTO
            {
                ProductID = p.ProductID,
                Name = p.Name,
                Barcode = p.Barcode,
                Description = p.Description,
                CurrentSellPrice = p.CurrentSellPrice,
                ReorderLevel = p.ReorderLevel,
                ImageUrl = p.ImageUrl,
                CategoryId = p.CategoryId,
                CategoryName = p.Category != null ? p.Category.Name : string.Empty,
                SupplierID = p.SupplierID,
                SupplierName = p.Supplier != null ? p.Supplier.CompanyName : null,
                TotalStock = totalStock
            };
        }

        public async Task<AdminProductDTO> CreateProductAsync(CreateProductDTO dto)
        {
            var categoryExists = await DbContext.Categories.AnyAsync(c => c.Id == dto.CategoryId && !c.IsDeleted);
            if (!categoryExists) throw new BusinessRuleException("Category not found.");

            if (dto.SupplierID.HasValue)
            {
                var supplierExists = await DbContext.Suppliers.AnyAsync(s => s.SupplierID == dto.SupplierID.Value && !s.IsDeleted);
                if (!supplierExists) throw new BusinessRuleException("Supplier not found.");
            }

            var product = new Product
            {
                Name = dto.Name,
                Barcode = dto.Barcode,
                Description = dto.Description,
                CurrentSellPrice = dto.CurrentSellPrice,
                ReorderLevel = dto.ReorderLevel,
                ImageUrl = dto.ImageUrl,
                CategoryId = dto.CategoryId,
                SupplierID = dto.SupplierID
            };

            await DbContext.Products.AddAsync(product);
            await DbContext.SaveChangesAsync();

            return await GetProductByIdAsync(product.ProductID) ?? throw new BusinessRuleException("An error occurred while creating the product.");
        }

        public async Task<AdminProductDTO> UpdateProductAsync(int productId, UpdateProductDTO dto)
        {
            var product = await DbContext.Products.FirstOrDefaultAsync(p => p.ProductID == productId && !p.IsDeleted);
            if (product == null) throw new KeyNotFoundException("Product not found.");

            var categoryExists = await DbContext.Categories.AnyAsync(c => c.Id == dto.CategoryId && !c.IsDeleted);
            if (!categoryExists) throw new BusinessRuleException("The specified category was not found.");

            if (dto.SupplierID.HasValue)
            {
                var supplierExists = await DbContext.Suppliers.AnyAsync(s => s.SupplierID == dto.SupplierID.Value && !s.IsDeleted);
                if (!supplierExists) throw new BusinessRuleException("The specified supplier was not found.");
            }

            product.Name = dto.Name;
            product.Barcode = dto.Barcode;
            product.Description = dto.Description;
            product.CurrentSellPrice = dto.CurrentSellPrice;
            product.ReorderLevel = dto.ReorderLevel;
            product.ImageUrl = dto.ImageUrl;
            product.CategoryId = dto.CategoryId;
            product.SupplierID = dto.SupplierID;

            await DbContext.SaveChangesAsync();

            return await GetProductByIdAsync(product.ProductID) ?? throw new BusinessRuleException("An error occurred while updating the product.");
        }

        public async Task DeleteProductAsync(int productId)
        {
            var product = await DbContext.Products.FirstOrDefaultAsync(p => p.ProductID == productId && !p.IsDeleted);
            if (product == null) throw new KeyNotFoundException("Product not found.");

            product.IsDeleted = true;
            product.DeletedAt = DateTime.UtcNow;

            await DbContext.SaveChangesAsync();
        }

    
        public async Task<IEnumerable<CategoryDTO>> GetCategoriesAsync()
        {
            return await DbContext.Categories
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.Name)
                .Select(c => new CategoryDTO { Id = c.Id, Name = c.Name })
                .ToListAsync();
        }

        public async Task<IEnumerable<SupplierDTO>> GetSuppliersAsync()
        {
            return await DbContext.Suppliers
                .Where(s => !s.IsDeleted)
                .OrderBy(s => s.CompanyName)
                .Select(s => new SupplierDTO
                {
                    SupplierID = s.SupplierID,
                    CompanyName = s.CompanyName,
                    SupplierPhone = s.SupplierPhone
                })
                .ToListAsync();
        }

 
        public async Task<IEnumerable<InventoryDTO>> GetInventoryAsync(int? productId)
        {
            var query = DbContext.Set<Inventory>()
                .Include(i => i.Product)
                .Where(i => !i.IsDeleted)
                .AsQueryable();

            if (productId.HasValue)
            {
                query = query.Where(i => i.ProductId == productId.Value);
            }

            return await query
                .OrderByDescending(i => i.DateAdded)
                .Select(i => new InventoryDTO
                {
                    Id = i.Id,
                    ProductId = i.ProductId,
                    ProductName = i.Product.Name,
                    Quantity = i.Quantity,
                    CostPrice = i.CostPrice,
                    DateAdded = i.DateAdded,
                    ExpiryDate = i.ExpiryDate
                })
                .ToListAsync();
        }

        public async Task<InventoryDTO> AddInventoryAsync(AddInventoryDTO dto)
        {
            var product = await DbContext.Products.FirstOrDefaultAsync(p => p.ProductID == dto.ProductId && !p.IsDeleted);
            if (product == null) throw new KeyNotFoundException("Product not found.");

            var inventory = new Inventory
            {
                ProductId = dto.ProductId,
                Quantity = dto.Quantity,
                CostPrice = dto.CostPrice,
                ExpiryDate = dto.ExpiryDate,
                DateAdded = DateTime.UtcNow
            };

            await DbContext.Set<Inventory>().AddAsync(inventory);
            await DbContext.SaveChangesAsync();

            return new InventoryDTO
            {
                Id = inventory.Id,
                ProductId = inventory.ProductId,
                ProductName = product.Name,
                Quantity = inventory.Quantity,
                CostPrice = inventory.CostPrice,
                DateAdded = inventory.DateAdded,
                ExpiryDate = inventory.ExpiryDate
            };
        }

        public async Task<bool> RemoveInventory(Cart cart)
        {
            if (cart == null)
                throw new ArgumentNullException(nameof(cart));

            if (cart.CartItems == null || !cart.CartItems.Any())
                throw new InvalidOperationException("Cart is empty.");

            foreach (var cartItem in cart.CartItems.Where(ci => !ci.IsDeleted))
            {
                int requiredQuantity = cartItem.Quantity;

                if (requiredQuantity <= 0)
                    continue;

                var inventories = await DbContext.Inventories
                    .Where(inv => inv.ProductId == cartItem.ProductID
                               && !inv.IsDeleted
                               && inv.Quantity > 0)
                    .OrderBy(inv => inv.ExpiryDate ?? DateTime.MaxValue)
                    .ThenBy(inv => inv.DateAdded)
                    .ToListAsync();

                int totalAvailable = inventories.Sum(inv => inv.Quantity);

                if (totalAvailable < requiredQuantity)
                {
                    throw new InvalidOperationException(
                        $"Insufficient stock for product '{cartItem.Product?.Name ?? cartItem.ProductID.ToString()}'. " +
                        $"Required: {requiredQuantity}, Available: {totalAvailable}.");
                }

                int remainingToDeduct = requiredQuantity;

                foreach (var inv in inventories)
                {
                    if (remainingToDeduct <= 0)
                        break;

                    int deductFromThisBatch = Math.Min(inv.Quantity, remainingToDeduct);

                    inv.Quantity -= deductFromThisBatch;
                    remainingToDeduct -= deductFromThisBatch;

                    if (inv.Quantity == 0)
                    {
                        inv.IsDeleted = true;
                        inv.DeletedAt = DateTime.UtcNow;
                    }
                }

                if (remainingToDeduct > 0)
                {
                    throw new InvalidOperationException(
                        $"Unexpected error deducting stock for product '{cartItem.ProductID}'.");
                }
            }

            await DbContext.SaveChangesAsync();
            return true;
        }
    }
}