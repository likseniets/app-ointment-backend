using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using app_ointment_backend.Data;
using app_ointment_backend.Models;
using System.Diagnostics;

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

    public async Task<IActionResult> Index()
    {
        // Get today's and upcoming appointments
        var upcomingAppointments = await _context.Appointments
            .Include(a => a.ElderlyUser)
            .Include(a => a.HealthcarePersonnel)
            .Include(a => a.Tasks)
            .Where(a => a.AppointmentDate >= DateTime.Today)
            .OrderBy(a => a.AppointmentDate)
            .Take(5)
            .ToListAsync();
        
        // Sort by time on client side since SQLite doesn't support TimeSpan ordering
        upcomingAppointments = upcomingAppointments
            .OrderBy(a => a.AppointmentDate)
            .ThenBy(a => a.StartTime)
            .ToList();

        ViewBag.UpcomingAppointments = upcomingAppointments;
        ViewBag.TotalUsers = await _context.Users.CountAsync();
        ViewBag.TotalAppointments = await _context.Appointments.CountAsync();
        ViewBag.UpcomingCount = upcomingAppointments.Count;
        
        return View();
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
