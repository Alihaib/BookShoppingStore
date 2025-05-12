namespace BookShopping1.Models
{
    public class PurchasedBook
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public string UserId { get; set; }
        public DateTime PurchaseDate { get; set; }
        public Book Book { get; set; }
    }
}
