using Microsoft.AspNetCore.Mvc;

namespace BookShopping1.Repositories
{
    public interface ICartRepository
    {
        Task<int> AddItemToCart(string userId, int bookId, int quantity);
        Task<int> RemoveItem(string userId, int bookId);
        Task<ShoppingCart> GetUserCart(string userId);
        Task<ShoppingCart> GetCart(string userId);
        Task<int> GetCartItemCount(string userId);
        Task<string> BuyEBook(int bookId, string creditCardNumber);
        Task<string> EnterWaitingList(int bookId);
        Task CreateCart(ShoppingCart cart);
        Task ClearCart(int cartId);
        Task AddToCart(int cartId, int bookId, int qty);
        Task<Book> GetBookById(int bookId);
        Task ClearUserCart(string userId);
        Task<IEnumerable<Book>> GetUserLibrary(string userId);
        Task AddBookToUserLibrary(string userId, int bookId);
        Task<int> GetUserBorrowedBooksCount(string userId);



    }
}
