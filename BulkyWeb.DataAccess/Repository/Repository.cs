using System.Linq.Expressions;
using BulkyWeb.Data;
using BulkyWeb.DataAccess.Repository.IRepository;
using Microsoft.EntityFrameworkCore;

namespace BulkyWeb.DataAccess.Repository;

public class Repository<T> : IRepository<T> where T : class
{
    private readonly ApplicationDbContext _db;
    internal DbSet<T> DbSet;

    public Repository(ApplicationDbContext db)
    {
        _db = db;
        DbSet = _db.Set<T>();
    }

    public void Add(T entity) => DbSet.Add(entity);

    public T? Get(Expression<Func<T, bool>> filter, string? includeProperties = null, bool tracked = true)
    {
        IQueryable<T> query = tracked ? DbSet : DbSet.AsNoTracking();
        query = query.Where(filter);

        if (!string.IsNullOrEmpty(includeProperties))
        {
            foreach (var includeProp in includeProperties.Split(
                         new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                query = query.Include(includeProp.Trim());
            }
        }

        return query.FirstOrDefault();
    }

    public IEnumerable<T> GetAll(Expression<Func<T, bool>>? filter = null, string? includeProperties = null)
    {
        IQueryable<T> query = DbSet;

        if (filter != null)
        {
            query = query.Where(filter);
        }

        if (!string.IsNullOrEmpty(includeProperties))
        {
            foreach (var includeProp in includeProperties.Split(
                         new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                query = query.Include(includeProp.Trim());
            }
        }

        return query.ToList();
    }

    public void Remove(T entity) => DbSet.Remove(entity);

    public void RemoveRange(IEnumerable<T> entity) => DbSet.RemoveRange(entity);
}
