using FitCore.DAL.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitCore.DAL.Data.Models
{
    public class Product : IAuditable,ISoftDelete
    {
        public int ProductID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Barcode { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal CurrentSellPrice { get; set; }
        public int ReorderLevel { get; set; }
        public string? ImageUrl { get; set; }


        public int CategoryId { get; set; }
        public Category Category { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
        public int? SupplierID { get; set; }
        public Supplier? Supplier { get; set; }
        public ICollection<Inventory> Inventories { get; set; } = new List<Inventory>();
        
        public ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();        
        public ICollection<InventoryTransactionItem> InventoryTransactionsItems { get; set; } = new List<InventoryTransactionItem>();
    }
}