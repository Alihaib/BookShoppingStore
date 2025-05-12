using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using BookShopping1.Services;
using Microsoft.EntityFrameworkCore;
using BookShopping1.Repositories;
using BookShopping1.Models;

namespace BookShopping1.Controllers
{
    [Route("Library")]
    public class LibraryController : Controller
    {
        private readonly ICartRepository _cartRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly IFileService _fileService;
        private readonly IBookRepository _bookRepository;
        private readonly IEmailSender _emailsender;
        private readonly ApplicationDbContext _context;
        private readonly BorrowService _borrowService;

        public LibraryController(ICartRepository cartRepository, IPaymentRepository paymentRepository, IFileService fileService, IBookRepository bookRepository, IEmailSender emailsender, ApplicationDbContext context, BorrowService borrowService)
        {
            _cartRepository = cartRepository;
            _paymentRepository = paymentRepository;
            _fileService = fileService;
            _bookRepository = bookRepository;
            _emailsender = emailsender;
            _context = context;
            _borrowService = borrowService;
        }

        [Authorize]
        [HttpGet("MyLibrary")]
        public async Task<IActionResult> MyLibrary()
        {
            var userId = User.Identity.Name;
            var books = await _cartRepository.GetUserLibrary(userId);

            // Assuming you have a way to check borrowed books status, update the model if needed
            var borrowedBooks = books.Where(book => book.IsBorrowed).ToList();
            var availableBooks = books.Where(book => !book.IsBorrowed).ToList();

            // Combine the lists (optional if you want them mixed or display separately)
            var allBooks = borrowedBooks.Concat(availableBooks).ToList();

            return View(allBooks);
        }

        [Authorize]
        [HttpGet("BooksCount")]
        public async Task<IActionResult> BooksCount()
        {
            var booksCount = await _bookRepository.GetBooksCountAsync();
            return View(booksCount);
        }

        [Authorize]
        [HttpPost("BorrowBook")]
        public async Task<IActionResult> BorrowBook([FromBody] BorrowBookRequest request)
        {
            var userId = User.Identity?.Name; // Assuming user is authenticated
            if (userId == null)
            {
                return Unauthorized("User is not authenticated.");
            }

            var book = await _cartRepository.GetBookById(request.BookId);
            if (book == null)
            {
                return BadRequest("Book not found.");
            }

            if (book.CopiesAvailable <= 0)
            {
                return BadRequest("No copies available for borrowing.");
            }

            // Check if the user has borrowed less than 3 books
            var borrowedBooksCount = await _cartRepository.GetUserBorrowedBooksCount(userId);
            if (borrowedBooksCount >= 3)
            {
                TempData["ErrorMessage"] = "You can only borrow up to 3 books at the same time.";
                return RedirectToAction("MyLibrary");  // Redirecting the user back to the MyLibrary page
            }

            // Update the book's borrowed status
            book.IsBorrowed = true;
            book.BorrowedBy = userId;
            book.BorrowedDate = DateTime.UtcNow;
            book.DueDate = DateTime.UtcNow.AddDays(14); // Set a default due date for borrowing
            book.CopiesAvailable -= 1;

            // Update the book in the database
            _context.Books.Update(book);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Book borrowed successfully.";
            return RedirectToAction("MyLibrary");  // Redirecting the user back to the MyLibrary page
        }

        [HttpPost("ReturnBook")]
        public async Task<IActionResult> ReturnBook([FromBody] ReturnBookRequest request)
        {
            if (request == null || request.BookId == 0)
            {
                return BadRequest(new { message = "Invalid request data." });
            }

            // Fetch the book from the database
            var book = await _cartRepository.GetBookById(request.BookId);
            if (book == null)
            {
                return NotFound("Book not found.");
            }

            // Check if the authenticated user is the one who borrowed the book
            if (book.BorrowedBy != User.Identity.Name)
            {
                return Unauthorized("You cannot return a book you did not borrow.");
            }

            // Update the book's status and increase available copies
            book.IsBorrowed = false;
            book.BorrowedBy = null;
            book.BorrowedDate = null;
            book.DueDate = null;
            book.CopiesAvailable += 1;

            // Update the book in the database
            _context.Books.Update(book);
            await _context.SaveChangesAsync();

            // Notify users in the waiting list (assuming WaitingList has UserEmail or UserId)
            var waitingListUsers = await _context.WaitingLists
                .Where(w => w.BookId == request.BookId)
                .Select(w => new
                {
                    UserEmail = _context.Users.FirstOrDefault(u => u.Id == w.UserId).Email
                })
                .ToListAsync();

            foreach (var user in waitingListUsers)
            {
                var subject = "Book Now Available!";
                var message = $@"
Dear {user.UserEmail}, 

The book '{book.BookName}' that you were waiting for is now available!

Please visit our library to borrow it or reserve it as soon as possible.

Happy reading!

The Library Team
";

                await _emailsender.SendEmailAsync(user.UserEmail, subject, message);
            }

            return Json(new { message = "Book returned successfully. Notifications sent to waiting list users." });
        }


        [HttpPost("BuyBook")]
        public async Task<IActionResult> BuyBook([FromBody] BuyRequest request)
        {
            var userId = User.Identity?.Name; // Assuming user is authenticated
            if (userId == null)
            {
                return Unauthorized("User is not authenticated.");
            }

            var book = await _cartRepository.GetBookById(request.BookId);
            if (book == null)
            {
                return BadRequest("Book not found.");
            }

            var paymentResult = await _paymentRepository.ProcessDirectPurchase(book, 1, false);
            if (!paymentResult.IsSuccess)
            {
                return BadRequest("Unable to process payment.");
            }

            await _cartRepository.AddBookToUserLibrary(userId, request.BookId);
            await _cartRepository.RemoveItem(userId, request.BookId);

            var bookDetails = new[]
             {
                new
                {
                    book.BookName,
                    book.Price,
                    Quantity = 1,
                    DiscountPrice = (double?)book.DiscountPrice, // Ensure nullable double
                    DiscountEndDate = (DateTime?)book.DiscountEndDate // Ensure nullable DateTime
                }
            };

                        var totalAmount = bookDetails.Sum(bd =>
                        {
                            if (bd.DiscountPrice.HasValue && bd.DiscountEndDate.HasValue && bd.DiscountEndDate > DateTime.Now)
                            {
                                // Use the discounted price if it's valid
                                return bd.DiscountPrice.Value * bd.Quantity;
                            }
                            else
                            {
                                // Use the regular price otherwise
                                return bd.Price * bd.Quantity;
                            }
                        });

            var bookList = string.Join("<br>", bookDetails.Select(bd =>
            {
                if (bd.DiscountPrice.HasValue && bd.DiscountEndDate.HasValue && bd.DiscountEndDate > DateTime.Now)
                {
                    // Show the discounted price
                    return $"{bd.BookName} - {bd.Quantity} x ${bd.DiscountPrice.Value} (Discounted from ${bd.Price})";
                }
                else
                {
                    // Show the regular price
                    return $"{bd.BookName} - {bd.Quantity} x ${bd.Price}";
                }
            }));


            var subject = "Congratulations on Your Purchase!";
            var htmlMessage = $@"
<html>
<head>
  <style>
    body {{
      font-family: Arial, sans-serif;
      color: #333;
      line-height: 1.6;
    }}
    h1 {{
      color: #2c3e50;
    }}
    p {{
      font-size: 14px;
    }}
    .footer {{
      margin-top: 20px;
      font-size: 12px;
      color: #888;
    }}
  </style>
</head>
<body>
  <h1>Congratulations on Your Purchase!</h1>

  <p>Dear User,</p>

  <p>Thank you for your purchase from our eBook Library Service! We are thrilled to have you as a valued customer.</p>

  <p>Here are the details of your purchase:</p>
  <p>{bookList}</p>
  <p><strong>Total Amount: ${totalAmount}</strong></p>

  <p>We hope you enjoy your new eBook(s) and have a wonderful reading experience. If you have any questions or need assistance, please do not hesitate to contact us.</p>

  <p>Happy Reading!</p>

  <div class='footer'>
    <p>Warm Regards,</p>
    <p>The eBook Library Service Team</p>
  </div>
</body>
</html>
";
            await _emailsender.SendEmailAsync(userId, subject, htmlMessage);

            var paymentInfo = new PaymentInfo
            {
                UserId = userId,
                BookId = request.BookId,
                Amount = book.Price,
                PaymentDate = DateTime.UtcNow,
                IsBorrow = false
            };
            await _paymentRepository.StorePaymentInfo(paymentInfo);

            return Ok(new { paypalUrl = paymentResult.RedirectUrl });
        }

        [HttpGet("DownloadBook/{id}/{format}")]
        public async Task<IActionResult> DownloadBook(int id, string format)
        {
            var book = await _cartRepository.GetBookById(id);
            if (book == null)
            {
                return NotFound();
            }

            string filePath = format.ToLower() switch
            {
                "pdf" => book.PdfFilePath,
                "epub" => book.EpubFilePath,
                "fb2" => book.F2bFilePath,
                "mobi" => book.MobiFilePath,
                _ => null
            };

            if (string.IsNullOrEmpty(filePath))
            {
                return NotFound();
            }

            var wwwPath = _fileService.GetWebRootPath();
            var path = Path.Combine(wwwPath, "imaget", filePath);
            if (!System.IO.File.Exists(path))
            {
                return NotFound();
            }

            var memory = new MemoryStream();
            using (var stream = new FileStream(path, FileMode.Open))
            {
                await stream.CopyToAsync(memory);
            }
            memory.Position = 0;
            return File(memory, GetContentType(path), Path.GetFileName(path));
        }

        private string GetContentType(string path)
        {
            var types = GetMimeTypes();
            var ext = Path.GetExtension(path).ToLowerInvariant();
            return types[ext];
        }

        private Dictionary<string, string> GetMimeTypes()
        {
            return new Dictionary<string, string>
            {
                {".txt", "text/plain"},
                {".pdf", "application/pdf"},
                {".doc", "application/vnd.ms-word"},
                {".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document"},
                {".xls", "application/vnd.ms-excel"},
                {".xlsx", "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"},
                {".png", "image/png"},
                {".jpg", "image/jpeg"},
                {".jpeg", "image/jpeg"},
                {".gif", "image/gif"},
                {".csv", "text/csv"},
                {".epub", "application/epub+zip"},
                {".mobi", "application/x-mobipocket-ebook"},
                {".fb2", "application/octet-stream"}
            };
        }

        // Send reminder notifications 5 days before the return time
        public async Task SendReminderNotifications()
        {
            var booksToRemind = await _context.Books
                .Where(b => b.IsBorrowed && b.DueDate.HasValue && b.DueDate.Value.AddDays(-5) <= DateTime.Now)
                .ToListAsync();

            foreach (var book in booksToRemind)
            {
                var notification = new Notification
                {
                    UserId = book.BorrowedBy,
                    Message = $"Reminder: The book '{book.BookName}' is due for return in 5 days.",
                    CreatedAt = DateTime.Now,
                    IsRead = false
                };

                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();
        }

        // Add the DeleteBook method
        [HttpPost("DeleteBook")]
        public async Task<IActionResult> DeleteBook([FromBody] DeleteBookRequest request)
        {
            var userId = User.Identity?.Name; // Assuming user is authenticated
            if (userId == null)
            {
                return Unauthorized("User is not authenticated.");
            }

            var book = await _cartRepository.GetBookById(request.BookId);
            if (book == null)
            {
                return BadRequest("Book not found.");
            }

            // Remove the book from the user's library
            var userLibrary = await _context.PurchasedBooks
                .FirstOrDefaultAsync(pb => pb.UserId == userId && pb.BookId == request.BookId);

            if (userLibrary == null)
            {
                return BadRequest("Book not found in user's library.");
            }

            _context.PurchasedBooks.Remove(userLibrary);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Book removed from library successfully." });
        }

    }


    public class BorrowBookRequest
    {
        public int BookId { get; set; }
    }

    public class ReturnBookRequest
    {
        public int BookId { get; set; }
    }

    public class BuyRequest
    {
        public int BookId { get; set; }
    }

    public class DeleteBookRequest
    {
        public int BookId { get; set; }
    }
}