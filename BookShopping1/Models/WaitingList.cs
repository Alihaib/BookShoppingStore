namespace BookShopping1.Models
{
    public class WaitingList
    {
        public int Id { get; set; }
        public int BookId { get; set; }
        public string UserId { get; set; }
        public DateTime RequestDate { get; set; }
        public Book Book { get; set; }
        public DateTime RequestedAt { get; set; }
    }
}
