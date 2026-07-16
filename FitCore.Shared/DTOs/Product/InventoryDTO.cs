
using System;

namespace FitCore.Shared.DTOs.Products
{
    public class InventoryDTO
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal CostPrice { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime? ExpiryDate { get; set; }
    }
}
