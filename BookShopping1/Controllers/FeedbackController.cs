using BookShopping1.Models;
using BookShopping1.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BookShopping1.Controllers
{
    [Authorize]
    public class FeedbackController : Controller
    {
        private readonly IFeedBackRepository _repository;
        private readonly UserManager<IdentityUser> _userManager;
        private readonly ILogger<FeedbackController> _logger;

        public FeedbackController(UserManager<IdentityUser> userManager, ILogger<FeedbackController> logger, IFeedBackRepository feedbackrepo)
        {
            _repository = feedbackrepo;
            _userManager = userManager;
            _logger = logger;
        }

        // GET: Feedback/GiveFeedback
        [HttpGet]
        public async Task<IActionResult> GiveFeedback(int? bookId)
        {
            if (!bookId.HasValue)
            {
                return RedirectToAction("Index", "Home");
            }

            // Fetch feedbacks for a specific book
            var feedbacks = await _repository.GetFeedbacksByBookIdAsync(bookId.Value);

            // Initialize the Feedback model with BookId
            var model = new FeedBack { BookId = bookId.Value };

            // Pass the feedbacks to ViewBag (for displaying in the same page)
            ViewBag.Feedbacks = feedbacks;
            ViewBag.BookId = bookId;

            return View(model);
        }

        // POST: Feedback/SubmitFeedback
        [HttpPost]
        public async Task<IActionResult> SubmitFeedback(FeedBack feedback)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                _logger.LogError("User not authenticated.");
                return RedirectToAction("Login", "Account");
            }

            feedback.UserId = user.Id;
            feedback.UserName = user.UserName;
            feedback.Date = DateTime.Now;

            try
            {
                await _repository.AddFeedbackAsync(feedback);
                _logger.LogInformation("Feedback submitted successfully for user: {UserName}", user.UserName);

                return RedirectToAction("GiveFeedback", new { bookId = feedback.BookId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving feedback for user: {UserName}", user.UserName);
                return View("Error");
            }
        }

        // GET: Feedback/ViewFeedback
        [HttpGet]
        public async Task<IActionResult> ViewFeedback(int? bookId)
        {
            if (!bookId.HasValue)
            {
                _logger.LogWarning("Book ID was null when attempting to view feedback.");
                return RedirectToAction("Index", "Home");
            }

            try
            {
                // Fetch all feedbacks for the specified book
                var feedbacks = await _repository.GetFeedbacksByBookIdAsync(bookId.Value);

                if (feedbacks == null)
                {
                    _logger.LogInformation("No feedback found for book ID {BookId}.", bookId.Value);
                    return View(Enumerable.Empty<FeedBack>());
                }

                _logger.LogInformation("Feedbacks retrieved for book ID {BookId}.", bookId.Value);
                return View(feedbacks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving feedbacks for book ID {BookId}.", bookId);
                return View("Error");
            }
        }
    }
}
