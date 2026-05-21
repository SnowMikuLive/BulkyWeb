using BulkyWeb.Models;

namespace BulkyWeb.DataAccess.Repository.IRepository;

public interface IShoppingCartRepository : IRepository<ShoppingCart>
{
    void Update(ShoppingCart obj);
}
