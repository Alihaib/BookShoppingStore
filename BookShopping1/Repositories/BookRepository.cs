using Microsoft.EntityFrameworkCore;

namespace BookShopping1.Repositories
{
    public interface IBookRepository
    {
        Task AddBook(Book book);
        Task DeleteBook(Book book);
        Task<Book?> GetBookById(int id);
        Task<IEnumerable<Book>> GetBooks();
        Task UpdateBook(Book book);
        Task<IEnumerable<BookCountViewModel>> GetBooksCountAsync();
        Task<IEnumerable<Book>> GetMostPopularBooksAsync();
    }

    public class BookRepository : IBookRepository
    {
        private readonly ApplicationDbContext _context;
        public BookRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task AddBook(Book book)
        {
            _context.Books.Add(book);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateBook(Book book)
        {
            _context.Books.Update(book);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteBook(Book book)
        {
            _context.Books.Remove(book);
            await _context.SaveChangesAsync();
        }

        public async Task<Book?> GetBookById(int id) => await _context.Books.Include(b => b.Genre).FirstOrDefaultAsync(b => b.Id == id);

        public async Task<IEnumerable<Book>> GetBooks() => await _context.Books.Include(b => b.Genre).ToListAsync();

        public async Task<IEnumerable<BookCountViewModel>> GetBooksCountAsync()
        {
            return await _context.Books
                .Select(b => new BookCountViewModel
                {
                    BookId = b.Id,
                    BookName = b.BookName,
                    AuthorName = b.AuthorName,
                    Publisher = b.Publisher,
                    Count = b.CartDetail.Count
                })
                .OrderByDescending(b => b.Count)
                .ToListAsync();
        }
        public async Task<IEnumerable<Book>> GetMostPopularBooksAsync()
        {
            return await _context.Books
                .OrderByDescending(b => b.CartDetail.Count)
                .ToListAsync();
        }


    }
}