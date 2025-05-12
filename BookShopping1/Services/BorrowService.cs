using BookShopping1.Repositories;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.EntityFrameworkCore;
using PayPal.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookShopping1.Services
{
    public class BorrowService
    {
        private readonly ApplicationDbContext _context;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IEmailSender _emailSender;

        public BorrowService(ApplicationDbContext context, IPaymentRepository paymentRepository, IEmailSender emailSender)
        {
            _context = context;
            _paymentRepository = paymentRepository;
            _emailSender = emailSender;
        }

        // Borrow book method
        public async Task<string> BorrowBookAsync(int bookId, string borrowedBy, DateTime borrowDate, int borrowDays, string redirectUrl)
        {
            var book = await _context.Books.FindAsync(bookId);

            if (book == null)
                return "Book not found";

            // If the book is borrowed by someone else but copies are available
            if (book.IsBorrowed && book.BorrowedBy != borrowedBy)
            {
                if (book.CopiesAvailable > 0)
                {
                    // Decrease the number of available copies
                    book.CopiesAvailable--;
                    book.BorrowedBy = borrowedBy;
                    book.BorrowedDate = borrowDate;
                    book.DueDate = borrowDate.AddDays(borrowDays);

                    _context.Books.Update(book);
                    await _context.SaveChangesAsync();

                    // Process PayPal payment
                    var cart = new ShoppingCart
                    {
                        UserId = borrowedBy,
                        CartDetails = new List<CartDetail>
                {
                    new CartDetail
                    {
                        Book = book,
                        Quantity = 1,
                        UnitPrice = book.BorrowPrice
                    }
                }
                    };

                    var paymentResult = await _paymentRepository.ProcessPayPalPayment(cart, true);

                    if (!paymentResult.IsSuccess)
                    {
                        // Rollback the borrow status on payment failure
                        book.CopiesAvailable++;
                        book.BorrowedBy = null;
                        book.BorrowedDate = null;
                        book.DueDate = null;

                        _context.Books.Update(book);
                        await _context.SaveChangesAsync();

                        return "Payment failed. Your borrow request has been canceled.";
                    }

                    return paymentResult.RedirectUrl; // Return the PayPal approval URL
                }

                // If no copies are available, add the user to the waiting list
                if (book.CopiesAvailable == 0)
                {
                    // Add user to waiting list if not already present
                    if (!book.WaitingList.Contains(borrowedBy))
                    {
                        book.WaitingList.Add(borrowedBy);
                        _context.Books.Update(book);
                        await _context.SaveChangesAsync();

                        var waitingCount = book.WaitingList.Count;
                        return $"Book is already borrowed. You have been added to the waiting list. " +
                               $"You are number {waitingCount} in the queue.";
                    }

                    return "You are already on the waiting list for this book.";
                }

                return "Book is already borrowed by another user.";
            }

            // If the book is available for borrowing
            if (!book.IsBorrowed && book.CopiesAvailable > 0|| book.IsBorrowed && book.CopiesAvailable>0)
            {
                // Decrease the available copies and update borrow status
                book.CopiesAvailable--;
                book.IsBorrowed = true;
                book.BorrowedBy = borrowedBy;
                book.BorrowedDate = borrowDate;
                book.DueDate = borrowDate.AddDays(borrowDays);

                _context.Books.Update(book);
                await _context.SaveChangesAsync();

                // Process PayPal payment
                var cart = new ShoppingCart
                {
                    UserId = borrowedBy,
                    CartDetails = new List<CartDetail>
            {
                new CartDetail
                {
                    Book = book,
                    Quantity = 1,
                    UnitPrice = book.BorrowPrice
                }
            }
                };

                var paymentResult = await _paymentRepository.ProcessPayPalPayment(cart, true);

                if (!paymentResult.IsSuccess)
                {
                    // Rollback the borrow status on payment failure
                    book.CopiesAvailable++;
                    book.IsBorrowed = false;
                    book.BorrowedBy = null;
                    book.BorrowedDate = null;
                    book.DueDate = null;

                    _context.Books.Update(book);
                    await _context.SaveChangesAsync();

                    return "Payment failed. Your borrow request has been canceled.";
                }

                return paymentResult.RedirectUrl; // Return the PayPal approval URL
            }

            return "No copies available for borrowing.";
        }

        



        // Return book method
        public async Task<bool> ReturnBookAsync(int bookId)
        {
            var book = await _context.Books.FindAsync(bookId);

            if (book == null)
                return false;  // Book not found

            if (!book.IsBorrowed)
                return false;  // The book is not borrowed

            // Reset the borrowed details
            book.IsBorrowed = false;
            book.BorrowedBy = null;
            book.BorrowedDate = null;
            book.DueDate = null;

            _context.Books.Update(book);
            await _context.SaveChangesAsync();

            // Notify the next user in the waiting list
            var nextUserId = book.WaitingList.FirstOrDefault();
            if (nextUserId != null)
            {
                book.WaitingList.Remove(nextUserId);
                _context.Books.Update(book);

                var notification = new Notification
                {
                    UserId = nextUserId,
                    Message = $"The book '{book.BookName}' is now available for borrowing.",
                    CreatedAt = DateTime.Now,
                    IsRead = false
                };

                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync();
            }

            return true;  // Successfully returned the book
        }

        // Confirm payment method
        public async Task<bool> ConfirmPaymentAsync(string paymentId, string payerId)
        {
            var apiContext = _paymentRepository.GetApiContext();
            var payment = new Payment { id = paymentId };
            var executedPayment = payment.Execute(apiContext, new PaymentExecution { payer_id = payerId });

            return executedPayment.state.ToLower() == "approved";
        }

        // Send reminder notifications 5 days before the return time
        public async Task SendReminderNotifications()
        {
            var booksToRemind = await _context.Books
                .Where(b => b.IsBorrowed && b.DueDate.HasValue && b.DueDate.Value.AddDays(-5) <= DateTime.Now)
                .ToListAsync();

            foreach (var book in booksToRemind)
            {
                var user = await _context.Users.FindAsync(book.BorrowedBy);
                if (user != null)
                {
                    var notification = new Notification
                    {
                        UserId = book.BorrowedBy,
                        Message = $"Reminder: The book '{book.BookName}' is due for return in 5 days.",
                        CreatedAt = DateTime.Now,
                        IsRead = false
                    };

                    _context.Notifications.Add(notification);

                    // Send email notification
                    var subject = "Book Return Reminder";
                    var message = $"Dear {user.UserName},<br/><br/>This is a reminder that the book '{book.BookName}' is due for return in 5 days.<br/><br/>Thank you,<br/>Book Shopping Team";
                    await _emailSender.SendEmailAsync(user.Email, subject, message);
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}