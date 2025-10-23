using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;

namespace HotelManagement.Controllers
{
    [Authorize]
    public class DashboardController : Controller
	{
		public IActionResult Index()
		{
            if (!(User?.Identity?.IsAuthenticated ?? false))
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
		}
	}
}


