using FitCore.DAL.Interfaces;
using FitCore.Shared.Enums;
using System;

namespace FitCore.DAL.Data.Models
{
    public class InvoiceItem : IAuditable, ISoftDelete
    {
        public int InvoiceItemID { get; set; }
        public int InvoiceID { get; set; }
        public Invoice Invoice { get; set; } = null!;
        public InvoiceItemType ItemType { get; set; }
        public int? ProductID { get; set; }
        public Product? Product { get; set; }
        public int? ServiceID { get; set; }
        public GymService? GymService { get; set; }
        public int? ClassID { get; set; }
        public Class? Class { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal SellPrice { get; set; }
        public decimal Discount { get; set; } = 0;
        public decimal LineTotal { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}