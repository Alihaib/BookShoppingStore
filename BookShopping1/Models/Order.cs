using System;
using System.ComponentModel.DataAnnotations;

namespace BookShopping1.Models
{
    public class Order
    {
        public int Id { get; set; }
        [Required]
        public string UserId { get; set; }
        [Required]
        public int BookId { get; set; }
        public Book Book { get; set; }
        [Required]
        public int Quantity { get; set; }
        [Required]
        public double TotalPrice { get; set; }
        [Required]
        public DateTime OrderDate { get; set; }
    }
}