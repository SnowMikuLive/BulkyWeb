using System.Security.Claims;
using BulkyWeb.DataAccess.Repository.IRepository;
using BulkyWeb.Models;
using BulkyWeb.Models.ViewModels;
using BulkyWeb.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe.Checkout;

namespace BulkyWeb.Areas.Customer.Controllers;

[Area("Customer")]
[Authorize]
public class CartController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;

    public CartController(IUnitOfWork unitOfWork, IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
    }

    [BindProperty]
    public ShoppingCartVM ShoppingCartVM { get; set; } = new();

    public IActionResult Index()
    {
        var userId = GetUserId();
        ShoppingCartVM = new ShoppingCartVM
        {
            ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(
                u => u.ApplicationUserId == userId,
                includeProperties: "Product"),
            OrderHeader = new OrderHeader()
        };

        foreach (var cart in ShoppingCartVM.ShoppingCartList)
        {
            cart.Price = GetPriceBasedOnQuantity(cart);
            ShoppingCartVM.OrderHeader.OrderTotal += cart.Price * cart.Count;
        }

        return View(ShoppingCartVM);
    }

    public IActionResult Summary()
    {
        var userId = GetUserId();
        ShoppingCartVM = new ShoppingCartVM
        {
            ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(
                u => u.ApplicationUserId == userId,
                includeProperties: "Product"),
            OrderHeader = new OrderHeader()
        };

        ShoppingCartVM.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);
        if (ShoppingCartVM.OrderHeader.ApplicationUser == null)
        {
            return NotFound();
        }

        ShoppingCartVM.OrderHeader.Name = ShoppingCartVM.OrderHeader.ApplicationUser.Name;
        ShoppingCartVM.OrderHeader.PhoneNumber = ShoppingCartVM.OrderHeader.ApplicationUser.PhoneNumber ?? string.Empty;
        ShoppingCartVM.OrderHeader.StreetAddress = ShoppingCartVM.OrderHeader.ApplicationUser.StreetAddress;
        ShoppingCartVM.OrderHeader.City = ShoppingCartVM.OrderHeader.ApplicationUser.City;
        ShoppingCartVM.OrderHeader.State = ShoppingCartVM.OrderHeader.ApplicationUser.State;
        ShoppingCartVM.OrderHeader.PostalCode = ShoppingCartVM.OrderHeader.ApplicationUser.PostalCode;

        foreach (var cart in ShoppingCartVM.ShoppingCartList)
        {
            cart.Price = GetPriceBasedOnQuantity(cart);
            ShoppingCartVM.OrderHeader.OrderTotal += cart.Price * cart.Count;
        }

        return View(ShoppingCartVM);
    }

    [HttpPost]
    [ActionName("Summary")]
    [ValidateAntiForgeryToken]
    public IActionResult SummaryPOST()
    {
        var userId = GetUserId();
        ShoppingCartVM.ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(
            u => u.ApplicationUserId == userId,
            includeProperties: "Product");

        ShoppingCartVM.OrderHeader.OrderDate = DateTime.Now;
        ShoppingCartVM.OrderHeader.ApplicationUserId = userId;

        foreach (var cart in ShoppingCartVM.ShoppingCartList)
        {
            cart.Price = GetPriceBasedOnQuantity(cart);
            ShoppingCartVM.OrderHeader.OrderTotal += cart.Price * cart.Count;
        }

        var applicationUser = _unitOfWork.ApplicationUser.Get(u => u.Id == userId);
        if (applicationUser == null)
        {
            return NotFound();
        }

        if (applicationUser.CompanyId.GetValueOrDefault() == 0)
        {
            if (string.IsNullOrWhiteSpace(_configuration["Stripe:SecretKey"]))
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Stripe SecretKey is missing. Add your Stripe test SecretKey in appsettings.json before placing a customer order.");
                return View("Summary", ShoppingCartVM);
            }

            ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
            ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusPending;
        }
        else
        {
            ShoppingCartVM.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
            ShoppingCartVM.OrderHeader.OrderStatus = SD.StatusApproved;
            ShoppingCartVM.OrderHeader.PaymentDueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30));
        }

        _unitOfWork.OrderHeader.Add(ShoppingCartVM.OrderHeader);
        _unitOfWork.Save();

        foreach (var cart in ShoppingCartVM.ShoppingCartList)
        {
            var orderDetail = new OrderDetail
            {
                ProductId = cart.ProductId,
                OrderHeaderId = ShoppingCartVM.OrderHeader.Id,
                Price = cart.Price,
                Count = cart.Count
            };
            _unitOfWork.OrderDetail.Add(orderDetail);
            _unitOfWork.Save();
        }

        if (applicationUser.CompanyId.GetValueOrDefault() != 0)
        {
            return RedirectToAction(nameof(OrderConfirmation), new { id = ShoppingCartVM.OrderHeader.Id });
        }

        var domain = $"{Request.Scheme}://{Request.Host.Value}/";
        var options = new SessionCreateOptions
        {
            SuccessUrl = domain + $"Customer/Cart/OrderConfirmation?id={ShoppingCartVM.OrderHeader.Id}",
            CancelUrl = domain + "Customer/Cart/Index",
            LineItems = new List<SessionLineItemOptions>(),
            Mode = "payment"
        };

        foreach (var item in ShoppingCartVM.ShoppingCartList)
        {
            var sessionLineItem = new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    UnitAmount = (long)(item.Price * 100),
                    Currency = "nzd",
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = item.Product?.Title ?? "Product"
                    }
                },
                Quantity = item.Count
            };
            options.LineItems.Add(sessionLineItem);
        }

        var service = new SessionService();
        var session = service.Create(options);

        _unitOfWork.OrderHeader.UpdateStripePaymentID(ShoppingCartVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
        _unitOfWork.Save();

        Response.Headers.Append("Location", session.Url);
        return new StatusCodeResult(303);
    }

    public IActionResult OrderConfirmation(int id)
    {
        var orderHeader = _unitOfWork.OrderHeader.Get(
            u => u.Id == id,
            includeProperties: "ApplicationUser");

        if (orderHeader == null)
        {
            return NotFound();
        }

        if (orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment && !string.IsNullOrEmpty(orderHeader.SessionId))
        {
            var service = new SessionService();
            var session = service.Get(orderHeader.SessionId);

            if (session.PaymentStatus.Equals("paid", StringComparison.OrdinalIgnoreCase))
            {
                _unitOfWork.OrderHeader.UpdateStripePaymentID(id, session.Id, session.PaymentIntentId);
                _unitOfWork.OrderHeader.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
                _unitOfWork.Save();
            }
        }

        var shoppingCarts = _unitOfWork.ShoppingCart
            .GetAll(u => u.ApplicationUserId == orderHeader.ApplicationUserId)
            .ToList();

        _unitOfWork.ShoppingCart.RemoveRange(shoppingCarts);
        _unitOfWork.Save();
        HttpContext.Session.Clear();

        return View(id);
    }

    public IActionResult Plus(int cartId)
    {
        var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId);
        if (cartFromDb == null)
        {
            return NotFound();
        }

        cartFromDb.Count += 1;
        _unitOfWork.ShoppingCart.Update(cartFromDb);
        _unitOfWork.Save();
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Minus(int cartId)
    {
        var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId);
        if (cartFromDb == null)
        {
            return NotFound();
        }

        if (cartFromDb.Count <= 1)
        {
            _unitOfWork.ShoppingCart.Remove(cartFromDb);
        }
        else
        {
            cartFromDb.Count -= 1;
            _unitOfWork.ShoppingCart.Update(cartFromDb);
        }

        _unitOfWork.Save();
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Remove(int cartId)
    {
        var cartFromDb = _unitOfWork.ShoppingCart.Get(u => u.Id == cartId);
        if (cartFromDb == null)
        {
            return NotFound();
        }

        _unitOfWork.ShoppingCart.Remove(cartFromDb);
        _unitOfWork.Save();
        return RedirectToAction(nameof(Index));
    }

    private double GetPriceBasedOnQuantity(ShoppingCart shoppingCart)
    {
        if (shoppingCart.Product == null)
        {
            return 0;
        }

        if (shoppingCart.Count <= 50)
        {
            return shoppingCart.Product.Price;
        }

        if (shoppingCart.Count <= 100)
        {
            return shoppingCart.Product.Price50;
        }

        return shoppingCart.Product.Price100;
    }

    private string GetUserId()
    {
        var claimsIdentity = (ClaimsIdentity?)User.Identity;
        return claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    }
}
