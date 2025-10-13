using Microsoft.AspNetCore.Mvc;

namespace app_ointment_backend.Controllers
{
    public class HomeController : Controller
    {
        // GET: /<controller>/
        public IActionResult Index()
        {
            return View();
        }
    }
}