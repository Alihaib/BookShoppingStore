using System;
using System.Diagnostics;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using BookShopping1.Models;
using BookShopping1.Models.DTOs;
using BookShopping1.Repositories;
using BookShopping1.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace BookShopping1.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IHomeRepository _homeRepository;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IBookRepository _bookRepository;
        private readonly ICartRepository _cartRepository;
        private readonly BorrowService _borrowService;
        private readonly IEmailSender _emailsender;

        public HomeController(ILogger<HomeController> logger, IHomeRepository homeRepository, ApplicationDbContext context, UserManager<IdentityUser> userManager, IBookRepository bookRepository, ICartRepository cartRepository, BorrowService borrowService,IEmailSender emailsender)
        {
            _logger = logger;
            _homeRepository = homeRepository;
            _context = context;
            _userManager = userManager;
            _bookRepository = bookRepository;
            _cartRepository = cartRepository;
            _borrowService = borrowService;
            _emailsender = emailsender;
        }
        public async Task<IActionResult> Details(int id)
        {
            var book = await _homeRepository.GetBookById(id);
            if (book == null)
            {
                return NotFound(); // Return 404 if the book is not found
            }

            // Pass the selected book to the view using ViewBag
            ViewBag.SelectedBook = book;
            return View(book);
        }

        public async Task<IActionResult> Index(string searchTitle = "", string searchAuthor = "", string searchPublisher = "", int genreId = 0, string orderBy = "", bool onSale = false, int ageLimit = 0, bool mostPopular = false)
        {
            _logger.LogInformation("Search Title: {SearchTitle}, Search Author: {SearchAuthor}, Search Publisher: {SearchPublisher}, Genre ID: {GenreId}, Order By: {OrderBy}, On Sale: {OnSale}, Age Limit: {AgeLimit}, Most Popular: {MostPopular}", searchTitle, searchAuthor, searchPublisher, genreId, orderBy, onSale, ageLimit, mostPopular);

            var books = await _homeRepository.GetBooks(searchTitle, searchAuthor, searchPublisher, genreId, orderBy, onSale, ageLimit);
            var genres = await _homeRepository.Genres();
            var mostbooks = mostPopular ? await _bookRepository.GetMostPopularBooksAsync() : null;

            var bookModel = new BookDisplayModel
            {
                Books = books,
                Genres = genres,
                SearchTitle = searchTitle,
                SearchAuthor = searchAuthor,
                SearchPublisher = searchPublisher,
                GenreId = genreId,
                OrderBy = orderBy,
                OnSale = onSale,
                AgeLimit = ageLimit,
                STerm = searchTitle,
                MostPopularBooks = mostbooks,
                MostPopular = mostPopular
            };

            var userId = _userManager.GetUserId(User);

            var notifications = await _context.Notifications
                                               .Where(n => n.UserId == userId)
                                               .OrderByDescending(n => n.CreatedAt)
                                               .ToListAsync();

            // Pass notifications to the view using ViewData
            ViewData["Notifications"] = notifications.Select(n => n.Message).ToList();

            // Retrieve feedbacks
            var feedbacks = await _context.RateWebsites.OrderByDescending(f => f.Date).ToListAsync();
            ViewData["Feedbacks"] = feedbacks;

            // Return the view with the book model and notifications
            return View(bookModel);
        }

        [HttpPost]
        public async Task<IActionResult> BorrowBook([FromBody] BorrowBookRequest request)
        {
            if (request == null || request.BookId == 0)
            {
                return BadRequest(new { message = "Invalid request data." });
            }

            // Get the current user
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Check if the user has already borrowed the maximum number of books
            var borrowedBooksCount = await _context.Books
                .CountAsync(b => b.BorrowedBy == user.UserName && b.IsBorrowed);

            if (borrowedBooksCount >= 3)
            {
                return BadRequest(new { message = "You have already borrowed the maximum of 3 books. Return a book to borrow another." });
            }

            // Find the book by its ID
            var book = await _context.Books
                .Include(b => b.Genre)
                .FirstOrDefaultAsync(b => b.Id == request.BookId);

            if (book == null)
            {
                return NotFound(new { message = "Book not found." });
            }

            // Check if the book has been borrowed
            if (book.IsBorrowed)
            {
                // Get the waiting list for the book
                var waitingList = await _context.WaitingLists
                    .Where(w => w.BookId == book.Id)
                    .OrderBy(w => w.RequestedAt)
                    .ToListAsync();

                // Calculate how many people are in the waiting list
                int waitingCount = waitingList.Count;
                DateTime estimatedAvailabilityDate = book.BorrowedDate.Value.AddDays(waitingCount * 30);

                // Notify the user how many are waiting and how long they will have to wait
                string waitingMessage = $"There are {waitingCount} people ahead of you. Estimated availability date: {estimatedAvailabilityDate.ToShortDateString()}";

                // Return waiting message with a flag for front-end
                return Json(new { success = true, waitingMessage });
            }

            // The book is available, proceed to borrow it
            book.IsBorrowed = true;
            book.BorrowedBy = user.UserName;
            book.BorrowedDate = DateTime.UtcNow;

            // Save the book’s new state
            _context.Books.Update(book);
            await _context.SaveChangesAsync();

            // Redirect to PayPal for payment
            var redirectUrl = Url.Action("PaymentConfirmation", "Home", null, Request.Scheme);
            var approvalUrl = await _borrowService.BorrowBookAsync(book.Id, user.UserName, DateTime.UtcNow, 30, redirectUrl);

            if (!string.IsNullOrEmpty(approvalUrl))
            {
                // Send email notification
                var subject = "Book Borrowed Successfully!";
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
  <h1>Book Borrowed Successfully!</h1>

  <p>Dear {user.UserName},</p>

  <p>Thank you for borrowing a book from our eBook Library Service! We are thrilled to have you as a valued customer.</p>

  <p>Here are the details of your borrowed book:</p>
  <p>Book Name: {book.BookName}</p>
  <p>Borrow Price: ${book.BorrowPrice}</p>

  <p>We hope you enjoy your new eBook and have a wonderful reading experience. If you have any questions or need assistance, please do not hesitate to contact us.</p>

  <p>Happy Reading!</p>

  <div class='footer'>
    <p>Warm Regards,</p>
    <p>The eBook Library Service Team</p>
  </div>
</body>
</html>
";
                await _emailsender.SendEmailAsync(user.Email, subject, htmlMessage);

                return Json(new { redirectUrl = approvalUrl });
            }
            else
            {
                return BadRequest(new { message = "Error borrowing book." });
            }
        }



        [HttpPost]
        public async Task<IActionResult> JoinWaitingList([FromBody] BorrowBookRequest request)
        {
            // Validate the request
            if (request == null || request.BookId == 0)
            {
                return BadRequest(new { message = "Invalid request data." });
            }

            // Get the current user
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login", "Account");
            }

            // Find the book by its ID
            var book = await _context.Books.FindAsync(request.BookId);
            if (book == null)
            {
                return NotFound(new { message = "Book not found." });
            }

            // Add the user to the waiting list for the book
            var waitingListEntry = new WaitingList
            {
                BookId = book.Id,
                UserId = user.Id,
                RequestedAt = DateTime.UtcNow
            };

            // Save the new waiting list entry
            _context.WaitingLists.Add(waitingListEntry);
            await _context.SaveChangesAsync();

            // Return success response
            return Json(new { success = true, message = "You have been added to the waiting list." });
        }

        public IActionResult GetBookPrice(int bookId)
        {
            var book = _context.Books.FirstOrDefault(b => b.Id == bookId);

            if (book != null)
            {
                decimal priceToShow = (decimal)book.Price;
                string oldPriceHtml = string.Empty;

                // Check if discount is applicable
                if (book.DiscountEndDate.HasValue && book.DiscountEndDate.Value > DateTime.Now)
                {
                    priceToShow = (decimal)book.DiscountPrice.Value;
                    oldPriceHtml = $"<span style=\"text-decoration: line-through;\" class=\"text-muted\">{book.Price.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("en-US"))}</span>";
                }

                return Json(new
                {
                    price = priceToShow.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("en-US")),
                    oldPrice = oldPriceHtml
                });
            }

            return Json(new { price = book?.Price.ToString("C", System.Globalization.CultureInfo.GetCultureInfo("en-US")) });
        }

        [HttpPost]
        public async Task<IActionResult> SubmitFeedback([FromBody] RateWebsite feedback)
        {
            if (feedback == null || string.IsNullOrEmpty(feedback.UserName) || feedback.Rating < 1 || feedback.Rating > 5)
            {
                return BadRequest(new { message = "Invalid feedback data." });
            }

            // Get the current user
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return Unauthorized(new { message = "User not logged in." });
            }

            // Set the UserId
            feedback.UserId = user.Id;

            // Save the feedback to the database
            _context.RateWebsites.Add(feedback);
            await _context.SaveChangesAsync();

            // Return success response
            return Json(new { success = true, message = "Thank you for your feedback!" });
        }

        [HttpGet]
        public async Task<IActionResult> GetFeedbacks()
        {
            var feedbacks = await _context.RateWebsites.OrderByDescending(f => f.Date).ToListAsync();
            return Json(feedbacks);
        }

    }
}