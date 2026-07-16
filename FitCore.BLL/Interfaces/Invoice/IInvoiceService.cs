using FitCore.Shared.DTOs;

public interface IInvoiceService
{
    Task<PaginationResponseDto<InvoiceSummaryDto>> GetUserInvoicesAsync(
        int page,
        int pageSize);

    Task<PaginationResponseDto<InvoiceSummaryDto>> GetAllInvoicesAsync(
        int page,
        int pageSize);

    Task<InvoiceDetailsDto?> GetUserInvoiceByIdAsync(int invoiceId);

    Task<InvoiceDetailsDto?> GetInvoiceByIdAsync(int invoiceId);
}