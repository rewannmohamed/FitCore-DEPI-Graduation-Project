using FitCore.DAL.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitCore.DAL.Data.Models
{
    public class InventoryTransactionItem : IAuditable
    {
        public int TransactionItemID { get; set; }

        public int TransactionID { get; set; }
        public InventoryTransaction Transaction { get; set; } = null!;


        public int? ProductID { get; set; }
        public Product? Product { get; set; }
        public string ProductName { get; set; }

        public int Quantity { get; set; }
        public decimal UnitCost { get; set; }
        public string BatchNumber { get; set; } = string.Empty;

    }
}
