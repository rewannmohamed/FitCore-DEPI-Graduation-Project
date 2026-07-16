using FitCore.Shared.Enums;

public class PaymentDto
{
    public int PaymentId { get; set; }

    public decimal AmountPaid { get; set; }

    public DateTime PaymentDate { get; set; }

    public PaymentMethod PaymentMethod { get; set; }

    public string TransactionReference { get; set; } = string.Empty;
}