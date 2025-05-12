using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

public class BookDTO
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
    public IFormFile? ImageFile { get; set; }
    public IFormFile? EpubFile { get; set; }
    public IFormFile? F2bFile { get; set; }
    public IFormFile? MobiFile { get; set; }
    public IFormFile? PdfFile { get; set; }
    public IEnumerable<SelectListItem>? GenreList { get; set; }
    public string Description { get; set; }
    public int YearOfPublishing { get; set; }
    public int AgeLimit { get; set; }
    public bool IsOnSale { get; set; }
    public bool IsBuyOnly { get; set; }
    public double? DiscountPrice { get; set; }
    public DateTime? DiscountEndDate { get; set; }
}
