using BulkyWeb.Data;
using BulkyWeb.DataAccess.Repository.IRepository;

namespace BulkyWeb.DataAccess.Repository;

public class UnitOfWork : IUnitOfWork
{
    private readonly ApplicationDbContext _db;

    public UnitOfWork(ApplicationDbContext db)
    {
        _db = db;
        Category = new CategoryRepository(_db);
        Product = new ProductRepository(_db);
        Company = new CompanyRepository(_db);
        ApplicationUser = new ApplicationUserRepository(_db);
        ShoppingCart = new ShoppingCartRepository(_db);
        OrderHeader = new OrderHeaderRepository(_db);
        OrderDetail = new OrderDetailRepository(_db);
    }

    public ICategoryRepository Category { get; }
    public IProductRepository Product { get; }
    public ICompanyRepository Company { get; }
    public IApplicationUserRepository ApplicationUser { get; }
    public IShoppingCartRepository ShoppingCart { get; }
    public IOrderHeaderRepository OrderHeader { get; }
    public IOrderDetailRepository OrderDetail { get; }

    public void Save() => _db.SaveChanges();
}
