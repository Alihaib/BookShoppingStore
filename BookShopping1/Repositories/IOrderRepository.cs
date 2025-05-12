using System.Collections.Generic;
using System.Threading.Tasks;
using BookShopping1.Models;

namespace BookShopping1.Repositories
{
    public interface IOrderRepository
    {
        Task<IEnumerable<Order>> GetAllOrdersAsync();
    }
}