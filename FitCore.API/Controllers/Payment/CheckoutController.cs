
using FitCore.BLL.Interfaces.Auth;
using FitCore.BLL.Interfaces.Payment;
using FitCore.BLL.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace FitCore.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CheckoutController : ControllerBase
    {

        private readonly ICheckoutService _checkoutService;
        private readonly ICurrentUserService _currentUserService;

        public CheckoutController(ICheckoutService checkoutService,ICurrentUserService currentUserService)
        {
            _checkoutService = checkoutService;
            _currentUserService = currentUserService;
        }
        //call it when press checkout then take the invoice id returned to create-checkout-session api then redirects it to his final order
        [Authorize]
        [HttpPost("process/")]
        public async Task<IActionResult> ProcessCheckout()
        {
            int userId = _currentUserService.UserId ?? throw new UnauthorizedAccessException();
            var invoiceId = await _checkoutService.ProcessCheckoutAsync(userId);

            if (invoiceId != null)
            {
                return Ok(new
                {
                    message = "Invoice created successfully!",
                    invoiceId = invoiceId
                });
            }

            return BadRequest(new { message = "Operation failed. The cart may be empty or the provided data is invalid." });
        }
    }
}