using BulkyWeb.Data;
using BulkyWeb.DataAccess.Repository.IRepository;
using BulkyWeb.Models;

namespace BulkyWeb.DataAccess.Repository;

public class OrderHeaderRepository : Repository<OrderHeader>, IOrderHeaderRepository
{
    private readonly ApplicationDbContext _db;

    public OrderHeaderRepository(ApplicationDbContext db) : base(db)
    {
        _db = db;
    }

    public void Update(OrderHeader obj) => _db.OrderHeaders.Update(obj);

    public void UpdateStatus(int id, string orderStatus, string? paymentStatus = null)
    {
        var orderFromDb = _db.OrderHeaders.FirstOrDefault(u => u.Id == id);
        if (orderFromDb == null)
        {
            return;
        }

        orderFromDb.OrderStatus = orderStatus;
        if (!string.IsNullOrEmpty(paymentStatus))
        {
            orderFromDb.PaymentStatus = paymentStatus;
        }
    }

    public void UpdateStripePaymentID(int id, string sessionId, string? paymentIntentId)
    {
        var orderFromDb = _db.OrderHeaders.FirstOrDefault(u => u.Id == id);
        if (orderFromDb == null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(sessionId))
        {
            orderFromDb.SessionId = sessionId;
        }

        if (!string.IsNullOrEmpty(paymentIntentId))
        {
            orderFromDb.PaymentIntentId = paymentIntentId;
            orderFromDb.PaymentDate = DateTime.Now;
        }
    }
}
