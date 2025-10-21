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