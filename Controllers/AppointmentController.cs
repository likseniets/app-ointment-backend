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
    public async Task<IActionResult> Create(int? SelectedCaregiverId, DateTime? SelectedDate, int? SelectedClientId)
    {
        var vm = await BuildCreateViewModel(SelectedCaregiverId, SelectedDate, SelectedClientId);
        return View(vm);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(app_ointment_backend.ViewModels.CreateAppointmentViewModel vm)
    {
        // Basic field presence checks
        if (!vm.SelectedCaregiverId.HasValue)
        {
            ModelState.AddModelError("SelectedCaregiverId", "Please select a caregiver.");
        }
        if (!vm.SelectedClientId.HasValue)
        {
            ModelState.AddModelError("SelectedClientId", "Please select a client.");
        }
        if (!vm.SelectedDate.HasValue)
        {
            ModelState.AddModelError("SelectedDate", "Please select a date.");
        }
        if (string.IsNullOrWhiteSpace(vm.SelectedTime))
        {
            ModelState.AddModelError("SelectedTime", "Please select a time slot.");
        }

        if (ModelState.IsValid)
        {
            try
            {
                int caregiverId = vm.SelectedCaregiverId!.Value;
                int clientId = vm.SelectedClientId!.Value;
                DateTime day = vm.SelectedDate!.Value.Date;
                if (!TimeSpan.TryParse(vm.SelectedTime, out var timeOfDay))
                {
                    ModelState.AddModelError("SelectedTime", "Invalid time format.");
                }
                else
                {
                    var appointmentDate = day.Add(timeOfDay);

                    // Sanity: users exist
                    bool caregiverExists = await _userDbContext.Users.AnyAsync(u => u.UserId == caregiverId && u.Role == UserRole.Caregiver);
                    bool clientExists = await _userDbContext.Users.AnyAsync(u => u.UserId == clientId && u.Role == UserRole.Client);
                    if (!caregiverExists)
                    {
                        ModelState.AddModelError("SelectedCaregiverId", "Selected caregiver does not exist.");
                    }
                    if (!clientExists)
                    {
                        ModelState.AddModelError("SelectedClientId", "Selected client does not exist.");
                    }

                    // Whole hour check
                    if (appointmentDate.Minute != 0 || appointmentDate.Second != 0)
                    {
                        ModelState.AddModelError("SelectedTime", "Appointments must start on the hour (e.g., 09:00).");
                    }

                    // Availability check
                    var windows = await _userDbContext.Availabilities
                        .Where(a => a.CaregiverId == caregiverId && a.Date.Date == day)
                        .ToListAsync();
                    bool withinAvailability = windows.Any(w =>
                    {
                        bool startOk = TimeSpan.TryParse(w.StartTime, out var s);
                        bool endOk = TimeSpan.TryParse(w.EndTime, out var e);
                        if (!startOk || !endOk) return false;
                        var startHour = s.Minutes > 0 ? s.Hours + 1 : s.Hours;
                        var endExclusive = e.Hours;
                        return appointmentDate.Hour >= startHour && appointmentDate.Hour < endExclusive;
                    });
                    if (!withinAvailability)
                    {
                        ModelState.AddModelError("SelectedTime", "Selected time is outside the caregiver's availability.");
                    }

                    // Double-booked check
                    bool alreadyBooked = await _userDbContext.Appointments
                        .AnyAsync(a => a.CaregiverId == caregiverId && a.Date.Date == day && a.Date.Hour == appointmentDate.Hour);
                    if (alreadyBooked)
                    {
                        ModelState.AddModelError("SelectedTime", "Selected time slot is already booked.");
                    }

                    if (ModelState.IsValid)
                    {
                        var appointment = new Appointment
                        {
                            CaregiverId = caregiverId,
                            ClientId = clientId,
                            Date = appointmentDate,
                            Location = vm.Location
                        };

                        bool returnOk = await _appointmentRepository.CreateAppointment(appointment);
                        if (returnOk)
                            return RedirectToAction(nameof(Table));
                        ModelState.AddModelError(string.Empty, "Unable to save appointment. Try again.");
                    }
                }
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Unable to save appointment. Try again.");
            }
        }

<<<<<<< HEAD
        // Rebuild view model for redisplay with timeslots
        var rebuilt = await BuildCreateViewModel(vm.SelectedCaregiverId, vm.SelectedDate, vm.SelectedClientId);
        rebuilt.SelectedTime = vm.SelectedTime;
        rebuilt.Location = vm.Location;
        return View(rebuilt);
    }

    private async Task<ViewModels.CreateAppointmentViewModel> BuildCreateViewModel(int? caregiverId, DateTime? date, int? clientId)
    {
        var caregivers = _userDbContext.Users
=======
        // Rebuilds dropdowns for redisplay
        /* var caregivers = _userDbContext.Users  
>>>>>>> f6ca18230721bd73b6eabbf28ccb3f25231ddc0d
            .Where(u => u.Role == UserRole.Caregiver)
            .Select(u => new SelectListItem { Value = u.UserId.ToString(), Text = u.Name, Selected = caregiverId.HasValue && caregiverId.Value == u.UserId })
            .ToList();
        var clients = _userDbContext.Users
            .Where(u => u.Role == UserRole.Client)
            .Select(u => new SelectListItem { Value = u.UserId.ToString(), Text = u.Name, Selected = clientId.HasValue && clientId.Value == u.UserId })
            .ToList();

        var vm = new ViewModels.CreateAppointmentViewModel
        {
            SelectedCaregiverId = caregiverId,
            SelectedClientId = clientId,
            SelectedDate = date?.Date,
            Caregivers = caregivers,
            Clients = clients,
            TimeSlots = new List<SelectListItem>(),
            DateHasAvailability = false
        };

        if (caregiverId.HasValue && date.HasValue)
        {
            vm.TimeSlots = await ComputeTimeSlots(caregiverId.Value, date.Value.Date, clientId);
            vm.DateHasAvailability = vm.TimeSlots.Any();
        }

        return vm;
    }

    private async Task<IEnumerable<SelectListItem>> ComputeTimeSlots(int caregiverId, DateTime day, int? clientId)
    {
        var items = new List<SelectListItem>();
        var windows = await _userDbContext.Availabilities
            .Where(a => a.CaregiverId == caregiverId && a.Date.Date == day.Date)
            .ToListAsync();

        var slotHours = new HashSet<int>();
        foreach (var w in windows)
        {
            if (!TimeSpan.TryParse(w.StartTime, out var s)) continue;
            if (!TimeSpan.TryParse(w.EndTime, out var e)) continue;
            var startHour = s.Minutes > 0 ? s.Hours + 1 : s.Hours;
            var endExclusive = e.Hours;
            for (int h = startHour; h < endExclusive; h++)
            {
                if (h >= 0 && h <= 23) slotHours.Add(h);
            }
        }

        var existing = await _userDbContext.Appointments
            .Where(a => a.CaregiverId == caregiverId && a.Date.Date == day.Date)
            .Select(a => new { a.Date, a.ClientId })
            .ToListAsync();

        foreach (var h in slotHours.OrderBy(h => h))
        {
            var bookedBy = existing.FirstOrDefault(x => x.Date.Hour == h)?.ClientId;
            bool isBooked = bookedBy != null;
            bool bySelf = clientId.HasValue && bookedBy == clientId.Value;
            var label = new TimeSpan(h, 0, 0).ToString(@"hh\:mm");
            if (isBooked) label += bySelf ? " (booked by you)" : " (booked)";
            items.Add(new SelectListItem
            {
                Text = label,
                Value = isBooked ? string.Empty : new TimeSpan(h, 0, 0).ToString(@"hh\:mm"),
                Disabled = isBooked
            });
        }

        return items;
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
