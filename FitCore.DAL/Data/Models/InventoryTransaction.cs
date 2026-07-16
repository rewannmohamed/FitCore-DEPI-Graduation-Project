using FitCore.DAL.Interfaces;
using FitCore.Shared.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitCore.DAL.Data.Models
{
    public class InventoryTransaction : IAuditable
    {
        public int TransactionID { get; set; }
        public int UserID { get; set; }
        public User User { get; set; } = null!;

        public TransactionType Type { get; set; }
        public DateTime TransactionDate { get; set; }

        public string ReferenceNumber { get; set; } = string.Empty;
        public string Notes { get; set; } = string.Empty;

        public ICollection<InventoryTransactionItem> TransactionItems { get; set; } = new List<InventoryTransactionItem>();
    }
}