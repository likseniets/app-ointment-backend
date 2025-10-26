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
    public async Task<IActionResult> Table()
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

        var appointmentsViewModel = new AppointmentsViewModel(appointments, "Table");
        return View(appointmentsViewModel);
    }

    public async Task<IActionResult> TableById(int clientId)
    {
        var appointments = await _appointmentRepository.GetClientAppointment(clientId);
        if (appointments == null)
        {
            _logger.LogError("[AppointmentController] Appointment list not found while executing _appointmentRepository.GetClientAppointment()");
            return NotFound("Appointment list for user not found");
        }

        var roleInt = HttpContext.Session.GetInt32("CurrentUserRole");
        var userId = HttpContext.Session.GetInt32("CurrentUserId");
        if (roleInt.HasValue && userId.HasValue && (UserRole)roleInt.Value == UserRole.Client)
        {
            appointments = appointments.Where(a => a.ClientId == userId.Value);
        }

        var appointmentsViewModel = new AppointmentsViewModel(appointments, "Table");
        ViewBag.ClientId = clientId;
        return View(appointmentsViewModel);
    }

    [HttpGet]
    public IActionResult Create(int? clientId)
    {
        var caregivers = _userDbContext.Users
            .Where(u => u.Role == UserRole.Caregiver)
            .Select(u => new { u.UserId, u.Name })
            .ToList();
        ViewBag.CaregiverList = new SelectList(caregivers, "UserId", "Name");

        var roleInt = HttpContext.Session.GetInt32("CurrentUserRole");
        var userId = HttpContext.Session.GetInt32("CurrentUserId");
        var clientsQuery = _userDbContext.Users.Where(u => u.Role == UserRole.Client);
        if (roleInt.HasValue && userId.HasValue && (UserRole)roleInt.Value == UserRole.Client)
        {
            clientsQuery = clientsQuery.Where(u => u.UserId == userId.Value);
        }
        else if (clientId.HasValue)
        {
            // Admin managing a specific client: narrow to that client and remember it for the view
            clientsQuery = clientsQuery.Where(u => u.UserId == clientId.Value);
            ViewBag.ForClientId = clientId.Value;
        }
        var clients = clientsQuery
            .Select(u => new { u.UserId, u.Name })
            .ToList();
        ViewBag.ClientList = new SelectList(clients, "UserId", "Name");

        // Build a list of 1h available slots across caregivers for clients
        // and for admins creating on behalf of a client
        if ((roleInt.HasValue && (UserRole)roleInt.Value == UserRole.Client) || clientId.HasValue)
        {
            ViewBag.AvailableSlots = BuildAvailableSlotSelectList();
        }

        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Appointment appointment, int? clientId)
    {
        try
        {
            var roleInt = HttpContext.Session.GetInt32("CurrentUserRole");
            var userId = HttpContext.Session.GetInt32("CurrentUserId");
            DateTime startDt;

            if (roleInt.HasValue && userId.HasValue && (UserRole)roleInt.Value == UserRole.Client)
            {
                // Clients can only create for themselves and must pick a slot
                appointment.ClientId = userId.Value;
                var selectedSlot = Request.Form["SelectedSlot"].ToString();
                if (!string.IsNullOrEmpty(selectedSlot))
                {
                    var parts = selectedSlot.Split('|');
                    if (parts.Length == 2 && int.TryParse(parts[0], out var parsedCaregiverId) && DateTime.TryParse(parts[1], null, DateTimeStyles.RoundtripKind, out var parsedStart))
                    {
                        appointment.CaregiverId = parsedCaregiverId;
                        appointment.Date = parsedStart;
                        // Clear any prior model state errors for Date/CaregiverId and re-validate
                        ModelState.Remove(nameof(Appointment.Date));
                        ModelState.Remove(nameof(Appointment.CaregiverId));
                        TryValidateModel(appointment);
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Invalid slot selection.");
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Please select an available slot.");
                }
            }
            else if (clientId.HasValue)
            {
                // Admin creating on behalf of a specific client: mirror client flow
                appointment.ClientId = clientId.Value;
                var selectedSlot = Request.Form["SelectedSlot"].ToString();
                if (!string.IsNullOrEmpty(selectedSlot))
                {
                    var parts = selectedSlot.Split('|');
                    if (parts.Length == 2 && int.TryParse(parts[0], out var parsedCaregiverId) && DateTime.TryParse(parts[1], null, DateTimeStyles.RoundtripKind, out var parsedStart))
                    {
                        appointment.CaregiverId = parsedCaregiverId;
                        appointment.Date = parsedStart;
                        // Clear any prior model state errors for Date/CaregiverId and re-validate
                        ModelState.Remove(nameof(Appointment.Date));
                        ModelState.Remove(nameof(Appointment.CaregiverId));
                        TryValidateModel(appointment);
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Invalid slot selection.");
                    }
                }
                else
                {
                    ModelState.AddModelError(string.Empty, "Please select an available slot.");
                }
            }

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

            // Check that the slot is still free and in availability (best-effort)
            startDt = appointment.Date;
            var endDt = appointment.Date.AddHours(1);
            bool alreadyBooked = await _userDbContext.Appointments.AnyAsync(a => a.CaregiverId == appointment.CaregiverId && a.Date == startDt);
            if (alreadyBooked)
            {
                ModelState.AddModelError(string.Empty, "Selected time slot already booked. Please choose another.");
            }

            if (ModelState.IsValid)
            {
                bool returnOk = await _appointmentRepository.CreateAppointment(appointment);
                if (returnOk)
                {
                    try
                    {
                        // Remove matching availability slot (1h slot)
                        string startStr = startDt.ToString("HH:mm");
                        string endStr = endDt.ToString("HH:mm");
                        var slot = await _userDbContext.Availabilities
                            .FirstOrDefaultAsync(a => a.CaregiverId == appointment.CaregiverId && a.Date.Date == startDt.Date && a.StartTime == startStr && a.EndTime == endStr);
                        if (slot != null)
                        {
                            await _availabilityRepository.DeleteAvailability(slot.AvailabilityId);
                        }
                    }
                    catch { /* ignore best-effort cleanup */ }

                    return RedirectToAction(nameof(Table));
                }
            }
        }
        catch (DbUpdateException)
        {
            ModelState.AddModelError(string.Empty, "Unable to save appointment. Try again.");
        }

        // Rebuild inputs/dropdowns for redisplay
        var caregiversForRedisplay = _userDbContext.Users
            .Where(u => u.Role == UserRole.Caregiver)
            .Select(u => new { u.UserId, u.Name })
            .ToList();
        ViewBag.CaregiverList = new SelectList(caregiversForRedisplay, "UserId", "Name");

        var roleInt2 = HttpContext.Session.GetInt32("CurrentUserRole");
        var userId2 = HttpContext.Session.GetInt32("CurrentUserId");
        var clientsQuery2 = _userDbContext.Users.Where(u => u.Role == UserRole.Client);
        if (roleInt2.HasValue && userId2.HasValue && (UserRole)roleInt2.Value == UserRole.Client)
        {
            clientsQuery2 = clientsQuery2.Where(u => u.UserId == userId2.Value);
            ViewBag.AvailableSlots = BuildAvailableSlotSelectList();
        }
        else if (clientId.HasValue)
        {
            clientsQuery2 = clientsQuery2.Where(u => u.UserId == clientId.Value);
            ViewBag.AvailableSlots = BuildAvailableSlotSelectList();
            ViewBag.ForClientId = clientId.Value;
        }
        var clients2 = clientsQuery2
            .Select(u => new { u.UserId, u.Name })
            .ToList();
        ViewBag.ClientList = new SelectList(clients2, "UserId", "Name");
        return View(appointment);
    }

    [HttpGet]
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update([Bind("AppointmentId,Date,CaregiverId,ClientId,Location")] Appointment appointment, bool returnToManage = false, int? caregiverId = null)
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

                bool returnOk = await _appointmentRepository.UpdateAppointment(appointment);
                if (returnOk)
                {
                    if (returnToManage && caregiverId.HasValue)
                    {
                        return RedirectToAction("Manage", "Availability", new { caregiverId = caregiverId.Value });
                    }
                    return RedirectToAction(nameof(Table));
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
        return View(appointment);
    }

    [HttpGet]
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

    [HttpPost]
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
                    EndTime = endStr,
                    Description = "Reopened from appointment cancellation"
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

        return RedirectToAction(nameof(Table));
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
