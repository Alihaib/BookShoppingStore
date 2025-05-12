using BookShopping1.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BookShopping1.Data
{
    public class ApplicationDbContext : IdentityDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Genre> Genres { get; set; }
        public DbSet<Book> Books { get; set; }
        public DbSet<ShoppingCart> ShoppingCarts { get; set; }
        public DbSet<CartDetail> CartDetails { get; set; }
        public DbSet<PurchasedBook> PurchasedBooks { get; set; }
        public DbSet<WaitingList> WaitingLists { get; set; }
        public DbSet<PaymentResult> PaymentResults { get; set; }
        public DbSet<Order> Orders { get; set; }
        public DbSet<FeedBack> FeedBacks { get; set; }
        public DbSet<PaymentInfo> PaymentInfos { get; set; }
        
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<RateWebsite> RateWebsites { get; set; }
        public DbSet<CustomIdentityUser> CustomIdentityUsers { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlServer("YourConnectionString", sqlServerOptions =>
                {
                    sqlServerOptions.CommandTimeout(180); // Set command timeout to 180 seconds
                });
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure PaymentResult entity
            modelBuilder.Entity<PaymentResult>().HasKey(pr => pr.Id);
        }
    }
}