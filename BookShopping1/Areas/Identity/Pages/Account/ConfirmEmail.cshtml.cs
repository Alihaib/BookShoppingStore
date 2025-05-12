// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
#nullable disable

using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace BookShopping1.Areas.Identity.Pages.Account
{
    public class ConfirmEmailModel : PageModel
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly IEmailSender _emailSender;

        public ConfirmEmailModel(UserManager<IdentityUser> userManager, IEmailSender emailSender)
        {
            _userManager = userManager;
            _emailSender = emailSender;
        }

        /// <summary>
        ///     This API supports the ASP.NET Core Identity default UI infrastructure and is not intended to be used
        ///     directly from your code. This API may change or be removed in future releases.
        /// </summary>
        [TempData]
        public string StatusMessage { get; set; }
        public async Task<IActionResult> OnGetAsync(string userId, string code)
        {
            if (userId == null || code == null)
            {
                return RedirectToPage("/Index");
            }

            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return NotFound($"Unable to load user with ID '{userId}'.");
            }

            code = Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(code));
            var result = await _userManager.ConfirmEmailAsync(user, code);
            if (result.Succeeded)
            {
                StatusMessage = "Thank you for confirming your email.";

                // Send welcome email
                var email = user.Email;
                var subject = "Welcome to our eBook website!";
                var htmlMessage = @"
<html>
<head>
  <style>
    body {
      font-family: Arial, sans-serif;
      color: #333;
      line-height: 1.6;
    }
    h1 {
      color: #2c3e50;
    }
    h2 {
      color: #16a085;
    }
    p {
      font-size: 14px;
    }
    .footer {
      margin-top: 20px;
      font-size: 12px;
      color: #888;
    }
  </style>
</head>
<body>
  <h1>Welcome to eBook Library Service!</h1>

  <p>Dear User,</p>

  <p>Welcome to the <strong>eBook Library Service</strong>! We’re excited to have you join us as you explore our diverse collection of eBooks, from mystery novels to educational resources.</p>

  <h2>What We Offer:</h2>
  <ul>
    <li>Seamless borrowing and buying experiences</li>
    <li>Personalized user profiles for managing your eBook library</li>
    <li>Dynamic search and filtering tools</li>
    <li>Fair pricing and secure payment options (credit cards & PayPal)</li>
  </ul>

  <p>We aim to make reading more accessible and enjoyable, with features like a waiting list system, notifications, and age-appropriate filters.</p>

  <h2>About Us:</h2>
  <p>Founded by <strong>Hasan Musa</strong> and <strong>Ali Heib</strong>, two passionate software engineers, we created this platform to help you easily manage and enjoy your digital library anytime, anywhere.</p>

  <p>Thank you for choosing us—<strong>Happy Reading!</strong></p>

  <div class='footer'>
    <p>Warm Regards,</p>
    <p>Hasan & Ali<br>The eBook Library Service</p>
  </div>
</body>
</html>
";

                await _emailSender.SendEmailAsync(email, subject, htmlMessage);
            }
            else
            {
                StatusMessage = "Error confirming your email.";
            }
            return Page();
        }
    }
}
