using System.Security.Claims;
using BulkyWeb.DataAccess.Repository.IRepository;
using Microsoft.AspNetCore.Mvc;

namespace BulkyWeb.ViewComponents;

public class ShoppingCartCountViewComponent : ViewComponent
{
    private readonly IUnitOfWork _unitOfWork;

    public ShoppingCartCountViewComponent(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public IViewComponentResult Invoke()
    {
        var userId = HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            return Content("0");
        }

        var count = _unitOfWork.ShoppingCart
            .GetAll(u => u.ApplicationUserId == userId)
            .Sum(u => u.Count);

        return Content(count.ToString());
    }
}
