using FitCore.DAL.Interfaces;
using FitCore.Shared.Enums;
using System;

namespace FitCore.DAL.Data.Models
{
    public class Payment : ISoftDelete
    {
        public int PaymentID { get; set; }
        public int InvoiceID { get; set; }
        public Invoice Invoice { get; set; } = null!;
        public int UserId { get; set; }
        public User User { get; set; } = null!;
        public decimal AmountPaid { get; set; }
        public DateTime PaymentDate { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string TransactionReference { get; set; } = string.Empty;
        public string? GatewayResponse { get; set; }
        public bool IsDeleted { get; set; } = false;
        public DateTime? DeletedAt { get; set; }
    }
}