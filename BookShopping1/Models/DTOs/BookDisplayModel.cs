namespace BookShopping1.Models.DTOs
{
    public class BookDisplayModel
    {
        public IEnumerable<Book> Books { get; set; }
        public IEnumerable<Genre> Genres { get; set; }
        public string SearchTitle { get; set; }
        public string SearchAuthor { get; set; }
        public string SearchPublisher { get; set; }
        public int GenreId { get; set; }
        public string OrderBy { get; set; }
        public bool OnSale { get; set; }
        public int AgeLimit { get; set; }
        public string STerm { get; set; }
        public IEnumerable<Book> MostPopularBooks { get; set; }
        public bool MostPopular { get; set; } // Add this property for the filter

    }
}
