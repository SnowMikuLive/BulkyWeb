using BulkyWeb.Models;

namespace BulkyWeb.DataAccess.Repository.IRepository;

public interface ICompanyRepository : IRepository<Company>
{
    void Update(Company obj);
}
