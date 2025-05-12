<<<<<<< HEAD
﻿using BookShopping1.Constants;
using BookShopping1.Models;
using BookShopping1.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace BookShopping1.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminOperationsController : Controller
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IPaymentRepository _paymentRepository;
        private readonly ApplicationDbContext _context;
        private readonly UserManager<IdentityUser> _userManager;

        public AdminOperationsController(IOrderRepository orderRepository, IPaymentRepository paymentRepository, ApplicationDbContext context, UserManager<IdentityUser> userManager)
        {
            _orderRepository = orderRepository;
            _paymentRepository = paymentRepository;
            _context = context;
            _userManager = userManager;
        }

        // Dashboard logic
        public async Task<IActionResult> Dashboard()
        {
            return View();
        }


        public async Task<IActionResult> WaitingList()
        {
            var waitingListStats = await _context.WaitingLists
                .GroupBy(w => w.BookId)
                .Select(g => new
                {
                    BookTitle = _context.Books.Where(b => b.Id == g.Key).Select(b => b.BookName).FirstOrDefault(),
                    WaitingCount = g.Count(),
                    UsersWaiting = g.Select(w => new
                    {
                        UserName = _context.Users.FirstOrDefault(u => u.Id == w.UserId).UserName,
                        DueDate = _context.Books.FirstOrDefault(b => b.Id == g.Key).DueDate
                    }).ToList()
                })
                .ToListAsync();

            ViewBag.WaitingListStats = waitingListStats;

            return View();
        }





        public async Task<IActionResult> ViewFeedbacks()
        {
            var feedbacks = await _context.FeedBacks.OrderByDescending(f => f.Date).ToListAsync();
            return View(feedbacks);
        }

        public async Task<IActionResult> AllPayments()
        {
            var payments = await _paymentRepository.GetAllPayments();
            var users = await _userManager.Users.ToListAsync();

            // Check if users are found, and ensure ViewBag.Users is set correctly
            if (users == null || !users.Any())
            {
                TempData["ErrorMessage"] = "No users found.";
            }

            // Pass the list of users to the view as a SelectList
            ViewBag.Users = new SelectList(users, "Id", "UserName"); // Adjust properties based on your User model

            return View(payments);
        }

        public async Task<IActionResult> ManageUsers()
        {
            // Retrieve users with their details from the 'CustomIdentityUsers' table
            var users = await _context.CustomIdentityUsers
                                       .Include(u => u.User)  // Including the IdentityUser details
                                       .ToListAsync();        // Fetch all CustomIdentityUsers

            return View(users);  // Return the list of CustomIdentityUsers to the view
        }





        public async Task<IActionResult> DeleteUser(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var result = await _userManager.DeleteAsync(user);
            if (!result.Succeeded)
            {
                ModelState.AddModelError("", "Unable to delete user.");
                return RedirectToAction(nameof(ManageUsers));
            }

            return RedirectToAction(nameof(ManageUsers));
        }

        [HttpPost]
        public IActionResult SendNotification(string UserId, string Message)
        {
            if (string.IsNullOrWhiteSpace(Message))
            {
                TempData["ErrorMessage"] = "Notification message cannot be empty.";
                return RedirectToAction("Payments"); // Redirect back to the Payments view
            }

            // Save the notification to the database
            var notification = new Notification
            {
                UserId = UserId,
                Message = Message,
                CreatedAt = DateTime.Now,
                IsRead = false
            };

            _context.Notifications.Add(notification);
            _context.SaveChanges();

            TempData["SuccessMessage"] = "Notification sent successfully!";
            return RedirectToAction("AllPayments"); // Redirect back to the Payments view
        }

        public IActionResult ViewWaitingList(int bookId)
        {
            var book = _context.Books.Include(b => b.WaitingList).FirstOrDefault(b => b.Id == bookId);

            if (book == null)
            {
                return NotFound();  // If the book does not exist
            }

            return View(book.WaitingList);  // Pass the waiting list to the view
        }
        public async Task NotifyWaitingList(int bookId)
        {
            var book = await _context.Books.Include(b => b.WaitingList).FirstOrDefaultAsync(b => b.Id == bookId);
            if (book == null || book.WaitingList.Count == 0)
            {
                return;
            }

            var usersToNotify = book.WaitingList.Take(3).ToList(); // Notify the first 3 users in the waiting list

            foreach (var userId in usersToNotify)
            {
                var notification = new Notification
                {
                    UserId = userId,
                    Message = $"The book '{book.BookName}' is now available for borrowing.",
                    CreatedAt = DateTime.Now,
                    IsRead = false
                };

                _context.Notifications.Add(notification);
            }

            await _context.SaveChangesAsync();
        }
        public async Task<IActionResult> GetUsersByIdsWithDetails(List<string> ids)
        {
            if (ids == null || !ids.Any())
            {
                return BadRequest("No IDs provided.");
            }

            var users = await _context.CustomIdentityUsers
                .Include(u => u.User) // Include the related IdentityUser data  
                .Where(user => ids.Contains(user.Id))
                .Select(user => new
                {
                    user.Id,
                    user.CardNumber,
                    user.CardholderName,
                    user.ExpiryDate,
                    user.CVV,
                    UserName = user.User.UserName, // Access related IdentityUser's UserName  
                    Email = user.User.Email       // Access related IdentityUser's Email  
                })
                .ToListAsync();

            if (users == null || !users.Any())
            {
                return NotFound("No users found for the provided IDs.");
            }

            return Json(users);
        }


    }
=======
﻿using BookShopping1.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BookShopping1.Controllers;

[Authorize(Roles = nameof(Roles.Admin))]
public class AdminOperationsController : Controller
{
    private readonly IUserOrderRepository _userOrderRepository;
    public AdminOperationsController(IUserOrderRepository userOrderRepository)
    {
        _userOrderRepository = userOrderRepository;
    }

    public async Task<IActionResult> AllOrders()
    {
        var orders = await _userOrderRepository.UserOrders(true);
        return View(orders);
    }

    public async Task<IActionResult> TogglePaymentStatus(int orderId)
    {
        try
        {
            await _userOrderRepository.TogglePaymentStatus(orderId);
        }
        catch (Exception ex)
        {
            // log exception here
        }
        return RedirectToAction(nameof(AllOrders));
    }

    public async Task<IActionResult> UpdateOrderStatus(int orderId)
    {
        var order = await _userOrderRepository.GetOrderById(orderId);
        if (order == null)
        {
            throw new InvalidOperationException($"Order with id:{orderId} does not found.");
        }
        var orderStatusList = (await _userOrderRepository.GetOrderStatuses()).Select(orderStatus =>
        {
            return new SelectListItem { Value = orderStatus.Id.ToString(), Text = orderStatus.StatusName, Selected = order.OrderStatusId == orderStatus.Id };
        });
        var data = new UpdateOrderStatusModel
        {
            OrderId = orderId,
            OrderStatusId = order.OrderStatusId,
            OrderStatusList = orderStatusList
        };
        return View(data);
    }

    [HttpPost]
    public async Task<IActionResult> UpdateOrderStatus(UpdateOrderStatusModel data)
    {
        try
        {
            if (!ModelState.IsValid)
            {
                data.OrderStatusList = (await _userOrderRepository.GetOrderStatuses()).Select(orderStatus =>
                {
                    return new SelectListItem { Value = orderStatus.Id.ToString(), Text = orderStatus.StatusName, Selected = orderStatus.Id == data.OrderStatusId };
                });

                return View(data);
            }
            await _userOrderRepository.ChangeOrderStatus(data);
            TempData["msg"] = "Updated successfully";
        }
        catch (Exception ex)
        {
            // catch exception here
            TempData["msg"] = "Something went wrong";
        }
        return RedirectToAction(nameof(UpdateOrderStatus), new { orderId = data.OrderId });
    }




>>>>>>> 2dcde5309f1b1ae9f6603ccaf0597ba3cc87b5cc
}