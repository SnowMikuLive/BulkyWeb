using BulkyWeb.Models;

namespace BulkyWeb.DataAccess.Repository.IRepository;

public interface IOrderDetailRepository : IRepository<OrderDetail>
{
    void Update(OrderDetail obj);
}
