using BulkyWeb.Models;

namespace BulkyWeb.DataAccess.Repository.IRepository;

public interface ICategoryRepository : IRepository<Category>
{
    void Update(Category obj);
}
