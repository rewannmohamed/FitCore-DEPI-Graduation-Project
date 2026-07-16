
using System;

namespace FitCore.Shared.DTOs.Products
{
    public class AdminProductDTO
    {
        public int ProductID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal CurrentSellPrice { get; set; }
        public int ReorderLevel { get; set; }
        public string? ImageUrl { get; set; }
        public int CategoryId { get; set; }
        public string CategoryName { get; set; } = string.Empty;
        public int? SupplierID { get; set; }
        public string? SupplierName { get; set; }
        public int TotalStock { get; set; }
    }
}
