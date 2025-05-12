using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BookShopping1.Models
{
    [Table("BookFormat")]
    public class BookFormat
    {
        public int Id { get; set; }

        [Required]
        public int BookId { get; set; }
        public Book Book { get; set; }

        [Required]
        [MaxLength(10)]
        public string FormatType { get; set; } 

        public string? FilePath { get; set; } 
    }
}
