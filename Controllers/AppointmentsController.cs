using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using app_ointment_backend.Data;
using app_ointment_backend.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;

namespace app_ointment_backend.Controllers;

public class AppointmentsController : Controller
{
    private readonly ApplicationDbContext _context;

    public AppointmentsController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: Appointments
    public async Task<IActionResult> Index()
    {
        var appointments = await _context.Appointments
            .Include(a => a.ElderlyUser)
            .Include(a => a.HealthcarePersonnel)
            .Include(a => a.Tasks)
            .OrderBy(a => a.AppointmentDate)
            .ToListAsync();
        
        // Sort by time on client side since SQLite doesn't support TimeSpan ordering
        appointments = appointments
            .OrderBy(a => a.AppointmentDate)
            .ThenBy(a => a.StartTime)
            .ToList();
        
        return View(appointments);
    }

    // GET: Appointments/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var appointment = await _context.Appointments
            .Include(a => a.ElderlyUser)
            .Include(a => a.HealthcarePersonnel)
            .Include(a => a.Tasks)
            .FirstOrDefaultAsync(m => m.Id == id);
        
        if (appointment == null)
        {
            return NotFound();
        }

        return View(appointment);
    }

    // GET: Appointments/Create
    public async Task<IActionResult> Create()
    {
        var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        var currentUser = await _context.Users.FindAsync(userId);
        
        if (currentUser == null)
        {
            return RedirectToAction("Login", "Auth");
        }

        ViewBag.CurrentUser = currentUser;
        ViewBag.ElderlyUsers = _context.Users.Where(u => u.Role == UserRole.Elderly).ToList();
        ViewBag.HealthcarePersonnel = _context.Users.Where(u => u.Role == UserRole.HealthcarePersonnel).ToList();
        return View();
    }

    // POST: Appointments/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("ElderlyUserId,HealthcarePersonnelId,AppointmentDate,StartTime,EndTime,Notes")] Appointment appointment)
    {
        // Remove validation errors for navigation properties
        ModelState.Remove("ElderlyUser");
        ModelState.Remove("HealthcarePersonnel");
        ModelState.Remove("Tasks");
        
        if (ModelState.IsValid)
        {
            // Check if healthcare personnel is available at the requested time
            var appointmentDate = appointment.AppointmentDate.Date;
            
            // Get all available days for this healthcare personnel (SQLite limitation - can't translate TimeSpan comparisons)
            var allAvailableDays = await _context.AvailableDays
                .Where(ad => 
                    ad.HealthcarePersonnelId == appointment.HealthcarePersonnelId &&
                    !ad.IsBooked)
                .ToListAsync();
            
            // Filter by date and time on client side (SQLite limitation)
            var availableDay = allAvailableDays
                .Where(ad => 
                    ad.Date.Date == appointmentDate &&
                    ad.StartTime <= appointment.StartTime &&
                    ad.EndTime >= appointment.EndTime)
                .FirstOrDefault();
            
            if (availableDay == null)
            {
                // Use the already fetched available days and filter for suggestions
                var availableDaysList = allAvailableDays
                    .Where(ad => ad.Date >= DateTime.Today)
                    .OrderBy(ad => ad.Date)
                    .ThenBy(ad => ad.StartTime)
                    .Take(10)
                    .ToList();
                
                var healthcarePersonnel = await _context.Users.FindAsync(appointment.HealthcarePersonnelId);
                
                TempData["ErrorMessage"] = $"{healthcarePersonnel?.Name} er ikke tilgjengelig på valgt tidspunkt.";
                TempData["AvailableDays"] = System.Text.Json.JsonSerializer.Serialize(availableDaysList.Select(ad => new {
                    Date = ad.Date.ToString("yyyy-MM-dd"),
                    StartTime = ad.StartTime.ToString(@"hh\:mm"),
                    EndTime = ad.EndTime.ToString(@"hh\:mm"),
                    Notes = ad.Notes
                }).ToList());
                
                var userId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
                var currentUser = await _context.Users.FindAsync(userId);
                ViewBag.CurrentUser = currentUser;
                ViewBag.ElderlyUsers = _context.Users.Where(u => u.Role == UserRole.Elderly).ToList();
                ViewBag.HealthcarePersonnel = _context.Users.Where(u => u.Role == UserRole.HealthcarePersonnel).ToList();
                return View(appointment);
            }
            
            // Mark the available day as booked
            availableDay.IsBooked = true;
            
            appointment.Status = AppointmentStatus.Scheduled;
            appointment.CreatedAt = DateTime.Now;
            _context.Add(appointment);
            await _context.SaveChangesAsync();
            
            TempData["SuccessMessage"] = "Avtale opprettet!";
            return RedirectToAction(nameof(Index));
        }
        
        var currentUserId = int.Parse(User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? "0");
        var user = await _context.Users.FindAsync(currentUserId);
        ViewBag.CurrentUser = user;
        ViewBag.ElderlyUsers = _context.Users.Where(u => u.Role == UserRole.Elderly).ToList();
        ViewBag.HealthcarePersonnel = _context.Users.Where(u => u.Role == UserRole.HealthcarePersonnel).ToList();
        return View(appointment);
    }

    // GET: Appointments/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var appointment = await _context.Appointments.FindAsync(id);
        if (appointment == null)
        {
            return NotFound();
        }
        
        ViewBag.ElderlyUsers = _context.Users.Where(u => u.Role == UserRole.Elderly).ToList();
        ViewBag.HealthcarePersonnel = _context.Users.Where(u => u.Role == UserRole.HealthcarePersonnel).ToList();
        return View(appointment);
    }

    // POST: Appointments/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,ElderlyUserId,HealthcarePersonnelId,AppointmentDate,StartTime,EndTime,Status,Notes,CreatedAt")] Appointment appointment)
    {
        if (id != appointment.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(appointment);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AppointmentExists(appointment.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        
        ViewBag.ElderlyUsers = _context.Users.Where(u => u.Role == UserRole.Elderly).ToList();
        ViewBag.HealthcarePersonnel = _context.Users.Where(u => u.Role == UserRole.HealthcarePersonnel).ToList();
        return View(appointment);
    }

    // GET: Appointments/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var appointment = await _context.Appointments
            .Include(a => a.ElderlyUser)
            .Include(a => a.HealthcarePersonnel)
            .Include(a => a.Tasks)
            .FirstOrDefaultAsync(m => m.Id == id);
        
        if (appointment == null)
        {
            return NotFound();
        }

        return View(appointment);
    }

    // POST: Appointments/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var appointment = await _context.Appointments.FindAsync(id);
        if (appointment != null)
        {
            _context.Appointments.Remove(appointment);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool AppointmentExists(int id)
    {
        return _context.Appointments.Any(e => e.Id == id);
    }
}

