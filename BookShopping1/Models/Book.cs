using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

public class Book
{
    public int Id { get; set; }
    [Required]
    [MaxLength(40)]
    public string? BookName { get; set; }
    [Required]
    [MaxLength(40)]
    public string? AuthorName { get; set; }
    [Required]
    [MaxLength(40)]
    public string? Publisher { get; set; }
    [Required]
    public double Price { get; set; }
    public double BorrowPrice { get; set; }
    public string? Imaget { get; set; }
    [Required]
    public int GenreId { get; set; }
    public Genre Genre { get; set; }
    public List<CartDetail> CartDetail { get; set; }

    [NotMapped]
    public string GenreName { get; set; }

    [NotMapped]
    public int Quantity { get; set; }
    public string? EpubFilePath { get; set; }
    public string? F2bFilePath { get; set; }
    public string? MobiFilePath { get; set; }
    public string? PdfFilePath { get; set; }
    public string Description { get; set; }
    public int YearOfPublishing { get; set; }
    public int AgeLimit { get; set; }
    public bool IsOnSale { get; set; }
    public bool IsBuyOnly { get; set; }
    public double? DiscountPrice { get; set; }
    public DateTime? DiscountEndDate { get; set; }

    // Add navigation property for feedback
    public ICollection<FeedBack> FeedBacks { get; set; }

    public bool IsBorrowed { get; set; } // Indicates if the book is currently borrowed

    public string? BorrowedBy { get; set; } // The name or ID of the person borrowing the book

    public DateTime? BorrowedDate { get; set; } // Date the book was borrowed

    public DateTime? DueDate { get; set; } // Date 

    public List<string> WaitingList { get; set; } = new List<string>();
    public int CopiesAvailable { get; set; } = 3;

    // Add TotalCopies to represent the total number of copies of the book
    public int TotalCopies { get; set; } = 3;
}
