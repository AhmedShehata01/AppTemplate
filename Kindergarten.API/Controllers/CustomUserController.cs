using Microsoft.AspNetCore.Mvc;

namespace Kindergarten.API.Controllers
{
    public class CustomUserController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
