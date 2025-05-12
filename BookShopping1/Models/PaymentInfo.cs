namespace BookShopping1.Models
{
    public class PaymentInfo
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public int BookId { get; set; }
        public double Amount { get; set; }
        public DateTime PaymentDate { get; set; }
        public bool IsBorrow { get; set; }
    }
}
