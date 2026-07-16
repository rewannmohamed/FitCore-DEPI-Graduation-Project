using FitCore.Shared.Enums;

namespace FitCore.Shared.DTOs.Subscriptions
{
    public class CreateSubscriptionDto
    {
        public int UserId { get; set; }
        public int MemberProfileId { get; set; }
        public int GymServiceId { get; set; }
        public int DurationInDays { get; set; }
        public decimal Price { get; set; }
        public string ServiceName { get; set; } = string.Empty;
    }

    public class PaymentDto
    {
        public int InvoiceId { get; set; }
        public int UserId { get; set; }
        public decimal Amount { get; set; }
        public PaymentMethod PaymentMethod { get; set; }
        public string TransactionReference { get; set; } = string.Empty;
    }
}