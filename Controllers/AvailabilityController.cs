using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using app_ointment_backend.Models;
using app_ointment_backend.DAL;

namespace app_ointment_backend.Controllers;

public class AvailabilityController : Controller
{
    private readonly IAvailabilityRepository _availabilityRepository;
    private readonly UserDbContext _userDbContext;
    private readonly ILogger<AvailabilityController> _logger;

    public AvailabilityController(
        IAvailabilityRepository availabilityRepository,
        UserDbContext userDbContext,
        ILogger<AvailabilityController> logger)
    {
        _availabilityRepository = availabilityRepository;
        _userDbContext = userDbContext;
        _logger = logger;
    }

    // GET: Availability/Manage/{caregiverId}
    // If caregiverId is not provided or is 0, show the first caregiver found.
    public async Task<IActionResult> Manage(int? caregiverId)
    {
        Caregiver? caregiver = null;

        var roleInt = HttpContext.Session.GetInt32("CurrentUserRole");
        var userId = HttpContext.Session.GetInt32("CurrentUserId");
        var isCaregiver = roleInt.HasValue && (UserRole)roleInt.Value == UserRole.Caregiver;

        // If caregiver is logged in, always show their own availability
        if (isCaregiver && userId.HasValue)
        {
            caregiverId = userId.Value;
        }

        if (caregiverId.HasValue && caregiverId.Value > 0)
        {
            caregiver = await _userDbContext.Caregivers
                .Include(c => c.Availability)
                .FirstOrDefaultAsync(c => c.UserId == caregiverId.Value && c.Role == UserRole.Caregiver);
        }
        else
        {
            caregiver = await _userDbContext.Caregivers
                .Include(c => c.Availability)
                .FirstOrDefaultAsync(c => c.Role == UserRole.Caregiver);
        }

        if (caregiver == null)
        {
            _logger.LogError("[AvailabilityController] Caregiver not found for Id {CaregiverId}", caregiverId ?? 0);
            // No caregiver available — redirect to the users table so the user can create one.
            return RedirectToAction("Table", "User");
        }

        // Also load this caregiver's booked appointments for display
        var appts = await _userDbContext.Appointments
            .Include(a => a.Client)
            .Where(a => a.CaregiverId == caregiver.UserId)
            .OrderBy(a => a.Date)
            .ToListAsync();
        ViewBag.CaregiverAppointments = appts;

        return View(caregiver);
    }

    // POST: Availability/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(Availability availability)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var roleInt = HttpContext.Session.GetInt32("CurrentUserRole");
                var userId = HttpContext.Session.GetInt32("CurrentUserId");
                if (roleInt.HasValue && (UserRole)roleInt.Value == UserRole.Caregiver && userId.HasValue)
                {
                    // Enforce caregiver can only create for self
                    availability.CaregiverId = userId.Value;
                }
                var caregiver = await _userDbContext.Users
                    .FirstOrDefaultAsync(c => c.UserId == availability.CaregiverId && c.Role == UserRole.Caregiver);

                if (caregiver == null)
                {
                    return NotFound("Caregiver not found");
                }

                // Parse range and split into 1-hour slots on the hour
                if (!TimeSpan.TryParse(availability.StartTime, out var startTs) ||
                    !TimeSpan.TryParse(availability.EndTime, out var endTs))
                {
                    ModelState.AddModelError(string.Empty, "Invalid time format. Please use HH:mm");
                    return View(availability);
                }
                if (startTs >= endTs)
                {
                    ModelState.AddModelError(string.Empty, "End time must be after start time");
                    return View(availability);
                }
                if (startTs.Minutes != 0 || endTs.Minutes != 0)
                {
                    ModelState.AddModelError(string.Empty, "Times must be on the hour (e.g., 09:00)");
                    return View(availability);
                }

                var created = 0;
                for (var t = startTs; t + TimeSpan.FromHours(1) <= endTs; t += TimeSpan.FromHours(1))
                {
                    var slotStart = new TimeSpan(t.Hours, 0, 0).ToString(@"hh\:mm");
                    var slotEnd = new TimeSpan((t + TimeSpan.FromHours(1)).Hours, 0, 0).ToString(@"hh\:mm");
                    bool exists = await _userDbContext.Availabilities.AnyAsync(a => a.CaregiverId == availability.CaregiverId && a.Date.Date == availability.Date.Date && a.StartTime == slotStart && a.EndTime == slotEnd);
                    if (exists) continue;
                    var slot = new Availability
                    {
                        CaregiverId = availability.CaregiverId,
                        Date = availability.Date.Date,
                        StartTime = slotStart,
                        EndTime = slotEnd,
                        Description = availability.Description
                    };
                    await _availabilityRepository.CreateAvailability(slot);
                    created++;
                }
                if (created > 0)
                {
                    return RedirectToAction(nameof(Manage), new { caregiverId = availability.CaregiverId });
                }
                ModelState.AddModelError(string.Empty, "No new slots created (may already exist)");
            }
            catch (DbUpdateException)
            {
                // fall through to redirect back to Manage
            }
        }

        // On failure or invalid model, return the Create view so validation messages can be displayed.
        return View(availability);
    }

    // POST: Availability/CreateInline
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> CreateInline([FromForm] int caregiverId, [FromForm] DateTime date, [FromForm] string startTime, [FromForm] string endTime, [FromForm] string description)
    {
        // Debug logging to see what we're receiving
        _logger.LogInformation("[AvailabilityController] Received availability: Day={Day}, Start={Start}, End={End}, CaregiverId={Id}, Description={Description}",
            date, startTime, endTime, caregiverId, description);

        var availability = new Availability
        {
            CaregiverId = caregiverId,
            Date = date,
            StartTime = startTime,
            EndTime = endTime,
            Description = description
        };

        // If a caregiver is logged in, enforce self-only
        var roleInt = HttpContext.Session.GetInt32("CurrentUserRole");
        var userId = HttpContext.Session.GetInt32("CurrentUserId");
        if (roleInt.HasValue && userId.HasValue && (UserRole)roleInt.Value == UserRole.Caregiver)
        {
            availability.CaregiverId = userId.Value;
        }

        if (!ModelState.IsValid)
        {
            var errors = string.Join(" ", ModelState.Values
                .SelectMany(v => v.Errors)
                .Select(e => e.ErrorMessage));
            TempData["Error"] = string.IsNullOrEmpty(errors) ?
                "Please fill in all required fields correctly." : errors;
            return RedirectToAction(nameof(Manage), new { caregiverId = availability.CaregiverId });
        }

        try
        {
            var caregiver = await _userDbContext.Users
                .FirstOrDefaultAsync(c => c.UserId == availability.CaregiverId && c.Role == UserRole.Caregiver);

            if (caregiver == null)
            {
                TempData["Error"] = "Caregiver not found";
                return RedirectToAction(nameof(Manage), new { caregiverId = availability.CaregiverId });
            }

            // Parse range and split into hourly slots on the hour
            if (!TimeSpan.TryParse(availability.StartTime, out var parsedStartTime) ||
                !TimeSpan.TryParse(availability.EndTime, out var parsedEndTime))
            {
                TempData["Error"] = "Invalid time format. Please use HH:mm format";
                return RedirectToAction(nameof(Manage), new { caregiverId = availability.CaregiverId });
            }
            if (parsedStartTime.Minutes != 0 || parsedEndTime.Minutes != 0)
            {
                TempData["Error"] = "Times must be on the hour (e.g., 09:00)";
                return RedirectToAction(nameof(Manage), new { caregiverId = availability.CaregiverId });
            }
            if (parsedEndTime <= parsedStartTime)
            {
                TempData["Error"] = "End time must be after start time";
                return RedirectToAction(nameof(Manage), new { caregiverId = availability.CaregiverId });
            }

            int created = 0;
            for (var t = parsedStartTime; t + TimeSpan.FromHours(1) <= parsedEndTime; t += TimeSpan.FromHours(1))
            {
                var slotStart = new TimeSpan(t.Hours, 0, 0).ToString(@"hh\:mm");
                var slotEnd = new TimeSpan((t + TimeSpan.FromHours(1)).Hours, 0, 0).ToString(@"hh\:mm");
                bool exists = await _userDbContext.Availabilities.AnyAsync(a => a.CaregiverId == availability.CaregiverId && a.Date.Date == availability.Date.Date && a.StartTime == slotStart && a.EndTime == slotEnd);
                if (exists) continue;
                var slot = new Availability
                {
                    CaregiverId = availability.CaregiverId,
                    Date = availability.Date.Date,
                    StartTime = slotStart,
                    EndTime = slotEnd,
                    Description = availability.Description
                };
                await _availabilityRepository.CreateAvailability(slot);
                created++;
            }
            if (created == 0)
            {
                TempData["Error"] = "No new slots created (may already exist).";
                return RedirectToAction(nameof(Manage), new { caregiverId = availability.CaregiverId });
            }

            TempData["Success"] = $"Created {created} slot(s).";
        }
        catch (DbUpdateException)
        {
            TempData["Error"] = "Unable to save availability. Please try again.";
        }

        // Return to Manage page
        return RedirectToAction(nameof(Manage), new { caregiverId = availability.CaregiverId });
    }

    // POST: Availability/Delete/{id}
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var availability = await _userDbContext.Availabilities.FindAsync(id);
        if (availability == null)
        {
            return NotFound();
        }

        try
        {
            // Restrict caregivers to deleting their own availability
            var roleInt = HttpContext.Session.GetInt32("CurrentUserRole");
            var userId = HttpContext.Session.GetInt32("CurrentUserId");
            if (roleInt.HasValue && userId.HasValue && (UserRole)roleInt.Value == UserRole.Caregiver && availability.CaregiverId != userId.Value)
            {
                return Forbid();
            }
            bool success = await _availabilityRepository.DeleteAvailability(id);
            if (success)
            {
                return RedirectToAction(nameof(Manage), new { caregiverId = availability.CaregiverId });
            }
            _logger.LogError("[AvailabilityController] Failed to delete availability {AvailabilityId:0000}", id);
            return BadRequest("Failed to delete availability");
        }
        catch (DbUpdateException)
        {
            _logger.LogError("[AvailabilityController] Failed to delete availability {AvailabilityId:0000}", id);
            return BadRequest("Failed to delete availability");
        }
    }
}
