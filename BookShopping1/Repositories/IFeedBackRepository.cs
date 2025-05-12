namespace BookShopping1.Repositories
{
    public interface IFeedBackRepository
    {
        Task AddFeedbackAsync(FeedBack feedback);
        Task<List<FeedBack>> GetAllFeedbacksAsync();
        Task<List<FeedBack>> GetFeedbacksByBookIdAsync(int bookId);
    }
}
