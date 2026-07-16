
using System.ComponentModel.DataAnnotations;

namespace FitCore.Shared.DTOs.Products
{
    public class CreateProductDTO
    {
        [Required, StringLength(150)]
        public string Name { get; set; } = string.Empty;

        public string Barcode { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Range(0, double.MaxValue)]
        public decimal CurrentSellPrice { get; set; }

        [Range(0, int.MaxValue)]
        public int ReorderLevel { get; set; }

        public string? ImageUrl { get; set; }

        [Required]
        public int CategoryId { get; set; }

        public int? SupplierID { get; set; }
    }
}
