namespace BookShopping1
{
    public interface IHomeRepository
    {
        Task<IEnumerable<Book>> GetBooks(string searchTitle = "", string searchAuthor = "", string searchPublisher = "", int genreId = 0, string orderBy = "", bool onSale = false, int ageLimit = 0);
        Task<IEnumerable<Genre>> Genres();
        Task<Book?> GetBookById(int id); // Add GetBookById method
    }
}