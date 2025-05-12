namespace BookShopping1.Models
{
    public class PaymentResult
    {
        public int Id { get; set; } // Add primary key
        public bool IsSuccess { get; set; }
        public string RedirectUrl { get; set; }
    }
}
