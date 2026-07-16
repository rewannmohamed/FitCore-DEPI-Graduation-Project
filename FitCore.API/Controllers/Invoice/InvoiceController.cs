using FitCore.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FitCore.API.Controllers.Invoice
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class InvoicesController : ControllerBase
    {
        private readonly IInvoiceService _invoiceService;

        public InvoicesController(IInvoiceService invoiceService)
        {
            _invoiceService = invoiceService;
        }

        [HttpGet]
        public async Task<IActionResult> GetUserInvoices(
            int page = 1,
            int pageSize = 10)
        {
            var result =
                await _invoiceService.GetUserInvoicesAsync(page, pageSize);

            return Ok(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetInvoice(int id)
        {
            var result =
                await _invoiceService.GetUserInvoiceByIdAsync(id);

            if (result == null)
                return NotFound();

            return Ok(result);
        }

        [Authorize(Roles = nameof(UserRoles.Admin) + "," + nameof(UserRoles.Receptionist))]
        [HttpGet("admin")]
        public async Task<IActionResult> GetAllInvoices(
            int page = 1,
            int pageSize = 10)
        {
            var result =
                await _invoiceService.GetAllInvoicesAsync(page, pageSize);

            return Ok(result);
        }

        [Authorize(Roles = nameof(UserRoles.Admin) + "," + nameof(UserRoles.Receptionist))]
        [HttpGet("admin/{id}")]
        public async Task<IActionResult> GetInvoiceAdmin(int id)
        {
            var result =
                await _invoiceService.GetInvoiceByIdAsync(id);

            if (result == null)
                return NotFound();

            return Ok(result);
        }
    }
}
