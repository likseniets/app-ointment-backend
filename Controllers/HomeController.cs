using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using app_ointment_backend.DAL;
using app_ointment_backend.Models;

namespace app_ointment_backend.Controllers
{
    public class HomeController : Controller
    {
        private readonly IUserRepository _userRepository;

        private const string SessionUserIdKey = "CurrentUserId";
        private const string SessionUserRoleKey = "CurrentUserRole";

        public HomeController(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        // Home page: choose user (mock login)
        public async Task<IActionResult> Index()
        {
            var users = await _userRepository.GetAll();
            return View(users ?? Enumerable.Empty<User>());
        }

        // Select a user and redirect based on role
        [HttpGet]
        public async Task<IActionResult> SelectUser(int id)
        {
            var user = await _userRepository.GetUserById(id);
            if (user == null)
            {
                return NotFound("User not found");
            }

            HttpContext.Session.SetInt32(SessionUserIdKey, user.UserId);
            HttpContext.Session.SetInt32(SessionUserRoleKey, (int)user.Role);

            return user.Role switch
            {
                UserRole.Client => RedirectToAction("Table", "Appointment"),
                UserRole.Caregiver => RedirectToAction("Manage", "Availability", new { caregiverId = user.UserId }),
                UserRole.Admin => RedirectToAction("Table", "User"),
                _ => RedirectToAction("Index")
            };
        }

        // Clear session (logout)
        [HttpGet]
        public IActionResult Logout()
        {
            HttpContext.Session.Clear();
            return RedirectToAction("Index");
        }
    }
}
