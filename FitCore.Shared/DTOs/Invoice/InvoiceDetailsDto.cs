using FitCore.Shared.Enums;

public class InvoiceDetailsDto
{
    public int InvoiceId { get; set; }

    public DateTime IssueDate { get; set; }

    public DateTime? DueDate { get; set; }

    public decimal SubTotal { get; set; }

    public decimal DiscountAmount { get; set; }

    public decimal TaxAmount { get; set; }

    public decimal TotalAmount { get; set; }

    public InvoiceStatus InvoiceStatus { get; set; }

    public string Description { get; set; } = string.Empty;

    public List<InvoiceItemDto> Items { get; set; } = new();

    public List<PaymentDto> Payments { get; set; } = new();
}