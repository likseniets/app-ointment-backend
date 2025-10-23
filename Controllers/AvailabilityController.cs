using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

        return View(caregiver);
    }

    // GET: Availability/Days?caregiverId=1&from=2025-01-01&to=2025-01-31
    // Returns a JSON array of yyyy-MM-dd strings for dates that have availability for the caregiver
    [HttpGet]
    public async Task<IActionResult> Days([FromQuery] int caregiverId, [FromQuery] DateTime? from, [FromQuery] DateTime? to)
    {
        if (caregiverId <= 0)
        {
            _logger.LogWarning("[AvailabilityController] Missing or invalid caregiverId on Days endpoint");
            return BadRequest("Missing caregiverId");
        }

        var caregiverExists = await _userDbContext.Users.AnyAsync(u => u.UserId == caregiverId && u.Role == UserRole.Caregiver);
        if (!caregiverExists)
        {
            _logger.LogError("[AvailabilityController] Caregiver not found for Id {CaregiverId:0000}", caregiverId);
            return NotFound("Caregiver not found");
        }

        DateTime fromDate = (from?.Date) ?? DateTime.Today;
        DateTime toDate = (to?.Date) ?? fromDate.AddDays(60);

        try
        {
            var dates = await _userDbContext.Availabilities
                .Where(a => a.CaregiverId == caregiverId && a.Date.Date >= fromDate && a.Date.Date <= toDate)
                .Select(a => a.Date.Date)
                .Distinct()
                .OrderBy(d => d)
                .ToListAsync();

            var result = dates.Select(d => d.ToString("yyyy-MM-dd")).ToList();
            return Ok(result);
        }
        catch (Exception e)
        {
            _logger.LogError("[AvailabilityController] Failed to query available days for caregiver {CaregiverId:0000}. Error: {Error}", caregiverId, e.Message);
            return BadRequest("Failed to query available days");
        }
    }

    // GET: Availability/Slots?caregiverId=1&date=2025-10-23&clientId=2
    // Returns JSON array of { time: "HH:mm", booked: bool, bookedBySelf: bool }
    [HttpGet]
    public async Task<IActionResult> Slots([FromQuery] int caregiverId, [FromQuery] DateTime date, [FromQuery] int? clientId)
    {
        if (caregiverId <= 0)
        {
            _logger.LogWarning("[AvailabilityController] Missing or invalid caregiverId on Slots endpoint");
            return BadRequest("Missing caregiverId");
        }

        var day = date.Date;

        var caregiverExists = await _userDbContext.Users.AnyAsync(u => u.UserId == caregiverId && u.Role == UserRole.Caregiver);
        if (!caregiverExists)
        {
            _logger.LogError("[AvailabilityController] Caregiver not found for Id {CaregiverId:0000}", caregiverId);
            return NotFound("Caregiver not found");
        }

        try
        {
            var windows = await _userDbContext.Availabilities
                .Where(a => a.CaregiverId == caregiverId && a.Date.Date == day)
                .AsNoTracking()
                .ToListAsync();

            var slotStarts = new HashSet<int>();
            foreach (var w in windows)
            {
                if (!TimeSpan.TryParse(w.StartTime, out var start)) continue; // Validation occurs on create; guard anyway
                if (!TimeSpan.TryParse(w.EndTime, out var end)) continue;
                // Use whole-hour slots; start rounds up, end is exclusive
                var firstHour = start.Minutes > 0 ? start.Hours + 1 : start.Hours;
                var lastExclusive = end.Hours; // end is exclusive; e.g., 09-17 => 09..16
                for (int h = firstHour; h < lastExclusive; h++)
                {
                    if (h >= 0 && h <= 23) slotStarts.Add(h);
                }
            }

            var existing = await _userDbContext.Appointments
                .Where(a => a.CaregiverId == caregiverId && a.Date.Date == day)
                .Select(a => new { a.Date, a.ClientId })
                .ToListAsync();

            var result = slotStarts
                .OrderBy(h => h)
                .Select(h =>
                {
                    var bookedBy = existing.FirstOrDefault(x => x.Date.Hour == h)?.ClientId;
                    bool booked = bookedBy != null;
                    bool bySelf = clientId.HasValue && bookedBy == clientId.Value;
                    return new { time = new TimeSpan(h, 0, 0).ToString(@"hh\:mm"), booked, bookedBySelf = bySelf };
                })
                .ToList();

            return Ok(result);
        }
        catch (Exception e)
        {
            _logger.LogError("[AvailabilityController] Failed to query slots for caregiver {CaregiverId:0000} on {Date}. Error: {Error}", caregiverId, day.ToString("yyyy-MM-dd"), e.Message);
            return BadRequest("Failed to query slots");
        }
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
                var caregiver = await _userDbContext.Users
                    .FirstOrDefaultAsync(c => c.UserId == availability.CaregiverId && c.Role == UserRole.Caregiver);

                if (caregiver == null)
                {
                    return NotFound("Caregiver not found");
                }

                bool success = await _availabilityRepository.CreateAvailability(availability);
                if (success)
                {
                    return RedirectToAction(nameof(Manage), new { caregiverId = availability.CaregiverId });
                }
                // fall through to redirect back to Manage so the user stays in context
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

            // Parse and validate time range
            TimeSpan parsedStartTime, parsedEndTime;
            if (!TimeSpan.TryParse(availability.StartTime, out parsedStartTime) ||
                !TimeSpan.TryParse(availability.EndTime, out parsedEndTime))
            {
                TempData["Error"] = "Invalid time format. Please use HH:mm format";
                return RedirectToAction(nameof(Manage), new { caregiverId = availability.CaregiverId });
            }

            if (parsedEndTime <= parsedStartTime)
            {
                TempData["Error"] = "End time must be after start time";
                return RedirectToAction(nameof(Manage), new { caregiverId = availability.CaregiverId });
            }

            bool success = await _availabilityRepository.CreateAvailability(availability);
            if (!success)
            {
                TempData["Error"] = "Failed to create availability";
                return RedirectToAction(nameof(Manage), new { caregiverId = availability.CaregiverId });
            }

            TempData["Success"] = "Availability slot added successfully";
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
