using BulkyWeb.Data;
using BulkyWeb.DataAccess.Repository.IRepository;
using BulkyWeb.Models;

namespace BulkyWeb.DataAccess.Repository;

public class ShoppingCartRepository : Repository<ShoppingCart>, IShoppingCartRepository
{
    private readonly ApplicationDbContext _db;

    public ShoppingCartRepository(ApplicationDbContext db) : base(db)
    {
        _db = db;
    }

    public void Update(ShoppingCart obj) => _db.ShoppingCarts.Update(obj);
}
