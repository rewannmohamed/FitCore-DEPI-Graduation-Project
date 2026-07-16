using FitCore.Shared.Enums;
using System;

namespace FitCore.BLL.DTOs.Payment
{
    public class PaymentDto
    {
        public int PaymentID { get; set; }
        public int InvoiceID { get; set; }
        public decimal AmountPaid { get; set; }
        public DateTime PaymentDate { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string TransactionReference { get; set; } = string.Empty;
    }
}