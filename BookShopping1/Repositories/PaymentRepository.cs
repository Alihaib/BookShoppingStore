using PayPal.Api;
using PayPal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

public class PaymentRepository : IPaymentRepository
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<PaymentRepository> _logger;
    private readonly ApplicationDbContext _context;

    public PaymentRepository(IConfiguration configuration, ILogger<PaymentRepository> logger, ApplicationDbContext context)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<PaymentResult> ProcessPayment(ShoppingCart cart)
    {
        if (cart == null) throw new ArgumentNullException(nameof(cart));

        // Implement other payment processing logic here
        return new PaymentResult { IsSuccess = true };
    }

    public async Task<PaymentResult> ProcessPayPalPayment(ShoppingCart cart, bool isBorrow)
    {
        if (cart == null) throw new ArgumentNullException(nameof(cart));

        // Calculate the total amount with possible discount for each book
        var totalAmount = cart.CartDetails.Sum(cd =>
        {
            var bookPrice = isBorrow ? cd.Book.BorrowPrice : cd.Book.Price;

            // Check if the book has a discount and is still valid
            if (cd.Book.DiscountPrice.HasValue && cd.Book.DiscountEndDate.HasValue && cd.Book.DiscountEndDate.Value > DateTime.Now)
            {
                bookPrice = cd.Book.DiscountPrice.Value; // Apply discount price
            }

            return bookPrice * cd.Quantity;
        });

        if (totalAmount == 0)
        {
            // If the total amount is zero, return a successful payment result
            return new PaymentResult
            {
                IsSuccess = true,
                RedirectUrl = "/Cart/PaymentSuccess" // Redirect to the payment success page
            };
        }

        var apiContext = GetApiContext();

        var payment = new Payment
        {
            intent = "sale",
            payer = new Payer { payment_method = "paypal" },
            transactions = new List<Transaction>
        {
            new Transaction
            {
                description = isBorrow ? "Book borrow" : "Book purchase",
                invoice_number = Guid.NewGuid().ToString(),
                amount = new Amount
                {
                    currency = "USD",
                    total = totalAmount.ToString("F2")
                },
                item_list = new ItemList
                {
                    items = cart.CartDetails.Select(cd =>
                    {
                        // Determine the final price considering any discounts
                        var itemPrice = isBorrow ? cd.Book.BorrowPrice : cd.Book.Price;
                        if (cd.Book.DiscountPrice.HasValue && cd.Book.DiscountEndDate.HasValue && cd.Book.DiscountEndDate.Value > DateTime.Now)
                        {
                            itemPrice = cd.Book.DiscountPrice.Value; // Apply discount price
                        }

                        return new Item
                        {
                            name = cd.Book.BookName,
                            currency = "USD",
                            price = itemPrice.ToString("F2"),
                            quantity = cd.Quantity.ToString(),
                            sku = cd.Book.Id.ToString()
                        };
                    }).ToList()
                }
            }
        },
            redirect_urls = new RedirectUrls
            {
                return_url = _configuration["PayPal:ReturnUrl"] ?? throw new ArgumentNullException("PayPal:ReturnUrl"),
                cancel_url = _configuration["PayPal:CancelUrl"] ?? throw new ArgumentNullException("PayPal:CancelUrl")
            }
        };

        try
        {
            var createdPayment = await Task.Run(() => payment.Create(apiContext));
            var approvalUrl = createdPayment.links.FirstOrDefault(link => link.rel == "approval_url")?.href;

            return new PaymentResult
            {
                IsSuccess = true,
                RedirectUrl = approvalUrl
            };
        }
        catch (PayPalException ex)
        {
            _logger.LogError(ex, "Error creating PayPal payment");
            _logger.LogError("PayPal error message: {0}", ex.Message);
            if (ex is PaymentsException paymentsException)
            {
                _logger.LogError("PayPal error response: {0}", paymentsException.Response);
            }
            return new PaymentResult
            {
                IsSuccess = false,
                RedirectUrl = null
            };
        }
    }

    public APIContext GetApiContext()
    {
        var clientId = _configuration["PayPal:clientId"] ?? throw new ArgumentNullException("PayPal:clientId");
        var clientSecret = _configuration["PayPal:clientSecret"] ?? throw new ArgumentNullException("PayPal:clientSecret");
        var config = new Dictionary<string, string>
        {
            { "mode", _configuration["PayPal:mode"] ?? throw new ArgumentNullException("PayPal:mode") }
        };
        var accessToken = new OAuthTokenCredential(clientId, clientSecret, config).GetAccessToken();
        return new APIContext(accessToken);
    }

    public async Task<PaymentResult> ProcessDirectPurchase(Book book, int quantity, bool isBorrow)
    {
        if (book == null) throw new ArgumentNullException(nameof(book));

        var cart = new ShoppingCart
        {
            UserId = "DirectPurchaseUser",
            CartDetails = new List<CartDetail>
            {
                new CartDetail
                {
                    Book = book,
                    Quantity = quantity,
                    UnitPrice = isBorrow ? book.BorrowPrice : book.Price
                }
            }
        };

        return await ProcessPayPalPayment(cart, isBorrow);
    }

    public async Task StorePaymentInfo(PaymentInfo paymentInfo)
    {
        _context.PaymentInfos.Add(paymentInfo);
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<PaymentInfo>> GetAllPayments()
    {
        return await _context.PaymentInfos.ToListAsync();
    }
}