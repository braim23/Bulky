using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using Stripe.Climate;
using System.Diagnostics;
using System.Security.Claims;

namespace BulkyWeb.Areas.Admin.Controllers;
[Area("Admin")]
[Authorize]
public class OrderController : Controller
{

	private readonly IUnitOfWork _unitOfWork;
    [BindProperty]
    public OrderViewModel OrderViewModel { get; set; }
    public OrderController(IUnitOfWork db)
	{
		_unitOfWork = db;
	}
	public IActionResult Index()
	{
		return View();
	}
    public IActionResult Details(int orderId)
    {
        OrderViewModel = new()
        {
            OrderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(u => u.Id == orderId, includeProperties: "ApplicationUser"),
            OrderDetail = _unitOfWork.OrderDetail.GetAll(u=>u.OrderHeaderId == orderId, includeProperties: "Product")
        };
        return View(OrderViewModel);
    }
    [HttpPost]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    public IActionResult UpdateOrderDetail()
    {
        var orderHeaderFromDb = _unitOfWork.OrderHeader.GetFirstOrDefault(x => x.Id == OrderViewModel.OrderHeader.Id);
        orderHeaderFromDb.Name = OrderViewModel.OrderHeader.Name;
        orderHeaderFromDb.PhoneNumber = OrderViewModel.OrderHeader.PhoneNumber;
        orderHeaderFromDb.StreetAddress = OrderViewModel.OrderHeader.StreetAddress;
        orderHeaderFromDb.City = OrderViewModel.OrderHeader.City;
        orderHeaderFromDb.State = OrderViewModel.OrderHeader.State;
        orderHeaderFromDb.PostalCode = OrderViewModel.OrderHeader.PostalCode;
        if (!string.IsNullOrEmpty(OrderViewModel.OrderHeader.Carrier))
        {
            orderHeaderFromDb.Carrier = OrderViewModel.OrderHeader.Carrier;
        }
        if (!string.IsNullOrEmpty(OrderViewModel.OrderHeader.TrackingNumber))
        {
            orderHeaderFromDb.TrackingNumber = OrderViewModel.OrderHeader.TrackingNumber;
        }
        _unitOfWork.OrderHeader.Update(orderHeaderFromDb);
        _unitOfWork.Save();
        TempData["Sucess"] = "Order Details Updated Successfully!";
        return RedirectToAction(nameof(Details), new {orderId = orderHeaderFromDb.Id});
    }

    [HttpPost]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    public IActionResult StartProcessing()
    {
        _unitOfWork.OrderHeader.UpdateStatus(OrderViewModel.OrderHeader.Id, SD.StatusInProcess);
        _unitOfWork.Save();
        TempData["success"] = "Order Details Updated Successfully!";

        return RedirectToAction(nameof(Details), new { orderId = OrderViewModel.OrderHeader.Id });


    }
    [HttpPost]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    public IActionResult ShipOrder()
    {

        var orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(x=>x.Id == OrderViewModel.OrderHeader.Id);
        orderHeader.TrackingNumber = OrderViewModel.OrderHeader.TrackingNumber;
        orderHeader.Carrier = OrderViewModel.OrderHeader.Carrier;
        orderHeader.OrderStatus = SD.StatusShipped;
        orderHeader.ShippingDate = DateTime.Now;
        if(orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
        {
            orderHeader.PaymentDueDate = DateTime.Now.AddDays(30);
        }
        _unitOfWork.OrderHeader.Update(orderHeader);
        _unitOfWork.Save();
        TempData["success"] = "Order Shipped Successfully.";
        return RedirectToAction(nameof(Details), new { orderId = OrderViewModel.OrderHeader.Id });

    }

    [HttpPost]
    [Authorize(Roles = SD.Role_Admin + "," + SD.Role_Employee)]
    public IActionResult CancelOrder()
    {
        var orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(x=>x.Id==OrderViewModel.OrderHeader.Id);
        if(orderHeader.PaymentStatus == SD.PaymentStatusApproved)
        {
            var options = new RefundCreateOptions
            {
                Reason = RefundReasons.RequestedByCustomer,
                PaymentIntent = orderHeader.PaymentIntentId
            };

            var service = new RefundService();
            Refund refund = service.Create(options);

            _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusRefunded);
        } 
        else
        {
            _unitOfWork.OrderHeader.UpdateStatus(orderHeader.Id, SD.StatusCancelled, SD.StatusCancelled);
        }

        _unitOfWork.Save();
        TempData["success"] = "Order Cancelled Successfully.";
        return RedirectToAction(nameof(Details), new { orderId = OrderViewModel.OrderHeader.Id });
    }
    [ActionName("Details")]
    [HttpPost]
    public IActionResult Details_PAY_NOW()
    {
        OrderViewModel.OrderHeader = _unitOfWork.OrderHeader
                .GetFirstOrDefault(u => u.Id == OrderViewModel.OrderHeader.Id, includeProperties: "ApplicationUser");
        OrderViewModel.OrderDetail = _unitOfWork.OrderDetail
            .GetAll(u => u.OrderHeaderId == OrderViewModel.OrderHeader.Id, includeProperties: "Product");

        //stripe logic
        var domain = "https://localhost:44365/";
        var options = new SessionCreateOptions
        {
            SuccessUrl = domain + $"admin/order/PaymentConfirmation?orderHeaderId={OrderViewModel.OrderHeader.Id}",
            CancelUrl = domain + $"admin/order/details?orderId={OrderViewModel.OrderHeader.Id}",
            LineItems = new List<SessionLineItemOptions>(),
            Mode = "payment",
        };

        foreach (var item in OrderViewModel.OrderDetail)
        {
            var sessionLineItem = new SessionLineItemOptions
            {
                PriceData = new SessionLineItemPriceDataOptions
                {
                    UnitAmount = (long)(item.Price * 100), // $20.50 => 2050
                    Currency = "usd",
                    ProductData = new SessionLineItemPriceDataProductDataOptions
                    {
                        Name = item.Product.Title
                    }
                },
                Quantity = item.Count
            };
            options.LineItems.Add(sessionLineItem);
        }


        var service = new SessionService();
        Session session = service.Create(options);
        _unitOfWork.OrderHeader.UpdateStripePaymentId(OrderViewModel.OrderHeader.Id, session.Id, session.PaymentIntentId);
        _unitOfWork.Save();
        Response.Headers.Add("Location", session.Url);
        return new StatusCodeResult(303);
    }

    public IActionResult PaymentConfirmation(int orderHeaderId)
    {
        OrderHeader orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(x => x.Id == orderHeaderId);
        if (orderHeader.PaymentStatus == SD.PaymentStatusDelayedPayment)
        {
            //order by company
            var service = new SessionService();
            Session session = service.Get(orderHeader.SessionId);
            if (session.PaymentStatus.ToLower() == "paid")
            {
                _unitOfWork.OrderHeader.UpdateStripePaymentId(orderHeaderId, session.Id, session.PaymentIntentId);
                _unitOfWork.OrderHeader.UpdateStatus(orderHeaderId, orderHeader.OrderStatus, SD.PaymentStatusApproved);
                _unitOfWork.Save();
            }
        }

        return View(orderHeaderId);
    }

    #region API CALLS
    [HttpGet]
	public IActionResult GetAll(string status)
	{
        IEnumerable<OrderHeader> objOrderheaders;

        if(User.IsInRole(SD.Role_Admin) || User.IsInRole(SD.Role_Employee))
        {
            objOrderheaders = _unitOfWork.OrderHeader.GetAll(includeProperties: "ApplicationUser").ToList();
        }
        else
        {
            var claimsIdentity = (ClaimsIdentity)User.Identity;
            var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

            objOrderheaders = _unitOfWork.OrderHeader.GetAll(x => x.ApplicationUser.Id == userId, includeProperties: "ApplicationUser");

        }

        switch (status)
		{
            case "pending":
				objOrderheaders = objOrderheaders.Where(x => x.PaymentStatus == SD.PaymentStatusDelayedPayment);
                break;
            case "inprocess":
                objOrderheaders = objOrderheaders.Where(x => x.OrderStatus == SD.StatusInProcess);
                break;
            case "completed":
                objOrderheaders = objOrderheaders.Where(x => x.OrderStatus == SD.StatusShipped);
                break;
            case "approved":
                objOrderheaders = objOrderheaders.Where(x => x.OrderStatus == SD.PaymentStatusApproved);
                break;
            default:
                break;
            }

		return Json(new { data = objOrderheaders });
	}

	#endregion
}
