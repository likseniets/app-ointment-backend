using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using app_ointment_backend.Models;
using app_ointment_backend.ViewModels;

namespace app_ointment_backend.Controllers;

public class AppointmentController : Controller
{
    private readonly UserDbContext _userDbContext;

    public AppointmentController(UserDbContext userDbContext)
    {
        _userDbContext = userDbContext; 
    }
    public async Task<IActionResult> Table()
    {
        var appointment = await _userDbContext.Appointments
            .Include(a => a.Client)
            .Include(a => a.Caregiver)
            .AsNoTracking()
            .ToListAsync();
        var appointmentsViewModel = new AppointmentsViewModel(appointment, "Table");
        return View(appointmentsViewModel);
    }
    
    [HttpGet]
    public IActionResult Create()
    {
        var caregivers = _userDbContext.Users
            .Where(u => u.Role == UserRole.Caregiver)
            .Select(u => new { u.UserId, u.Name })
            .ToList();
        ViewBag.CaregiverList = new SelectList(caregivers, "UserId", "Name");
        var clients = _userDbContext.Users
            .Where(u => u.Role == UserRole.Client)
            .Select(u => new { u.UserId, u.Name })
            .ToList();
        ViewBag.ClientList = new SelectList(clients, "UserId", "Name");
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Appointment appointment)
    {
        if (ModelState.IsValid)
        {
            try
            {
                // Basic sanity: referenced users must exist
                bool caregiverExists = await _userDbContext.Users.AnyAsync(u => u.UserId == appointment.CaregiverId && u.Role == UserRole.Caregiver);
                bool clientExists = await _userDbContext.Users.AnyAsync(u => u.UserId == appointment.ClientId && u.Role == UserRole.Client);
                if (!caregiverExists)
                {
                    ModelState.AddModelError("CaregiverId", "Selected caregiver does not exist.");
                }
                if (!clientExists)
                {
                    ModelState.AddModelError("ClientId", "Selected client does not exist.");
                }

                if (ModelState.IsValid)
                {
                    _userDbContext.Appointments.Add(appointment);
                    await _userDbContext.SaveChangesAsync();
                    return RedirectToAction(nameof(Table));
                }
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Unable to save appointment. Try again.");
            }
        }
        
        // Rebuild dropdowns for redisplay
        var caregivers = _userDbContext.Users  
            .Where(u => u.Role == UserRole.Caregiver)
            .Select(u => new { u.UserId, u.Name })
            .ToList();
        ViewBag.CaregiverList = new SelectList(caregivers, "UserId", "Name");
        var clients = _userDbContext.Users
            .Where(u => u.Role == UserRole.Client)
            .Select(u => new { u.UserId, u.Name })
            .ToList();
        ViewBag.ClientList = new SelectList(clients, "UserId", "Name");
        return View(appointment);
    }

    [HttpGet]
    public async Task<IActionResult> Update(int id)
    {
        var appointment = await _userDbContext.Appointments.FindAsync(id);
        if (appointment == null)
        {
            return NotFound();
        }
        // Populate dropdowns for caregiver and client
        var caregivers = _userDbContext.Users
            .Where(u => u.Role == UserRole.Caregiver)
            .Select(u => new { u.UserId, u.Name })
            .ToList();
        ViewBag.CaregiverList = new SelectList(caregivers, "UserId", "Name", appointment.CaregiverId);

        var clients = _userDbContext.Users
            .Where(u => u.Role == UserRole.Client)
            .Select(u => new { u.UserId, u.Name })
            .ToList();
        ViewBag.ClientList = new SelectList(clients, "UserId", "Name", appointment.ClientId);
        return View(appointment);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update([Bind("AppointmentId,Date,CaregiverId,ClientId,Location")] Appointment appointment)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var existing = await _userDbContext.Appointments.FindAsync(appointment.AppointmentId);
                if (existing == null)
                {
                    return NotFound();
                }

                // Update only allowed fields to avoid overwriting non-posted values
                existing.Date = appointment.Date;
                existing.CaregiverId = appointment.CaregiverId;
                existing.ClientId = appointment.ClientId; // kept from hidden field
                existing.Location = appointment.Location;

                await _userDbContext.SaveChangesAsync();
                return RedirectToAction(nameof(Table));
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError(string.Empty, "The appointment was updated by another process. Reload and try again.");
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Unable to save changes. Try again.");
            }
        }

        // Rebuild dropdowns when redisplaying the form
        var caregivers = _userDbContext.Users
            .Where(u => u.Role == UserRole.Caregiver)
            .Select(u => new { u.UserId, u.Name })
            .ToList();
        ViewBag.CaregiverList = new SelectList(caregivers, "UserId", "Name", appointment.CaregiverId);

        var clients = _userDbContext.Users
            .Where(u => u.Role == UserRole.Client)
            .Select(u => new { u.UserId, u.Name })
            .ToList();
        ViewBag.ClientList = new SelectList(clients, "UserId", "Name", appointment.ClientId);
        return View(appointment);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var appointment = await _userDbContext.Appointments.FindAsync(id);
        if (appointment == null)
        {
            return NotFound();
        }
        return View(appointment);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var appointment = await _userDbContext.Appointments.FindAsync(id);
        if (appointment == null)
        {
            return NotFound();
        }
        _userDbContext.Appointments.Remove(appointment);
        await _userDbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Table));
    }
}    
