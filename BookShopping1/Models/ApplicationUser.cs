using Microsoft.AspNetCore.Identity;

namespace BookShopping1.Models
{
    public class ApplicationUser : IdentityUser
    {
        public string ID { get; set; }
        public string CreditCardNumber { get; set; }
        public string ValidDate { get; set; }
        public string CVC { get; set; }
    }

}
