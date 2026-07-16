using FitCore.Shared.DTOs.Products;

namespace FitCore.BLL.Interfaces.Category
{
    public interface ICategoryService
    {
        Task<IEnumerable<AdminCategoryDTO>> GetAllCategoriesAsync();
        Task<AdminCategoryDTO?> GetCategoryByIdAsync(int id);
        Task<AdminCategoryDTO> CreateCategoryAsync(CreateCategoryDTO dto);
        Task<AdminCategoryDTO> UpdateCategoryAsync(int id, UpdateCategoryDTO dto);
        Task DeleteCategoryAsync(int id);
    }
}
