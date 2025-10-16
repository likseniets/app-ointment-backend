using Microsoft.AspNetCore.Mvc;
using app_ointment_backend.DAL;


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