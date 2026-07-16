using System.ComponentModel.DataAnnotations;

namespace FitCore.Shared.DTOs.Products
{
    public class CreateCategoryDTO
    {
        [Required(ErrorMessage = "اسم الفئة مطلوب.")]
        [StringLength(100, ErrorMessage = "اسم الفئة يجب ألا يتجاوز 100 حرف.")]
        public string Name { get; set; } = string.Empty;
    }
}
