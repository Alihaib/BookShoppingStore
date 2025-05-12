using PayPal.Api;

namespace BookShopping1.Repositories
{
    public interface IPaymentRepository
    {
        Task<PaymentResult> ProcessPayment(ShoppingCart cart);
        Task<PaymentResult> ProcessPayPalPayment(ShoppingCart cart, bool isBorrow);
        Task<PaymentResult> ProcessDirectPurchase(Book book, int quantity, bool isBorrow);
        Task StorePaymentInfo(PaymentInfo paymentInfo);
        Task<IEnumerable<PaymentInfo>> GetAllPayments();
        APIContext GetApiContext();
    }

}

