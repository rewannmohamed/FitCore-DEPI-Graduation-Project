using FitCore.BLL.Exceptions;
using FitCore.BLL.Interfaces.Category;
using FitCore.DAL.Data.Contexts;
using FitCore.Shared.DTOs.Products;
using Microsoft.EntityFrameworkCore;
using CategoryModel = FitCore.DAL.Data.Models.Category;

namespace FitCore.BLL.Services.Category
{
    public class CategoryService(FitCoreDbContext DbContext) : ICategoryService
    {
        public async Task<IEnumerable<AdminCategoryDTO>> GetAllCategoriesAsync()
        {
            return await DbContext.Categories
                .Where(c => !c.IsDeleted)
                .OrderBy(c => c.Name)
                .Select(c => new AdminCategoryDTO
                {
                    Id = c.Id,
                    Name = c.Name,
                    ProductsCount = c.Products.Count(p => !p.IsDeleted)
                })
                .ToListAsync();
        }

        public async Task<AdminCategoryDTO?> GetCategoryByIdAsync(int id)
        {
            var category = await DbContext.Categories
                .Where(c => c.Id == id && !c.IsDeleted)
                .Select(c => new AdminCategoryDTO
                {
                    Id = c.Id,
                    Name = c.Name,
                    ProductsCount = c.Products.Count(p => !p.IsDeleted)
                })
                .FirstOrDefaultAsync();

            return category;
        }

        public async Task<AdminCategoryDTO> CreateCategoryAsync(CreateCategoryDTO dto)
        {
            var name = dto.Name.Trim();

            var nameExists = await DbContext.Categories
                .AnyAsync(c => !c.IsDeleted && c.Name.ToLower() == name.ToLower());
            if (nameExists) throw new BusinessRuleException("يوجد فئة بنفس الاسم بالفعل.");

            var category = new CategoryModel
            {
                Name = name
            };

            await DbContext.Categories.AddAsync(category);
            await DbContext.SaveChangesAsync();

            return new AdminCategoryDTO
            {
                Id = category.Id,
                Name = category.Name,
                ProductsCount = 0
            };
        }

        public async Task<AdminCategoryDTO> UpdateCategoryAsync(int id, UpdateCategoryDTO dto)
        {
            var category = await DbContext.Categories
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
            if (category == null) throw new KeyNotFoundException("الفئة غير موجودة.");

            var name = dto.Name.Trim();

            var nameExists = await DbContext.Categories
                .AnyAsync(c => !c.IsDeleted && c.Id != id && c.Name.ToLower() == name.ToLower());
            if (nameExists) throw new BusinessRuleException("يوجد فئة بنفس الاسم بالفعل.");

            category.Name = name;
            await DbContext.SaveChangesAsync();

            var productsCount = await DbContext.Products.CountAsync(p => p.CategoryId == id && !p.IsDeleted);

            return new AdminCategoryDTO
            {
                Id = category.Id,
                Name = category.Name,
                ProductsCount = productsCount
            };
        }

        public async Task DeleteCategoryAsync(int id)
        {
            var category = await DbContext.Categories
                .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
            if (category == null) throw new KeyNotFoundException("الفئة غير موجودة.");

            var hasProducts = await DbContext.Products.AnyAsync(p => p.CategoryId == id && !p.IsDeleted);
            if (hasProducts) throw new BusinessRuleException("لا يمكن حذف الفئة لأنها مرتبطة بمنتجات موجودة. قم بنقل أو حذف المنتجات أولاً.");

            category.IsDeleted = true;
            category.DeletedAt = DateTime.UtcNow;

            await DbContext.SaveChangesAsync();
        }
    }
}
