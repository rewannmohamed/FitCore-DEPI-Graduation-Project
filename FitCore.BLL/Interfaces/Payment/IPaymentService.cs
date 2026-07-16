using FitCore.BLL.DTOs.Payment;
using System.Threading.Tasks;

namespace FitCore.BLL.Interfaces.Payment
{
    public interface IPaymentService
    {
        /// <summary>
        /// Creates a Stripe Checkout Session for an existing invoice and returns
        /// the URL the browser should be redirected to.
        /// </summary>
        Task<CheckoutSessionResponseDto> CreateCheckoutSessionAsync(int invoiceId, string successUrl, string cancelUrl);

        /// <summary>
        /// Verifies and processes an incoming Stripe webhook event.
        /// This is the ONLY place a payment should ever be marked as confirmed.
        /// </summary>
        Task HandleStripeWebhookAsync(string json, string stripeSignatureHeader);
    }
}