using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using BookShopping1.Models;
using BookShopping1.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using BookShopping1.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services; // Add this using directive

namespace BookShopping1.Controllers
{
    [Authorize]
    public class CartController : Controller
    {
        private readonly ICartRepository _cartRepo;
        private readonly IPaymentRepository _paymentRepo;
        private readonly ILogger<CartController> _logger;
        private readonly string _clientId;
        private readonly string _clientSecret;
        private readonly IEmailSender _emailSender; // Add this fie ld

        public CartController(ICartRepository cartRepo, IPaymentRepository paymentRepo, ILogger<CartController> logger, IConfiguration configuration, IEmailSender emailsender)
        {
            _cartRepo = cartRepo;
            _paymentRepo = paymentRepo;
            _logger = logger;
            _clientId = configuration["PayPal:clientId"];
            _clientSecret = configuration["PayPal:clientSecret"];
            _emailSender = emailsender;
        }

        [HttpPost]
        public async Task<IActionResult> AddItem([FromBody] AddItemRequest request)
        {
            var userId = User.Identity.Name;
            var cartCount = await _cartRepo.AddItemToCart(userId, request.BookId, request.Qty);
            return Json(cartCount); // Return JSON result
        }

        public async Task<IActionResult> GetUserCart()
        {
            var userId = User.Identity.Name;
            var cart = await _cartRepo.GetUserCart(userId);
            return View(cart);
        }

        [HttpPost]
        public async Task<IActionResult> RemoveItem(int bookId)
        {
            var userId = User.Identity.Name;
            var cartItemCount = await _cartRepo.RemoveItem(userId, bookId);
            return RedirectToAction("GetUserCart");
        }

        [HttpGet]
        public async Task<IActionResult> GetTotalItemInCart()
        {
            var userId = User.Identity.Name;
            int cartItem = await _cartRepo.GetCartItemCount(userId);
            return Ok(cartItem);
        }

        [HttpPost]
        public async Task<IActionResult> Checkout()
        {
            var userId = User.Identity.Name;
            var cart = await _cartRepo.GetUserCart(userId);

            if (cart == null || !cart.CartDetails.Any())
            {
                TempData["ErrorMessage"] = "Your cart is empty.";
                return RedirectToAction("GetUserCart");
            }

            var paymentResult = await _paymentRepo.ProcessPayPalPayment(cart, false); // Pass false for isBorrow

            if (paymentResult.IsSuccess)
            {

                return Redirect(paymentResult.RedirectUrl);
            }
            else
            {
                TempData["ErrorMessage"] = "Payment failed. Please try again.";
                return RedirectToAction("GetUserCart");
            }
        }

        public async Task<IActionResult> PaymentSuccess()
        {
            TempData["SuccessMessage"] = "Payment successful!";

            // Send email notification
            var userId = User.Identity.Name;
            var userEmail = User.FindFirst("email")?.Value; // Assuming the email claim is available
            var cart = await _cartRepo.GetUserCart(userId);

            if (!string.IsNullOrEmpty(userEmail) && cart != null)
            {
                var bookDetails = cart.CartDetails.Select(cd => new
                {
                    cd.Book.BookName,
                    cd.Book.Price,
                    cd.Quantity
                }).ToList();

                var totalAmount = bookDetails.Sum(bd => bd.Price * bd.Quantity);
                var bookList = string.Join("<br>", bookDetails.Select(bd => $"{bd.BookName} - {bd.Quantity} x ${bd.Price}"));

                var subject = "Congratulations on Your Purchase!";
                var htmlMessage = $@"
<html>
<head>
  <style>
    body {{
      font-family: Arial, sans-serif;
      color: #333;
      line-height: 1.6;
    }}
    h1 {{
      color: #2c3e50;
    }}
    p {{
      font-size: 14px;
    }}
    .footer {{
      margin-top: 20px;
      font-size: 12px;
      color: #888;
    }}
  </style>
</head>
<body>
  <h1>Congratulations on Your Purchase!</h1>

  <p>Dear User,</p>

  <p>Thank you for your purchase from our eBook Library Service! We are thrilled to have you as a valued customer.</p>

  <p>Here are the details of your purchase:</p>
  <p>{bookList}</p>
  <p><strong>Total Amount: ${totalAmount}</strong></p>

  <p>We hope you enjoy your new eBook(s) and have a wonderful reading experience. If you have any questions or need assistance, please do not hesitate to contact us.</p>

  <p>Happy Reading!</p>

  <div class='footer'>
    <p>Warm Regards,</p>
    <p>The eBook Library Service Team</p>
  </div>
</body>
</html>
";
                await _emailSender.SendEmailAsync(userEmail, subject, htmlMessage);

            }

            // Clear the user's cart
            await _cartRepo.ClearUserCart(userId);

            return RedirectToAction("Index", "Home");
        }


        public IActionResult PaymentCancel()
        {
            TempData["ErrorMessage"] = "Payment was cancelled.";
            return RedirectToAction("GetUserCart");
        }
    }

    public class AddItemRequest
    {
        public int BookId { get; set; }
        public int Qty { get; set; }
    }
}