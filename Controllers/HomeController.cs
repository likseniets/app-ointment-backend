using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using app_ointment_backend.Data;
using app_ointment_backend.Models;
using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace app_ointment_backend.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ILogger<HomeController> _logger;

    public HomeController(ApplicationDbContext context, ILogger<HomeController> logger)
    {
        _context = context;
        _logger = logger;
    }

    public IActionResult Index()
    {
        // Redirect to login if not authenticated
        if (!User.Identity?.IsAuthenticated == true)
        {
            return RedirectToAction("Login", "Auth");
        }
        
        return RedirectToAction("Dashboard");
    }

    [Authorize]
    public async Task<IActionResult> Dashboard()
    {
        var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
        var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
        
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        ViewBag.CurrentUser = user;

        // Redirect admin users to admin dashboard
        if (user.Role == UserRole.Admin)
        {
            return RedirectToAction("Index", "Admin");
        }

        if (user.Role == UserRole.Elderly)
        {
            // Elderly user dashboard
            var myAppointments = await _context.Appointments
                .Include(a => a.HealthcarePersonnel)
                .Include(a => a.Tasks)
                .Where(a => a.ElderlyUserId == userId && a.AppointmentDate >= DateTime.Today)
                .OrderBy(a => a.AppointmentDate)
                .Take(5)
                .ToListAsync();
            
            myAppointments = myAppointments
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.StartTime)
                .ToList();

            ViewBag.MyAppointments = myAppointments;
            return View("ElderlyDashboard");
        }
        else
        {
            // Healthcare personnel dashboard
            var myAppointments = await _context.Appointments
                .Include(a => a.ElderlyUser)
                .Include(a => a.Tasks)
                .Where(a => a.HealthcarePersonnelId == userId && a.AppointmentDate >= DateTime.Today)
                .OrderBy(a => a.AppointmentDate)
                .Take(5)
                .ToListAsync();
            
            myAppointments = myAppointments
                .OrderBy(a => a.AppointmentDate)
                .ThenBy(a => a.StartTime)
                .ToList();

            var myAvailableDays = await _context.AvailableDays
                .Where(a => a.HealthcarePersonnelId == userId && a.Date >= DateTime.Today)
                .OrderBy(a => a.Date)
                .Take(5)
                .ToListAsync();

            ViewBag.MyAppointments = myAppointments;
            ViewBag.MyAvailableDays = myAvailableDays;
            return View("HealthcareDashboard");
        }
    }

    public IActionResult Privacy()
    {
        return View();
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
