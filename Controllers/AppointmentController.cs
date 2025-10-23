using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using app_ointment_backend.Models;
using app_ointment_backend.ViewModels;
using app_ointment_backend.DAL;

namespace app_ointment_backend.Controllers;

// Controller setup based on course demos
public class AppointmentController : Controller
{
    private readonly UserDbContext _userDbContext;
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly ILogger<AppointmentController> _logger;

    public AppointmentController(UserDbContext userDbContext, IAppointmentRepository appointmentRepository, ILogger<AppointmentController> logger)
    {
        _userDbContext = userDbContext;
        _appointmentRepository = appointmentRepository;
        _logger = logger;
    }
    public async Task<IActionResult> Table()
    {
        var appointments = await _appointmentRepository.GetAll();
        if (appointments == null)
        {
            _logger.LogError("[AppointmentController] Appointment list not found while executing _appointmentRepository.GetAll()");
            return NotFound("Appointment list not found");
        }
        var appointmentsViewModel = new AppointmentsViewModel(appointments, "Table");
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
                    bool returnOk = await _appointmentRepository.CreateAppointment(appointment);
                    if (returnOk)
                        return RedirectToAction(nameof(Table));
                }
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Unable to save appointment. Try again.");
            }
        }

        // Rebuilds dropdowns for redisplay
        /* var caregivers = _userDbContext.Users  
            .Where(u => u.Role == UserRole.Caregiver)
            .Select(u => new { u.UserId, u.Name })
            .ToList();
        ViewBag.CaregiverList = new SelectList(caregivers, "UserId", "Name");
        var clients = _userDbContext.Users
            .Where(u => u.Role == UserRole.Client)
            .Select(u => new { u.UserId, u.Name })
            .ToList();
        ViewBag.ClientList = new SelectList(clients, "UserId", "Name"); */
        return View(appointment);
    }

    [HttpGet]
    public async Task<IActionResult> Update(int id)
    {
        var appointment = await _appointmentRepository.GetAppointmentById(id);
        if (appointment == null)
        {
            _logger.LogError("[AppointmentController] appointment not found when updating the AppointmentId {AppointmentId:0000}", id);
            return BadRequest("Appointment not found");
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
                var existing = await _appointmentRepository.GetAppointmentById(appointment.AppointmentId);
                if (existing == null)
                {
                    _logger.LogError("[AppointmentController] appointment not found for AppointmentId");
                    return NotFound();
                }

                // Update only allowed fields to avoid overwriting non-posted values
                existing.Date = appointment.Date;
                existing.CaregiverId = appointment.CaregiverId;
                existing.ClientId = appointment.ClientId; // kept from hidden field
                existing.Location = appointment.Location;

                bool returnOk = await _appointmentRepository.UpdateAppointment(appointment);
                if (returnOk)
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

        // Rebuild dropdowns when redisplaying the form. Without this, the app does not return to the appointments table and does not save the update.
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
        var appointment = await _appointmentRepository.GetAppointmentById(id);
        if (appointment == null)
        {
            _logger.LogError("[AppointmentController] appointment not found for Id {AppointmentId:0000}", id);
            return BadRequest("Not found");
        }
        return View(appointment);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        /* var appointment = await _appointmentRepository.GetAppointmentById(id);
        if (appointment == null)
        {
            return NotFound();
        } */
        bool returnOk = await _appointmentRepository.DeleteAppointment(id);
        if (!returnOk)
        {
            _logger.LogError("[AppointmentController] Appointment deletion failed for the Id {AppointmentId:0000}", id);
            return BadRequest("Appointment deletion failed");
        }
        return RedirectToAction(nameof(Table));
    }
}    
