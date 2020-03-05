using System;
using MediatR;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.eShopWeb.Infrastructure.Data;

namespace Microsoft.eShopWeb.Web.Features.MyOrders
{
    [ApiExplorerSettings(IgnoreApi = true)]
    [Authorize] // Controllers that mainly require Authorization still use Controller/View; other pages use Pages
    public class GetMyOrdersController : Controller
    {
        private readonly IMediator _mediator;

        public GetMyOrdersController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet("/order/my-orders")]
        public async Task<IActionResult> MyOrders()
        {
            var viewModel = await _mediator.Send(new GetMyOrders(User.Identity.Name));
            if (HttpContext.Request.Headers["Accept"] == "application/json")
            {
                return Ok(viewModel);
            }
            else
            {
                return View("/Features/MyOrders/GetMyOrders.cshtml", viewModel);    
            }
        }
    }

    public class GetMyOrders : IRequest<MyOrdersViewModel>
    {
        public string UserName { get; set; }

        public GetMyOrders(string userName)
        {
            UserName = userName;
        }
    }
    
    public class MyOrdersViewModel
    {
        public IEnumerable<OrderSummaryViewModel> Orders { get; set; }
    }
    
    public class OrderSummaryViewModel
    {
        private const string DEFAULT_STATUS = "Pending";

        public int OrderNumber { get; set; }
        public DateTimeOffset OrderDate { get; set; }
        public decimal Total { get; set; }
        public string Status => DEFAULT_STATUS;
    }
    
    public class GetMyOrdersHandler : IRequestHandler<GetMyOrders, MyOrdersViewModel>
    {
        private readonly CatalogContext _db;

        public GetMyOrdersHandler(CatalogContext db)
        {
            _db = db;
        }

        public async Task<MyOrdersViewModel> Handle(GetMyOrders request, CancellationToken cancellationToken)
        {
            var result = new MyOrdersViewModel();
            result.Orders = await _db.Orders
                .Include(x => x.OrderItems)
                .Where(x => x.BuyerId == request.UserName)
                .Select(o => new OrderSummaryViewModel
                {
                    OrderDate = o.OrderDate,
                    OrderNumber = o.Id,
                    Total = o.OrderItems.Sum(x => x.Units * x.UnitPrice),
                })
                .ToArrayAsync(cancellationToken: cancellationToken);
            
            return result;
        }
    }
}
