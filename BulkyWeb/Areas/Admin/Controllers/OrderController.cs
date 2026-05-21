using System.Security.Claims;
using BulkyWeb.DataAccess.Repository.IRepository;
using BulkyWeb.Models;
using BulkyWeb.Models.ViewModels;
using BulkyWeb.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;

namespace BulkyWeb.Areas.Admin.Controllers;

[Area("Admin")]
[Authorize]
public class OrderController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IConfiguration _configuration;

    public OrderController(IUnitOfWork unitOfWork, IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _configuration = configuration;
    }

    [BindProperty]
    public OrderVM OrderVM { get; set; } = new();

    public IActionResult Index()
    {
        return View();
    }

    public IActionResult Details(int orderId)
    {
        var orderVm = LoadOrderVM(orderId);
        if (orderVm == null)
        {
            return NotFound();
        }

        if (!CanAccessOrder(orderVm.OrderHeader.ApplicationUserId))
        {
            return Forbid();
        }

        OrderVM = orderVm;
        return View(OrderVM);
    }

    [HttpPost]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    public IActionResult UpdateOrderDetail()
    {
        var orderHeaderFromDb = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id);
        if (orderHeaderFromDb == null || !CanAccessOrder(orderHeaderFromDb.ApplicationUserId))
        {
            return NotFound();
        }

        orderHeaderFromDb.Name = OrderVM.OrderHeader.Name;
        orderHeaderFromDb.PhoneNumber = OrderVM.OrderHeader.PhoneNumber;
        orderHeaderFromDb.StreetAddress = OrderVM.OrderHeader.StreetAddress;
        orderHeaderFromDb.City = OrderVM.OrderHeader.City;
        orderHeaderFromDb.State = OrderVM.OrderHeader.State;
        orderHeaderFromDb.PostalCode = OrderVM.OrderHeader.PostalCode;

        if (!string.IsNullOrWhiteSpace(OrderVM.OrderHeader.Carrier))
        {
            orderHeaderFromDb.Carrier = OrderVM.OrderHeader.Carrier;
        }

        if (!string.IsNullOrWhiteSpace(OrderVM.OrderHeader.TrackingNumber))
        {
            orderHeaderFromDb.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
        }

        _unitOfWork.OrderHeader.Update(orderHeaderFromDb);
        _unitOfWork.Save();
        TempData["success"] = "Order Details Updated Successfully.";
        return RedirectToAction(nameof(Details), new { orderId = orderHeaderFromDb.Id });
    }

    [HttpPost]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    public IActionResult StartProcessing()
    {
        var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id);
        if (orderHeader == null || !CanAccessOrder(orderHeader.ApplicationUserId))
        {
            return NotFound();
        }

        _unitOfWork.OrderHeader.UpdateStatus(OrderVM.OrderHeader.Id, SD.StatusInProcess);
        _unitOfWork.Save();
        TempData["success"] = "Order status updated successfully.";
        return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });
    }

    [HttpPost]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    public IActionResult ShipOrder()
    {
        var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id);
        if (orderHeader == null || !CanAccessOrder(orderHeader.ApplicationUserId))
        {
            return NotFound();
        }

        orderHeader.TrackingNumber = OrderVM.OrderHeader.TrackingNumber;
        orderHeader.Carrier = OrderVM.OrderHeader.Carrier;
        orderHeader.OrderStatus = SD.StatusShipped;
        orderHeader.ShippingDate = DateTime.Now;

        if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
        {
            orderHeader.PaymentDueDate = DateOnly.FromDateTime(DateTime.Now.AddDays(30));
        }

        _unitOfWork.OrderHeader.Update(orderHeader);
        _unitOfWork.Save();
        TempData["success"] = "Order Shipped Successfully.";
        return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });
    }

    [HttpPost]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    public IActionResult CancelOrder()
    {
        var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == OrderVM.OrderHeader.Id);
        if (orderHeader == null || !CanAccessOrder(orderHeader.ApplicationUserId))
        {
            return NotFound();
        }

        if (orderHeader.PaymentStatus == SD.PaymentStatusApproved && !string.IsNullOrWhiteSpace(orderHeader.PaymentIntentId))
        {
            var options = new RefundCreateOptions
            {
                Reason = RefundReasons.RequestedByCustomer,
                PaymentIntent = orderHeader.PaymentIntentId
            };

            var service = new RefundService();
            service.Create(options);
            _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusRefunded);
        }
        else
        {
            _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusCancelled);
        }

        _unitOfWork.Save();
        TempData["success"] = "Order Cancelled Successfully.";
        return RedirectToAction(nameof(Details), new { orderId = OrderVM.OrderHeader.Id });
    }

    [HttpPost]
    [ActionName("Details")]
    [Authorize(Roles = SD.Role_Company)]
    public IActionResult DetailsPayNow()
    {
        var orderHeader = _unitOfWork.OrderHeader.Get(
            u => u.Id == OrderVM.OrderHeader.Id,
            includeProperties: "ApplicationUser");

        if (orderHeader == null || !CanAccessOrder(orderHeader.ApplicationUserId))
        {
            return NotFound();
        }

        if (orderHeader.PaymentStatus != SD.PaymentStatusDelayedPayment || orderHeader.OrderStatus != SD.StatusShipped)
        {
            TempData["error"] = "This order is not eligible for payment.";
            return RedirectToAction(nameof(Details), new { orderId = orderHeader.Id });
        }

        OrderVM = new OrderVM
        {
            OrderHeader = orderHeader,
            OrderDetail = _unitOfWork.OrderDetail.GetAll(
                u => u.OrderHeaderId == OrderVM.OrderHeader.Id,
                includeProperties: "Product")
        };

        if (string.IsNullOrWhiteSpace(_configuration["Stripe:SecretKey"]))
        {
            ModelState.AddModelError(
                string.Empty,
                "Stripe SecretKey is missing. Add your Stripe test SecretKey in appsettings.json before paying this order.");
            return View(OrderVM);
        }

        var domain = $"{Request.Scheme}://{Request.Host.Value}/";
        var options = new SessionCreateOptions
        {
            SuccessUrl = domain + $"Admin/Order/PaymentConfirmation?orderHeaderId={OrderVM.OrderHeader.Id}",
            CancelUrl = domain + $"Admin/Order/Details?orderId={OrderVM.OrderHeader.Id}",
            LineItems = new List<SessionLineItemOptions>(),
            Mode = "payment"
        };

        foreach (var item in OrderVM.OrderDetail)
        {
            options.LineItems.Add(new SessionLineItemOptions
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
            });
        }

        var service = new SessionService();
        var session = service.Create(options);

        _unitOfWork.OrderHeader.UpdateStripePaymentID(OrderVM.OrderHeader.Id, session.Id, session.PaymentIntentId);
        _unitOfWork.Save();

        Response.Headers.Append("Location", session.Url);
        return new StatusCodeResult(303);
    }

    public IActionResult PaymentConfirmation(int orderHeaderId)
    {
        var orderHeader = _unitOfWork.OrderHeader.Get(u => u.Id == orderHeaderId);
        if (orderHeader == null)
        {
            return NotFound();
        }

        if (!CanAccessOrder(orderHeader.ApplicationUserId))
        {
            return Forbid();
        }

        if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment && !string.IsNullOrWhiteSpace(orderHeader.SessionId))
        {
            var service = new SessionService();
            var session = service.Get(orderHeader.SessionId);

            if (session.PaymentStatus.Equals("paid", StringComparison.OrdinalIgnoreCase))
            {
                _unitOfWork.OrderHeader.UpdateStripePaymentID(orderHeaderId, session.Id, session.PaymentIntentId);
                _unitOfWork.OrderHeader.UpdateStatus(orderHeaderId, orderHeader.OrderStatus ?? SD.StatusShipped, SD.PaymentStatusApproved);
                _unitOfWork.Save();
            }
        }

        return View(orderHeaderId);
    }

    #region API CALLS

    [HttpGet]
    public IActionResult GetAll(string status)
    {
        IEnumerable<OrderHeader> objOrderHeaders;

        if (User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
        {
            objOrderHeaders = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser");
        }
        else
        {
            var userId = GetUserId();
            objOrderHeaders = _unitOfWork.OrderHeader.GetAll(
                u => u.ApplicationUserId == userId,
                includeProperties: "ApplicationUser");
        }

        objOrderHeaders = status switch
        {
            "pending" => objOrderHeaders.Where(u => u.OrderStatus == SD.StatusPending),
            "inprocess" => objOrderHeaders.Where(u => u.OrderStatus == SD.StatusInProcess),
            "completed" => objOrderHeaders.Where(u => u.OrderStatus == SD.StatusShipped),
            "approved" => objOrderHeaders.Where(u => u.OrderStatus == SD.StatusApproved),
            _ => objOrderHeaders
        };

        return Json(new { data = objOrderHeaders });
    }

    #endregion

    private OrderVM? LoadOrderVM(int orderId)
    {
        var orderHeader = _unitOfWork.OrderHeader.Get(
            u => u.Id == orderId,
            includeProperties: "ApplicationUser");

        if (orderHeader == null)
        {
            return null;
        }

        return new OrderVM
        {
            OrderHeader = orderHeader,
            OrderDetail = _unitOfWork.OrderDetail.GetAll(
                u => u.OrderHeaderId == orderId,
                includeProperties: "Product")
        };
    }

    private string GetUserId()
    {
        var claimsIdentity = (ClaimsIdentity?)User.Identity;
        return claimsIdentity?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
    }

    private bool CanAccessOrder(string applicationUserId)
    {
        return User.IsInRole(SD.Role_Admin) ||
               User.IsInRole(SD.Role_Employee) ||
               applicationUserId == GetUserId();
    }
}
