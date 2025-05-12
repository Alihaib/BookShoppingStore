using BookShopping1;
using BookShopping1.Models;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;

namespace BookShopping1.Repositories
{
    public class HomeRepository : IHomeRepository
    {
        private readonly ApplicationDbContext _db;
       

        public HomeRepository(ApplicationDbContext db)
        {
            _db = db;
        }

        public async Task<IEnumerable<Genre>> Genres()
        {
            return await _db.Genres.ToListAsync();
        }

        public async Task<IEnumerable<Book>> GetBooks(
       string searchTitle = "",
       string searchAuthor = "",
       string searchPublisher = "",
       int genreId = 0,
       string orderBy = "",
       bool onSale = false,
       int ageLimit = 0)
        {
            searchTitle = searchTitle?.ToLower();
            searchAuthor = searchAuthor?.ToLower();
            searchPublisher = searchPublisher?.ToLower();

            var query = from book in _db.Books
                        join genre in _db.Genres on book.GenreId equals genre.Id
                        select new Book
                        {
                            Id = book.Id,
                            Imaget = book.Imaget,
                            AuthorName = book.AuthorName,
                            BookName = book.BookName,
                            Publisher = book.Publisher,
                            GenreId = book.GenreId,
                            Price = book.DiscountEndDate != null && book.DiscountEndDate > DateTime.Now
                                    ? book.DiscountPrice ?? book.Price
                                    : book.Price, // Apply discount
                            BorrowPrice = book.BorrowPrice,
                            DiscountPrice = book.DiscountPrice, 
                            DiscountEndDate = book.DiscountEndDate, 
                            GenreName = genre.GenreName,
                            Quantity = book.Quantity,
                            Description = book.Description,
                            YearOfPublishing = book.YearOfPublishing,
                            AgeLimit = book.AgeLimit,
                            IsOnSale = book.IsOnSale
                        };

            if (!string.IsNullOrWhiteSpace(searchTitle))
            {
                query = query.Where(b => b.BookName.ToLower().Contains(searchTitle));
            }

            if (!string.IsNullOrWhiteSpace(searchAuthor))
            {
                query = query.Where(b => b.AuthorName.ToLower().Contains(searchAuthor));
            }

            if (!string.IsNullOrWhiteSpace(searchPublisher))
            {
                query = query.Where(b => b.Publisher.ToLower().Contains(searchPublisher));
            }

            if (genreId > 0)
            {
                query = query.Where(b => b.GenreId == genreId);
            }

            if (onSale)
            {
                query = query.Where(b => b.IsOnSale);
            }

            if (ageLimit > 0)
            {
                query = query.Where(b => b.AgeLimit <= ageLimit);
            }

            switch (orderBy.ToLower())
            {
                case "priceasc":
                    query = query.OrderBy(b => b.Price);
                    break;
                case "pricedesc":
                    query = query.OrderByDescending(b => b.Price);
                    break;
                case "year":
                    query = query.OrderByDescending(b => b.YearOfPublishing);
                    break;
                case "genre":
                    query = query.OrderBy(b => b.GenreName);
                    break;
                    // Add more cases as needed
            }

            return await query.ToListAsync();
        }



        public async Task<Book?> GetBookById(int id)
        {
            return await _db.Books.Include(b => b.Genre).FirstOrDefaultAsync(b => b.Id == id);
        }

        

        public async Task<bool> BuyBook(int bookId, string userId)
        {
            var book = await _db.Books.FindAsync(bookId);
            if (book == null)
            {
                return false;
            }

            var purchasedBook = new PurchasedBook
            {
                BookId = bookId,
                UserId = userId,
                PurchaseDate = DateTime.Now
            };

            _db.PurchasedBooks.Add(purchasedBook);
            await _db.SaveChangesAsync();

            // Send email notification
           

            return true;
        }
    }
}