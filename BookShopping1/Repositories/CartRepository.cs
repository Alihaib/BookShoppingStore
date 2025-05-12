using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using BookShopping1.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace BookShopping1.Repositories
{
    public class CartRepository : ICartRepository
    {
        private readonly ApplicationDbContext _db;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public CartRepository(ApplicationDbContext db, IHttpContextAccessor httpContextAccessor, UserManager<IdentityUser> userManager)
        {
            _db = db;
            _userManager = userManager;
            _httpContextAccessor = httpContextAccessor;
        }


        public async Task<int> AddItemToCart(string userId, int bookId, int qty)
        {
            var strategy = _db.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _db.Database.BeginTransactionAsync();
                try
                {
                    if (string.IsNullOrEmpty(userId))
                        throw new Exception("User is not logged-in");

                    var cart = await GetCart(userId);
                    if (cart == null)
                    {
                        cart = new ShoppingCart { UserId = userId };
                        _db.ShoppingCarts.Add(cart);
                        await _db.SaveChangesAsync();
                    }

                    var cartItem = await _db.CartDetails.FirstOrDefaultAsync(a => a.ShoppingCartId == cart.Id && a.BookId == bookId);
                    if (cartItem != null)
                    {
                        cartItem.Quantity += qty;
                    }
                    else
                    {
                        var book = await _db.Books.FindAsync(bookId);
                        if (book == null)
                            throw new Exception("Book not found");

                        cartItem = new CartDetail
                        {
                            BookId = bookId,
                            ShoppingCartId = cart.Id,
                            Quantity = qty,
                            UnitPrice = book.Price
                        };
                        _db.CartDetails.Add(cartItem);
                    }

                    await _db.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    // Log the exception (ex)
                    throw;
                }

                var cartItemCount = await GetCartItemCount(userId);
                return cartItemCount;
            });
        }

        public async Task<int> RemoveItem(string userId, int bookId)
        {
            try
            {
                if (string.IsNullOrEmpty(userId))
                    throw new UnauthorizedAccessException("User is not logged-in");

                var cart = await GetCart(userId);
                if (cart == null)
                    throw new InvalidOperationException("Invalid cart");

                var cartItem = _db.CartDetails.FirstOrDefault(a => a.ShoppingCartId == cart.Id && a.BookId == bookId);
                if (cartItem == null)
                    throw new InvalidOperationException("No items in cart");

                if (cartItem.Quantity == 1)
                {
                    _db.CartDetails.Remove(cartItem);
                }
                else
                {
                    cartItem.Quantity -= 1;
                }

                await _db.SaveChangesAsync();
            }
            catch
            {
                // Handle exception (log or rethrow if necessary)
            }
            var cartItemCount = await GetCartItemCount(userId);
            return cartItemCount;
        }

        public async Task<ShoppingCart> GetUserCart(string userId)
        {
            if (string.IsNullOrEmpty(userId))
                throw new InvalidOperationException("Invalid user ID");

            var shoppingCart = await _db.ShoppingCarts
                .Include(a => a.CartDetails)
                .ThenInclude(a => a.Book)
                .Include(a => a.CartDetails)
                .ThenInclude(a => a.Book)
                .ThenInclude(a => a.Genre)
                .FirstOrDefaultAsync(a => a.UserId == userId);

            return shoppingCart;
        }

        public async Task<ShoppingCart> GetCart(string userId)
        {
            return await _db.ShoppingCarts.FirstOrDefaultAsync(x => x.UserId == userId);
        }

        public async Task<Book> GetBookById(int bookId)
        {
            return await _db.Books.FindAsync(bookId);
        }

        public async Task<int> GetCartItemCount(string userId)
        {
            var data = await (from cart in _db.ShoppingCarts
                              join cartDetail in _db.CartDetails
                              on cart.Id equals cartDetail.ShoppingCartId
                              where cart.UserId == userId
                              select cartDetail.Id).ToListAsync();
            return data.Count;
        }

        public async Task<string> BuyEBook(int bookId, string creditCardNumber)
        {
            // Implement the logic to buy an eBook using the credit card number
            // This could involve redirecting to a payment gateway like PayPal

            return "eBook purchase successful!";
        }

        public async Task<string> EnterWaitingList(int bookId)
        {
            string userId = GetUserId();
            if (string.IsNullOrEmpty(userId))
                throw new UnauthorizedAccessException("User is not logged-in");

            var book = await _db.Books.FindAsync(bookId);
            if (book == null)
                return "Book not found.";

            var waitingListEntry = new WaitingList
            {
                BookId = bookId,
                UserId = userId,
                RequestDate = DateTime.Now
            };

            _db.WaitingLists.Add(waitingListEntry);
            await _db.SaveChangesAsync();

            var waitingCount = await _db.WaitingLists.CountAsync(w => w.BookId == bookId);
            return $"You have been added to the waiting list. There are {waitingCount} people ahead of you.";
        }

        public async Task CreateCart(ShoppingCart cart)
        {
            _db.ShoppingCarts.Add(cart);
            await _db.SaveChangesAsync();
        }

        public async Task ClearCart(int cartId)
        {
            var cartItems = _db.CartDetails.Where(cd => cd.ShoppingCartId == cartId);
            _db.CartDetails.RemoveRange(cartItems);
            await _db.SaveChangesAsync();
        }

        public async Task ClearUserCart(string userId)
        {
            var cart = await GetCart(userId);
            if (cart != null)
            {
                await ClearCart(cart.Id);
            }
        }

        public async Task AddToCart(int cartId, int bookId, int qty)
        {
            var cartItem = await _db.CartDetails.FirstOrDefaultAsync(a => a.ShoppingCartId == cartId && a.BookId == bookId);
            if (cartItem != null)
            {
                cartItem.Quantity += qty;
            }
            else
            {
                var book = await _db.Books.FindAsync(bookId);
                if (book == null)
                    throw new Exception("Book not found");

                cartItem = new CartDetail
                {
                    BookId = bookId,
                    ShoppingCartId = cartId,
                    Quantity = qty,
                    UnitPrice = book.Price
                };
                _db.CartDetails.Add(cartItem);
            }

            await _db.SaveChangesAsync();
        }

        public async Task<IEnumerable<Book>> GetUserLibrary(string userId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedAccessException("User is not logged-in");
            }

            var purchasedBooks = await _db.PurchasedBooks
                .Include(pb => pb.Book)
                .Where(pb => pb.UserId == userId)
                .Select(pb => pb.Book)
                .ToListAsync();

            var borrowedBooks = await _db.Books
    .Where(b => b.BorrowedBy == userId && b.IsBorrowed == true )
    .ToListAsync();


            return purchasedBooks.Concat(borrowedBooks);
        }

        public async Task AddBookToUserLibrary(string userId, int bookId)
        {
            if (string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedAccessException("User is not logged-in");
            }

            var book = await _db.Books.FindAsync(bookId);
            if (book == null)
            {
                throw new Exception("Book not found");
            }

            var isBookAlreadyInLibrary = await _db.PurchasedBooks
                .AnyAsync(pb => pb.UserId == userId && pb.BookId == bookId);

            if (isBookAlreadyInLibrary)
            {
            
            }

            var purchasedBook = new PurchasedBook
            {
                BookId = bookId,
                UserId = userId,
                PurchaseDate = DateTime.Now
            };

            _db.PurchasedBooks.Add(purchasedBook);

            

            await _db.SaveChangesAsync();
        }

        private string GetUserId()
        {
            var principal = _httpContextAccessor.HttpContext?.User;
            return _userManager.GetUserId(principal);
        }
        public async Task<int> GetUserBorrowedBooksCount(string userId)
        {
            // Ensure userId is not null or empty
            if (string.IsNullOrEmpty(userId))
            {
                throw new UnauthorizedAccessException("User is not logged-in");
            }

            // Fetch the count of borrowed books for the user
            var borrowedBooksCount = await _db.Books
                .Where(b => b.BorrowedBy == userId && b.IsBorrowed == true)
                .CountAsync();

            return borrowedBooksCount;
        }

    }
}
