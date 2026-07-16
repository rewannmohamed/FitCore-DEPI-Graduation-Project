using FitCore.BLL.Interfaces.Auth;
using FitCore.DAL.Data.Contexts;
using FitCore.Shared.DTOs;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FitCore.BLL.Services.Invoices
{
    public class InvoiceService(FitCoreDbContext _context, ICurrentUserService _currentUser) : IInvoiceService
    {
        public async Task<PaginationResponseDto<InvoiceSummaryDto>> GetUserInvoicesAsync(
    int page,
    int pageSize)
        {
            var userId = _currentUser.UserId;

            var query = _context.Invoices
                .AsNoTracking()
                .Where(i => !i.IsDeleted && i.UserID == userId);

            var totalCount = await query.CountAsync();

            var invoices = await query
                .OrderByDescending(i => i.IssueDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(i => new InvoiceSummaryDto
                {
                    InvoiceId = i.InvoiceID,
                    IssueDate = i.IssueDate,
                    TotalAmount = i.TotalAmount,
                    InvoiceStatus = i.InvoiceStatus,
                    Description = i.Description
                })
                .ToListAsync();

            return new PaginationResponseDto<InvoiceSummaryDto>
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                Data = invoices
            };
        }
        public async Task<PaginationResponseDto<InvoiceSummaryDto>> GetAllInvoicesAsync(
    int page,
    int pageSize)
        {
            var query = _context.Invoices
                .AsNoTracking()
                .Where(i => !i.IsDeleted);

            var totalCount = await query.CountAsync();

            var invoices = await query
                .OrderByDescending(i => i.IssueDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(i => new InvoiceSummaryDto
                {
                    InvoiceId = i.InvoiceID,
                    IssueDate = i.IssueDate,
                    TotalAmount = i.TotalAmount,
                    InvoiceStatus = i.InvoiceStatus,
                    Description = i.Description
                })
                .ToListAsync();

            return new PaginationResponseDto<InvoiceSummaryDto>
            {
                CurrentPage = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                Data = invoices
            };
        }
        public async Task<InvoiceDetailsDto?> GetUserInvoiceByIdAsync(int invoiceId)
        {
            var userId = _currentUser.UserId;

            var invoice = await _context.Invoices
                .AsNoTracking()
                .Include(i => i.InvoiceItems)
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i =>
                    i.InvoiceID == invoiceId &&
                    i.UserID == userId &&
                    !i.IsDeleted);

            if (invoice is null)
                return null;

            return new InvoiceDetailsDto
            {
                InvoiceId = invoice.InvoiceID,
                IssueDate = invoice.IssueDate,
                DueDate = invoice.DueDate,
                SubTotal = invoice.SubTotal,
                DiscountAmount = invoice.DiscountAmount,
                TaxAmount = invoice.TaxAmount,
                TotalAmount = invoice.TotalAmount,
                InvoiceStatus = invoice.InvoiceStatus,
                Description = invoice.Description,

                Items = invoice.InvoiceItems
                    .Where(x => !x.IsDeleted)
                    .Select(item => new InvoiceItemDto
                    {
                        InvoiceItemId = item.InvoiceItemID,
                        ItemType = item.ItemType,
                        ItemName = item.ItemName,
                        Quantity = item.Quantity,
                        SellPrice = item.SellPrice,
                        Discount = item.Discount,
                        LineTotal = item.LineTotal
                    })
                    .ToList(),

                Payments = invoice.Payments
                    .Where(x => !x.IsDeleted)
                    .Select(payment => new PaymentDto
                    {
                        PaymentId = payment.PaymentID,
                        AmountPaid = payment.AmountPaid,
                        PaymentDate = payment.PaymentDate,
                        PaymentMethod = payment.PaymentMethod,
                        TransactionReference = payment.TransactionReference
                    })
                    .ToList()
            };
        }
        public async Task<InvoiceDetailsDto?> GetInvoiceByIdAsync(int invoiceId)
        {
            var invoice = await _context.Invoices
                .AsNoTracking()
                .Include(i => i.InvoiceItems)
                .Include(i => i.Payments)
                .FirstOrDefaultAsync(i =>
                    i.InvoiceID == invoiceId &&
                    !i.IsDeleted);

            if (invoice is null)
                return null;

            return new InvoiceDetailsDto
            {
                InvoiceId = invoice.InvoiceID,
                IssueDate = invoice.IssueDate,
                DueDate = invoice.DueDate,
                SubTotal = invoice.SubTotal,
                DiscountAmount = invoice.DiscountAmount,
                TaxAmount = invoice.TaxAmount,
                TotalAmount = invoice.TotalAmount,
                InvoiceStatus = invoice.InvoiceStatus,
                Description = invoice.Description,

                Items = invoice.InvoiceItems
                    .Where(x => !x.IsDeleted)
                    .Select(item => new InvoiceItemDto
                    {
                        InvoiceItemId = item.InvoiceItemID,
                        ItemType = item.ItemType,
                        ItemName = item.ItemName,
                        Quantity = item.Quantity,
                        SellPrice = item.SellPrice,
                        Discount = item.Discount,
                        LineTotal = item.LineTotal
                    })
                    .ToList(),

                Payments = invoice.Payments
                    .Where(x => !x.IsDeleted)
                    .Select(payment => new PaymentDto
                    {
                        PaymentId = payment.PaymentID,
                        AmountPaid = payment.AmountPaid,
                        PaymentDate = payment.PaymentDate,
                        PaymentMethod = payment.PaymentMethod,
                        TransactionReference = payment.TransactionReference
                    })
                    .ToList()
            };
        }
    }
}
