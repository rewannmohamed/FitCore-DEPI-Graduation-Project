
using System;
using System.ComponentModel.DataAnnotations;

namespace FitCore.Shared.DTOs.Products
{
    public class AddInventoryDTO
    {
        [Required]
        public int ProductId { get; set; }

        [Range(1, int.MaxValue)]
        public int Quantity { get; set; }

        [Range(0, double.MaxValue)]
        public decimal CostPrice { get; set; }

        public DateTime? ExpiryDate { get; set; }
    }
}
