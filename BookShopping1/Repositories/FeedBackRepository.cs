using BookShopping1.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace BookShopping1.Repositories
{
    public class FeedBackRepository:IFeedBackRepository
    {
        private readonly ApplicationDbContext _context;

        public FeedBackRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // Fetch all feedbacks
        public async Task<List<FeedBack>> GetAllFeedbacksAsync()
        {
            return await _context.FeedBacks.ToListAsync();
        }

        // Fetch feedbacks by BookId
        public async Task<List<FeedBack>> GetFeedbacksByBookIdAsync(int bookId)
        {
            return await _context.FeedBacks.Where(f => f.BookId == bookId).ToListAsync();
        }

        // Add new feedback
        public async Task AddFeedbackAsync(FeedBack feedback)
        {
            try
            {
                _context.FeedBacks.Add(feedback);
                await _context.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                throw new Exception("Error saving feedback to the database.", ex);
            }
        }

    }
}
