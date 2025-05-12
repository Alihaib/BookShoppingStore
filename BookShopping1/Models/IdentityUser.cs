using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.ComponentModel.DataAnnotations;

namespace BookShopping1.Models
{
    
    public class CustomIdentityUser
    {
        [Key, ForeignKey("User")]
        public string Id { get; set; }

        public string? CardNumber { get; set; }
        public string? CardholderName { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? CVV { get; set; }

        // Navigation property one to one realtionship 
        public IdentityUser User { get; set; }
    }

}
