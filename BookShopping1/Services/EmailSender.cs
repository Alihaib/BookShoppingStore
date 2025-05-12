using Microsoft.AspNetCore.Identity.UI.Services;
using System.Net;
using System.Net.Mail;

namespace BookShopping1.Services
{
    public class EmailSender:IEmailSender
    {
        public async Task SendEmailAsync(string email, string subject, string htmlMessage)
        {
            var FromEmail = "aleheeb1@gmail.com";
            var FromPassword = "mlrw fbiz lfwc gcuw";

            MailMessage message = new();
            message.From = new MailAddress(FromEmail);
            message.To.Add(new MailAddress(email));
            message.Subject = subject;
            message.Body = htmlMessage;
            message.IsBodyHtml = true;

            var smtpClient = new SmtpClient("smtp.gmail.com")
            {
                Port = 587,
                Credentials = new NetworkCredential(FromEmail, FromPassword),
                EnableSsl = true,
            };
            smtpClient.Send(message);

        }
    }
}
