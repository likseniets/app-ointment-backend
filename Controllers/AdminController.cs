using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using app_ointment_backend.Data;
using app_ointment_backend.Models;

namespace app_ointment_backend.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;

    public AdminController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: /Admin or /Admin/Index
    public async Task<IActionResult> Index()
    {
        // Statistics
        ViewBag.TotalUsers = await _context.Users.CountAsync();
        ViewBag.TotalHealthcarePersonnel = await _context.Users.CountAsync(u => u.Role == UserRole.HealthcarePersonnel);
        ViewBag.TotalElderly = await _context.Users.CountAsync(u => u.Role == UserRole.Elderly);
        ViewBag.TotalAppointments = await _context.Appointments.CountAsync();
        ViewBag.ScheduledAppointments = await _context.Appointments.CountAsync(a => a.Status == AppointmentStatus.Scheduled);
        ViewBag.CompletedAppointments = await _context.Appointments.CountAsync(a => a.Status == AppointmentStatus.Completed);
        ViewBag.TotalAvailableDays = await _context.AvailableDays.CountAsync();
        ViewBag.TotalTasks = await _context.AppointmentTasks.CountAsync();

        // Recent activity
        var recentUsers = await _context.Users
            .OrderByDescending(u => u.CreatedAt)
            .Take(5)
            .ToListAsync();

        var recentAppointments = await _context.Appointments
            .Include(a => a.ElderlyUser)
            .Include(a => a.HealthcarePersonnel)
            .OrderByDescending(a => a.CreatedAt)
            .Take(5)
            .ToListAsync();

        ViewBag.RecentUsers = recentUsers;
        ViewBag.RecentAppointments = recentAppointments;

        return View();
    }

    // GET: /Admin/Users
    public async Task<IActionResult> Users()
    {
        var users = await _context.Users
            .OrderBy(u => u.Role)
            .ThenBy(u => u.Name)
            .ToListAsync();
        return View(users);
    }

    // GET: /Admin/Appointments
    public async Task<IActionResult> Appointments()
    {
        var appointments = await _context.Appointments
            .Include(a => a.ElderlyUser)
            .Include(a => a.HealthcarePersonnel)
            .Include(a => a.Tasks)
            .OrderBy(a => a.AppointmentDate)
            .ToListAsync();
        
        appointments = appointments
            .OrderBy(a => a.AppointmentDate)
            .ThenBy(a => a.StartTime)
            .ToList();
        
        return View(appointments);
    }

    // GET: /Admin/AvailableDays
    public async Task<IActionResult> AvailableDays()
    {
        var availableDays = await _context.AvailableDays
            .Include(a => a.HealthcarePersonnel)
            .OrderBy(a => a.Date)
            .ToListAsync();
        
        availableDays = availableDays
            .OrderBy(a => a.Date)
            .ThenBy(a => a.StartTime)
            .ToList();
        
        return View(availableDays);
    }

    // GET: /Admin/DeleteUser/{id}
    public async Task<IActionResult> DeleteUser(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var user = await _context.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }

        return View(user);
    }

    // POST: /Admin/DeleteUser/{id}
    [HttpPost, ActionName("DeleteUser")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteUserConfirmed(int id)
    {
        var user = await _context.Users.FindAsync(id);
        if (user != null)
        {
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Users));
    }
}

