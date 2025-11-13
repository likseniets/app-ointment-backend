using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using app_ointment_backend.Models;
using app_ointment_backend.ViewModels;
using app_ointment_backend.DAL;
using System.Globalization;

namespace app_ointment_backend.Controllers;

// Controller setup based on course demos
[ApiController]
[Route("api/[controller]")]
public class AppointmentController : Controller
{
    private readonly UserDbContext _userDbContext;
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IAvailabilityRepository _availabilityRepository;
    private readonly ILogger<AppointmentController> _logger;

    public AppointmentController(UserDbContext userDbContext, IAppointmentRepository appointmentRepository, IAvailabilityRepository availabilityRepository, ILogger<AppointmentController> logger)
    {
        _userDbContext = userDbContext;
        _appointmentRepository = appointmentRepository;
        _availabilityRepository = availabilityRepository;
        _logger = logger;
    }

    [HttpGet]
    public async Task<ActionResult<IEnumerable<Appointment>>> GetAll()
    {
        var appointments = await _appointmentRepository.GetAll();
        if (appointments == null)
        {
            _logger.LogError("[AppointmentController] Appointment list not found while executing _appointmentRepository.GetAll()");
            return NotFound("Appointment list not found");
        }

        var roleInt = HttpContext.Session.GetInt32("CurrentUserRole");
        var userId = HttpContext.Session.GetInt32("CurrentUserId");
        if (roleInt.HasValue && userId.HasValue && (UserRole)roleInt.Value == UserRole.Client)
        {
            appointments = appointments.Where(a => a.ClientId == userId.Value);
        }

        return Ok(appointments);
    }

    [HttpGet("byclient/{clientId:int}")]
    public async Task<IActionResult> GetById(int clientId)
    {
        var appointments = await _appointmentRepository.GetClientAppointment(clientId);
        if (appointments == null)
        {
            _logger.LogError("[AppointmentController] Appointment list not found while executing _appointmentRepository.GetClientAppointment()");
            return NotFound("Appointment list for user not found");
        }

        appointments = appointments.Where(a => a.ClientId == clientId);
        var appointmentDtos = appointments.Select(AppointmentDto.FromAppointment);

        return Ok(appointmentDtos);
    }

    [HttpGet("bycaregiver/{caregiverId:int}")]
    public async Task<IActionResult> GetByCaregiver(int caregiverId)
    {
        var appointments = await _appointmentRepository.GetCaregiverAppointments(caregiverId);
        if (appointments == null)
        {
            _logger.LogError("[AppointmentController] Appointment list not found while executing _appointmentRepository.GetCaregiverAppointments()");
            return NotFound("Appointment list for caregiver not found");
        }

        var appointmentDtos = appointments.Select(AppointmentDto.FromAppointment);
        return Ok(appointmentDtos);
    }

    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentDto appointmentDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        try
        {
            // Get the availability slot
            var availability = await _availabilityRepository.GetAvailabilityById(appointmentDto.AvailabilityId);
            if (availability == null)
            {
                return BadRequest(new { message = "Selected availability slot not found" });
            }

            // Verify client exists
            var client = await _userDbContext.Users.FirstOrDefaultAsync(u => u.UserId == appointmentDto.ClientId && u.Role == UserRole.Client);
            if (client == null)
            {
                return BadRequest(new { message = "Client not found" });
            }

            // Verify caregiver exists
            var caregiver = await _userDbContext.Users.FirstOrDefaultAsync(u => u.UserId == availability.CaregiverId && u.Role == UserRole.Caregiver);
            if (caregiver == null)
            {
                return BadRequest(new { message = "Caregiver not found" });
            }

            // Parse the availability time to create the appointment datetime
            if (!TimeSpan.TryParse(availability.StartTime, out var startTime))
            {
                return BadRequest(new { message = "Invalid availability time format" });
            }

            var appointmentDate = availability.Date.Date + startTime;

            // Check if slot is already booked
            bool alreadyBooked = await _userDbContext.Appointments.AnyAsync(a =>
                a.CaregiverId == availability.CaregiverId &&
                a.Date == appointmentDate);
            if (alreadyBooked)
            {
                return BadRequest(new { message = "Selected time slot is already booked" });
            }

            // Create the appointment
            var appointment = new Appointment
            {
                Date = appointmentDate,
                CaregiverId = availability.CaregiverId,
                ClientId = appointmentDto.ClientId,
                Location = appointmentDto.Location,
                Description = appointmentDto.Description
            };

            bool created = await _appointmentRepository.CreateAppointment(appointment);
            if (created)
            {
                // Remove the availability slot
                await _availabilityRepository.DeleteAvailability(appointmentDto.AvailabilityId);

                // Get all appointments for the client and return them
                var clientAppointments = await _appointmentRepository.GetClientAppointment(appointmentDto.ClientId);
                if (clientAppointments != null)
                {
                    var appointmentDtos = clientAppointments
                        .OrderBy(a => a.Date)
                        .Select(AppointmentDto.FromAppointment);
                    return Ok(appointmentDtos);
                }
                return Ok(new { message = "Appointment created successfully" });
            }

            return BadRequest(new { message = "Failed to create appointment" });
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError("[AppointmentController] Failed to create appointment: {Error}", ex.Message);
            return BadRequest(new { message = "Unable to save appointment. Try again." });
        }
    }

    [HttpGet("update/{id}")]
    public async Task<IActionResult> Update(int id, bool returnToManage = false, int? caregiverId = null)
    {
        var appointment = await _appointmentRepository.GetAppointmentById(id);
        if (appointment == null)
        {
            _logger.LogError("[AppointmentController] appointment not found when updating the AppointmentId {AppointmentId:0000}", id);
            return BadRequest("Appointment not found");
        }
        var roleInt = HttpContext.Session.GetInt32("CurrentUserRole");
        var userId = HttpContext.Session.GetInt32("CurrentUserId");
        if (roleInt.HasValue && userId.HasValue && (UserRole)roleInt.Value == UserRole.Client && appointment.ClientId != userId.Value)
        {
            return Forbid();
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
        ViewBag.ReturnToManage = returnToManage;
        ViewBag.ManageCaregiverId = caregiverId;
        return View(appointment);
    }

    [HttpPost("update")]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult<Appointment>> Update([Bind("AppointmentId,Date,CaregiverId,ClientId,Location,Description")] Appointment appointment, bool returnToManage = false, int? caregiverId = null)
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

                var roleInt = HttpContext.Session.GetInt32("CurrentUserRole");
                var userId = HttpContext.Session.GetInt32("CurrentUserId");
                if (roleInt.HasValue && userId.HasValue && (UserRole)roleInt.Value == UserRole.Client && existing.ClientId != userId.Value)
                {
                    return Forbid();
                }

                // Update only allowed fields to avoid overwriting non-posted values
                existing.Date = appointment.Date;
                existing.CaregiverId = appointment.CaregiverId;
                existing.ClientId = appointment.ClientId; // kept from hidden field
                existing.Location = appointment.Location;
                existing.Description = appointment.Description;
                bool returnOk = await _appointmentRepository.UpdateAppointment(existing);
                if (returnOk)
                {
                    if (returnToManage && caregiverId.HasValue)
                    {
                        return RedirectToAction("Manage", "Availability", new { caregiverId = caregiverId.Value });
                    }
                    return existing;
                }
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
        ViewBag.ReturnToManage = returnToManage;
        ViewBag.ManageCaregiverId = caregiverId;
        return appointment;
    }

    [HttpDelete("delete/{id}")]
    public async Task<IActionResult> Delete(int id, bool returnToManage = false, int? caregiverId = null)
    {
        var appointment = await _appointmentRepository.GetAppointmentById(id);
        if (appointment == null)
        {
            _logger.LogError("[AppointmentController] appointment not found for Id {AppointmentId:0000}", id);
            return BadRequest("Not found");
        }
        var roleInt = HttpContext.Session.GetInt32("CurrentUserRole");
        var userId = HttpContext.Session.GetInt32("CurrentUserId");
        if (roleInt.HasValue && userId.HasValue && (UserRole)roleInt.Value == UserRole.Client && appointment.ClientId != userId.Value)
        {
            return Forbid();
        }
        ViewBag.ReturnToManage = returnToManage;
        ViewBag.ManageCaregiverId = caregiverId;
        return View(appointment);
    }

    [HttpDelete("deleteconfirmed/{id}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id, bool returnToManage = false, int? caregiverId = null)
    {
        var existing = await _appointmentRepository.GetAppointmentById(id);
        if (existing == null)
        {
            return NotFound();
        }
        var roleInt = HttpContext.Session.GetInt32("CurrentUserRole");
        var userId = HttpContext.Session.GetInt32("CurrentUserId");
        if (roleInt.HasValue && userId.HasValue && (UserRole)roleInt.Value == UserRole.Client && existing.ClientId != userId.Value)
        {
            return Forbid();
        }
        bool returnOk = await _appointmentRepository.DeleteAppointment(id);
        if (!returnOk)
        {
            _logger.LogError("[AppointmentController] Appointment deletion failed for the Id {AppointmentId:0000}", id);
            return BadRequest("Appointment deletion failed");
        }

        // Re-open the corresponding availability slot (best-effort)
        try
        {
            var start = existing.Date;
            var end = start.AddHours(1);
            string startStr = start.ToString("HH:mm");
            string endStr = end.ToString("HH:mm");

            bool exists = await _userDbContext.Availabilities.AnyAsync(a => a.CaregiverId == existing.CaregiverId && a.Date.Date == start.Date && a.StartTime == startStr && a.EndTime == endStr);
            if (!exists)
            {
                var newSlot = new Availability
                {
                    CaregiverId = existing.CaregiverId,
                    Date = start.Date,
                    StartTime = startStr,
                    EndTime = endStr
                };
                await _availabilityRepository.CreateAvailability(newSlot);
            }
        }
        catch { /* ignore slot recreation failures */ }

        if (returnToManage && (caregiverId.HasValue || existing != null))
        {
            var caregiverToUse = caregiverId ?? existing.CaregiverId;
            return RedirectToAction("Manage", "Availability", new { caregiverId = caregiverToUse });
        }

        return NoContent();
    }

    // Build a select list of available 1-hour slots across all caregivers
    private List<SelectListItem> BuildAvailableSlotSelectList()
    {
        var now = DateTime.Now;
        var slots = new List<(int CaregiverId, string CaregiverName, DateTime Start)>();
        var availabilities = _userDbContext.Availabilities
            .Include(a => a.Caregiver)
            .AsNoTracking()
            .ToList();

        foreach (var a in availabilities)
        {
            if (!TimeSpan.TryParse(a.StartTime, out var startTs) || !TimeSpan.TryParse(a.EndTime, out var endTs))
                continue;
            for (var t = startTs; t + TimeSpan.FromHours(1) <= endTs; t = t + TimeSpan.FromHours(1))
            {
                var slotStart = a.Date.Date + t;
                if (slotStart < now) continue;
                bool booked = _userDbContext.Appointments.Any(ap => ap.CaregiverId == a.CaregiverId && ap.Date == slotStart);
                if (booked) continue;
                slots.Add((a.CaregiverId, a.Caregiver?.Name ?? $"Caregiver #{a.CaregiverId}", slotStart));
            }
        }

        var items = slots
            .OrderBy(s => s.Start)
            .Select(s => new SelectListItem
            {
                Value = $"{s.CaregiverId}|{s.Start:O}",
                Text = $"{s.Start:yyyy-MM-dd HH:mm} - {s.Start.AddHours(1):HH:mm} — {s.CaregiverName}"
            })
            .ToList();
        return items;
    }
}
