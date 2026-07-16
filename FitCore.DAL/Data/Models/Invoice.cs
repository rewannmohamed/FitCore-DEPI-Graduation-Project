using FitCore.DAL.Interfaces;
using FitCore.Shared.Enums;
using System;
using System.Collections.Generic;

namespace FitCore.DAL.Data.Models
{
    public class Invoice : IAuditable, ISoftDelete
    {
        public int InvoiceID { get; set; }

        public int UserID { get; set; }
        public User User { get; set; } = null!;

        public DateTime IssueDate { get; set; }
        public DateTime? DueDate { get; set; } 
      
        public decimal SubTotal { get; set; } 
        public decimal DiscountAmount { get; set; }
        public decimal TaxAmount { get; set; } 
        public decimal TotalAmount { get; set; } 

        public InvoiceStatus InvoiceStatus { get; set; }
        public string Description { get; set; } = string.Empty;

        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }

        public ICollection<InvoiceItem> InvoiceItems { get; set; } = new List<InvoiceItem>();
        public ICollection<Payment> Payments { get; set; } = new List<Payment>();
        public ICollection<Membership> Memberships { get; set; } = new List<Membership>();
    }
}