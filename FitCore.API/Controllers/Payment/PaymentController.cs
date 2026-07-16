using FitCore.BLL.DTOs.Payment;
using FitCore.BLL.Interfaces.Payment;
using FitCore.Shared.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;

namespace FitCore.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly IPaymentService _paymentService;

        public PaymentsController(IPaymentService paymentService)
        {
            _paymentService = paymentService;
        }

        // Called by your frontend when the user clicks "Pay now"
        [HttpPost("create-checkout-session")]
        [Authorize]
        public async Task<IActionResult> CreateCheckoutSession([FromBody] CreateCheckoutSessionRequestDto request)
        {
            try
            {

                
                var result = await _paymentService.CreateCheckoutSessionAsync(
                    request.InvoiceID,
                    successUrl: $"http://localhost:5184/html/user/payment/invoice-details.html?id={request.InvoiceID}",
                    cancelUrl: "http://localhost:5184/html/FailedPayment.html"
                );

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }

        // Called by Stripe, not by your frontend. Must accept the raw request body.
        [HttpPost("webhook")]
        [AllowAnonymous]
        public async Task<IActionResult> StripeWebhook()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            var signature = Request.Headers["Stripe-Signature"];

            try
            {
                await _paymentService.HandleStripeWebhookAsync(json, signature);
                return Ok();
            }
            catch (Stripe.StripeException)
            {
                return BadRequest();
            }
            catch (Exception e)
            {
                return StatusCode(500, new { Error = "Internal Server Error" });
            }
        }
    }
}