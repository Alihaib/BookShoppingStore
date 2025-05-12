using System.ComponentModel.DataAnnotations;

namespace BookShopping1.Models
{
    public class FeedBack
    {
        public int Id { get; set; }
        public string UserId { get; set; }
        public string UserName { get; set; }
        [Range(1, 5)]
        public int Rating { get; set; }
        [MaxLength(500)]
        public string Comment { get; set; }
        public DateTime Date { get; set; } = DateTime.Now;

        // Foreign key to Book
        public int BookId { get; set; }

        // Navigation property to Book
        public Book Book { get; set; }
    }

}
