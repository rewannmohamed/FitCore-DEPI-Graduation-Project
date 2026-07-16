using FitCore.Shared.Enums;

public class InvoiceSummaryDto
{
    public int InvoiceId { get; set; }

    public DateTime IssueDate { get; set; }

    public decimal TotalAmount { get; set; }

    public InvoiceStatus InvoiceStatus { get; set; }

    public string Description { get; set; } = string.Empty;
}