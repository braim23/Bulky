using Bulky.DataAccess.Repository.IRepository;
using Bulky.Models;
using Bulky.Models.ViewModels;
using Bulky.Utilities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Stripe.Checkout;
using System.Security.Claims;

namespace BulkyWeb.Areas.Customer.Controllers;

[Area("customer")]
[Authorize]
public class CartController : Controller
{
    private readonly IUnitOfWork _unitOfWork;
    [BindProperty]
    public ShoppingCartViewModel ShoppingCartViewModel { get; set; }

    public CartController(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }



    public IActionResult Index()
    {
        var claimsIdentity = (ClaimsIdentity)User.Identity;
        var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

        ShoppingCartViewModel = new()
        {
            ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId,
            includeProperties: "Product"),
            OrderHeader = new OrderHeader()

        };

        foreach (var cart in ShoppingCartViewModel.ShoppingCartList)
        {
            cart.Price = GetPriceBasedOnQuantity(cart);
            ShoppingCartViewModel.OrderHeader.OrderTotal += (cart.Price * cart.Count);
        }

        return View(ShoppingCartViewModel);
    }

    public IActionResult Summary()
    {
        var claimsIdentity = (ClaimsIdentity)User.Identity;
        var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;

        ShoppingCartViewModel = new()
        {
            ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId,
            includeProperties: "Product"),
            OrderHeader = new OrderHeader()

        };

        ShoppingCartViewModel.OrderHeader.ApplicationUser = _unitOfWork.ApplicationUser.
            GetFirstOrDefault(u => u.Id == userId);
        ShoppingCartViewModel.OrderHeader.Name = ShoppingCartViewModel.OrderHeader.ApplicationUser.Name;
        ShoppingCartViewModel.OrderHeader.PhoneNumber = ShoppingCartViewModel.OrderHeader.ApplicationUser.PhoneNumber;
        ShoppingCartViewModel.OrderHeader.StreetAddress = ShoppingCartViewModel.OrderHeader.ApplicationUser.StreetAddress;
        ShoppingCartViewModel.OrderHeader.City = ShoppingCartViewModel.OrderHeader.ApplicationUser.City;
        ShoppingCartViewModel.OrderHeader.State = ShoppingCartViewModel.OrderHeader.ApplicationUser.State;
        ShoppingCartViewModel.OrderHeader.PostalCode = ShoppingCartViewModel.OrderHeader.ApplicationUser.PostalCode;

        foreach (var cart in ShoppingCartViewModel.ShoppingCartList)
        {
            cart.Price = GetPriceBasedOnQuantity(cart);
            ShoppingCartViewModel.OrderHeader.OrderTotal += (cart.Price * cart.Count);
        }

        return View(ShoppingCartViewModel);
    }
    [HttpPost]
    [ActionName("Summary")]
	public IActionResult SummaryPOST()
	{
		var claimsIdentity = (ClaimsIdentity)User.Identity;
		var userId = claimsIdentity.FindFirst(ClaimTypes.NameIdentifier).Value;


        ShoppingCartViewModel.ShoppingCartList = _unitOfWork.ShoppingCart.GetAll(u => u.ApplicationUserId == userId,
        includeProperties: "Product");

        ShoppingCartViewModel.OrderHeader.OrderDate = System.DateTime.Now;
        ShoppingCartViewModel.OrderHeader.ApplicationUserId = userId;

		ApplicationUser applicationUser = _unitOfWork.ApplicationUser.GetFirstOrDefault(u => u.Id == userId);
		
		foreach (var cart in ShoppingCartViewModel.ShoppingCartList)
		{
			cart.Price = GetPriceBasedOnQuantity(cart);
			ShoppingCartViewModel.OrderHeader.OrderTotal += (cart.Price * cart.Count);
		}

        if(applicationUser.CompanyId.GetValueOrDefault() == 0)
        {
            //regular account
            ShoppingCartViewModel.OrderHeader.PaymentStatus = SD.PaymentStatusPending;
			ShoppingCartViewModel.OrderHeader.OrderStatus = SD.StatusPending;
		}
		else
        {
			//company user, different route
			ShoppingCartViewModel.OrderHeader.PaymentStatus = SD.PaymentStatusDelayedPayment;
			ShoppingCartViewModel.OrderHeader.OrderStatus = SD.StatusApproved;
		}

        _unitOfWork.OrderHeader.Add(ShoppingCartViewModel.OrderHeader);
        _unitOfWork.Save();
        foreach(var cart in ShoppingCartViewModel.ShoppingCartList)
        {
            OrderDetail orderDetail = new()
            {
                ProductId = cart.ProductId,
                OrderHeaderId = ShoppingCartViewModel.OrderHeader.Id,
                Price = cart.Price,
                Count = cart.Count

            };
            _unitOfWork.OrderDetail.Add(orderDetail);
            _unitOfWork.Save();
        }

		if (applicationUser.CompanyId.GetValueOrDefault() == 0)
		{
            //regular customer ... Stripe logic
            var domain = "https://localhost:44365/";
            var options = new SessionCreateOptions
			{
                SuccessUrl = domain + $"customer/cart/OrderConfirmation?id={ShoppingCartViewModel.OrderHeader.Id}",
                CancelUrl = domain + "customer/cart/Index",
				LineItems = new List<SessionLineItemOptions>(),
				Mode = "payment",
			};

            foreach (var item in ShoppingCartViewModel.ShoppingCartList)
            {
                var sessionLineItem = new SessionLineItemOptions
                {
                    PriceData = new SessionLineItemPriceDataOptions
                    {
                        UnitAmount = (long)(item.Price * 100), //$20.50 => 2050
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
            _unitOfWork.OrderHeader.UpdateStripePaymentId(ShoppingCartViewModel.OrderHeader.Id, session.Id, session.PaymentIntentId);
            _unitOfWork.Save();
            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);

		}
		return RedirectToAction(nameof(OrderConfirmation), new { id = ShoppingCartViewModel.OrderHeader.Id });
	}

	public IActionResult OrderConfirmation(int id)
    {
        OrderHeader orderHeader = _unitOfWork.OrderHeader.GetFirstOrDefault(x => x.Id == id, includeProperties: "ApplicationUser");
        if(orderHeader.PaymentStatus!= SD.PaymentStatusDelayedPayment)
        {
            //order by customer
            var service = new SessionService();
            Session session = service.Get(orderHeader.SessionId);
            if(session.PaymentStatus.ToLower() == "paid")
            {
				_unitOfWork.OrderHeader.UpdateStripePaymentId(id, session.Id, session.PaymentIntentId);
                _unitOfWork.OrderHeader.UpdateStatus(id, SD.StatusApproved, SD.PaymentStatusApproved);
                _unitOfWork.Save();
			}
            HttpContext.Session.Clear();
		}
        List<ShoppingCart> shoppingCarts = _unitOfWork.ShoppingCart
            .GetAll(x=>x.ApplicationUserId==orderHeader.ApplicationUserId).ToList();

        _unitOfWork.ShoppingCart.RemoveRange(shoppingCarts);
        _unitOfWork.Save();

        return View(id);
    }
	public IActionResult Plus(int cartId)
    {
        var cartFromDb = _unitOfWork.ShoppingCart.GetFirstOrDefault(x => x.Id == cartId);
        cartFromDb.Count += 1;
        _unitOfWork.ShoppingCart.Update(cartFromDb);
        _unitOfWork.Save();
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Minus(int cartId)
    {
        var cartFromDb = _unitOfWork.ShoppingCart.GetFirstOrDefault(x => x.Id == cartId, tracked: true);
        if (cartFromDb.Count <= 1)
        {
            //remove from cart
            HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart
                .GetAll(u => u.ApplicationUserId == cartFromDb.ApplicationUserId).Count() - 1);
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
        var cartFromDb = _unitOfWork.ShoppingCart.GetFirstOrDefault(x => x.Id == cartId, tracked: true);
        HttpContext.Session.SetInt32(SD.SessionCart, _unitOfWork.ShoppingCart
            .GetAll(u => u.ApplicationUserId == cartFromDb.ApplicationUserId).Count() - 1);
        _unitOfWork.ShoppingCart.Remove(cartFromDb);
        _unitOfWork.Save();
        return RedirectToAction(nameof(Index));
    }




    private double GetPriceBasedOnQuantity(ShoppingCart shoppingCart)
    {
        if (shoppingCart.Count <= 50)
        {
            return shoppingCart.Product.Price;
        }
        else if (shoppingCart.Count <= 100)
        {
            return shoppingCart.Product.Price50;
        }
        else
        {
            return shoppingCart.Product.Price100;
        }
    }
}
