using System.Diagnostics;
using System.Security.Claims;
using BulkyWeb.DataAccess.Repository.IRepository;
using BulkyWeb.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BulkyWeb.Areas.Customer.Controllers;

[Area("Customer")]
public class HomeController : Controller
{
    private readonly IUnitOfWork _unitOfWork;

    public HomeController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public IActionResult Index()
    {
        IEnumerable<Product> productList = _unitOfWork.Product.GetAll(includeProperties: "Category");
        return View(productList);
    }

    public IActionResult Details(int productId)
    {
        var shoppingCart = new ShoppingCart
        {
            Product = _unitOfWork.Product.Get(
                u => u.Id == productId,
                includeProperties: "Category"),
            Count = 1,
            ProductId = productId
        };

        return shoppingCart.Product == null ? NotFound() : View(shoppingCart);
    }

    [HttpPost]
    [Authorize]
    [ValidateAntiForgeryToken]
    public IActionResult Details(ShoppingCart shoppingCart)
    {
        var claimsIdentity = (ClaimsIdentity?)User.Identity;
        var userId = claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userId))
        {
            return Challenge();
        }

        shoppingCart.ApplicationUserId = userId;

        var cartFromDb = _unitOfWork.ShoppingCart.Get(
            u => u.ApplicationUserId == userId && u.ProductId == shoppingCart.ProductId);

        if (cartFromDb != null)
        {
            cartFromDb.Count += shoppingCart.Count;
            _unitOfWork.ShoppingCart.Update(cartFromDb);
        }
        else
        {
            _unitOfWork.ShoppingCart.Add(shoppingCart);
        }

        _unitOfWork.Save();
        TempData["success"] = "Cart updated successfully";
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
