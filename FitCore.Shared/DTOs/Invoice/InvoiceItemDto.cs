using FitCore.Shared.Enums;

public class InvoiceItemDto
{
    public int InvoiceItemId { get; set; }

    public InvoiceItemType ItemType { get; set; }

    public string ItemName { get; set; } = string.Empty;

    public int Quantity { get; set; }

    public decimal SellPrice { get; set; }

    public decimal Discount { get; set; }

    public decimal LineTotal { get; set; }
}