using FitCore.DAL.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitCore.DAL.Data.Models
{
    public class Inventory : IAuditable,  ISoftDelete
    {
        public int Id { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public DateTime DateAdded { get; set; }
        public int Quantity { get; set; }
        public decimal CostPrice { get; set; }

        public int ProductId { get; set; }
        public Product Product { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}
