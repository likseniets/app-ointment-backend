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

[ApiController]
[Route("api/[controller]")]

public class AvailabilityController : Controller
{
    private readonly IAvailabilityRepository _availabilityRepository;
    private readonly IUserRepository _userRepository;
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly ILogger<AvailabilityController> _logger;

    public AvailabilityController(
        IAvailabilityRepository availabilityRepository,
        IUserRepository userRepository,
        IAppointmentRepository appointmentRepository,
        ILogger<AvailabilityController> logger)
    {
        _availabilityRepository = availabilityRepository;
        _userRepository = userRepository;
        _appointmentRepository = appointmentRepository;
        _logger = logger;
    }

    // GET: api/Availability
    // Returns all availabilities ordered by date and time
    [HttpGet]
    public async Task<IActionResult> GetAllAvailabilities()
    {
        var availabilities = await _availabilityRepository.GetAll();
        if (availabilities == null)
        {
            _logger.LogError("[AvailabilityController] Failed to get all availabilities");
            return NotFound("Availabilities not found");
        }
        var availabilityDtos = availabilities
            .OrderBy(a => a.Date)
            .ThenBy(a => a.StartTime)
            .Select(AvailabilityDto.FromAvailability);
        return Ok(availabilityDtos);
    }

    // GET: api/Availability/caregiver/{caregiverId}
    // Returns only the availabilities for the specified caregiver
    [HttpGet("caregiver/{caregiverId}")]
    public async Task<IActionResult> GetAvailabilitiesByCaregiver(int caregiverId)
    {
        var availabilities = await _availabilityRepository.GetAvailabilityByCaregiver(caregiverId);
        if (availabilities == null)
        {
            _logger.LogError("[AvailabilityController] Failed to get availabilities for CaregiverId {CaregiverId:0000}", caregiverId);
            return NotFound("Availabilities not found");
        }
        var availabilityDtos = availabilities
            .OrderBy(a => a.Date)
            .ThenBy(a => a.StartTime)
            .Select(AvailabilityDto.FromAvailability);
        return Ok(availabilityDtos);
    }

    // GET: Availability/Manage/{caregiverId}
    // If caregiverId is not provided or is 0, show the first caregiver found.
    [HttpGet("{caregiverId?}")]
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
            caregiver = await _userRepository.GetCaregiverWithAvailability(caregiverId.Value);
        }
        else
        {
            caregiver = await _userRepository.GetFirstCaregiver();
        }

        if (caregiver == null)
        {
            _logger.LogError("[AvailabilityController] Caregiver not found for Id {CaregiverId}", caregiverId ?? 0);
            // No caregiver available — redirect to the users table so the user can create one.
            return BadRequest("Caregiver not found");
        }

        // Also load this caregiver's booked appointments for display
        var appts = await _appointmentRepository.GetCaregiverAppointments(caregiver.UserId);
        ViewBag.CaregiverAppointments = appts ?? new List<Appointment>();

        return Ok(caregiver);
    }

    // POST: Availability/Create
    [HttpPost("create")]
    public async Task<IActionResult> Create([FromBody] CreateAvailabilityDto availabilityDto)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var caregiver = await _userRepository.GetUserById(availabilityDto.CaregiverId);

                if (caregiver == null || caregiver.Role != UserRole.Caregiver)
                {
                    return NotFound("Caregiver not found");
                }

                // Parse range and split into 1-hour slots on the hour
                if (!TimeSpan.TryParse(availabilityDto.StartTime, out var startTs) ||
                    !TimeSpan.TryParse(availabilityDto.EndTime, out var endTs))
                {
                    ModelState.AddModelError(string.Empty, "Invalid time format. Please use HH:mm");
                    return BadRequest(ModelState);
                }
                if (startTs >= endTs)
                {
                    ModelState.AddModelError(string.Empty, "End time must be after start time");
                    return BadRequest(ModelState);
                }
                if (startTs.Minutes != 0 || endTs.Minutes != 0)
                {
                    ModelState.AddModelError(string.Empty, "Times must be on the hour (e.g., 09:00)");
                    return BadRequest(ModelState);
                }

                var created = 0;
                for (var t = startTs; t + TimeSpan.FromHours(1) <= endTs; t += TimeSpan.FromHours(1))
                {
                    var slotStart = new TimeSpan(t.Hours, 0, 0).ToString(@"hh\:mm");
                    var slotEnd = new TimeSpan((t + TimeSpan.FromHours(1)).Hours, 0, 0).ToString(@"hh\:mm");
                    bool exists = await _availabilityRepository.AvailabilityExists(availabilityDto.CaregiverId, availabilityDto.Date.Date, slotStart, slotEnd);
                    if (exists) continue;
                    var slot = new Availability
                    {
                        CaregiverId = availabilityDto.CaregiverId,
                        Date = availabilityDto.Date.Date,
                        StartTime = slotStart,
                        EndTime = slotEnd
                    };
                    await _availabilityRepository.CreateAvailability(slot);
                    created++;
                }
                if (created > 0)
                {
                    // Get updated list of availabilities for this caregiver
                    var availabilities = await _availabilityRepository.GetAvailabilityByCaregiver(availabilityDto.CaregiverId);
                    var availabilityDtos = availabilities?
                        .OrderBy(a => a.Date)
                        .ThenBy(a => a.StartTime)
                        .Select(AvailabilityDto.FromAvailability) ?? Enumerable.Empty<AvailabilityDto>();
                    return Ok(new { message = $"Created {created} slot(s)", count = created, availabilities = availabilityDtos });
                }
                return BadRequest(new { message = "No new slots created (may already exist)" });
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError("[AvailabilityController] Failed to create availability: {Error}", ex.Message);
                return BadRequest(new { message = "Unable to save availability. Please try again." });
            }
        }

        return BadRequest(ModelState);
    }

    // POST: Availability/CreateInline
    [HttpPost("createinline")]
    public async Task<IActionResult> CreateInline([FromForm] int caregiverId, [FromForm] DateTime date, [FromForm] string startTime, [FromForm] string endTime)
    {
        // Debug logging to see what we're receiving
        _logger.LogInformation("[AvailabilityController] Received availability: Day={Day}, Start={Start}, End={End}, CaregiverId={Id}",
            date, startTime, endTime, caregiverId);

        var availability = new Availability
        {
            CaregiverId = caregiverId,
            Date = date,
            StartTime = startTime,
            EndTime = endTime
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
            var caregiver = await _userRepository.GetUserById(availability.CaregiverId);

            if (caregiver == null || caregiver.Role != UserRole.Caregiver)
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
                bool exists = await _availabilityRepository.AvailabilityExists(availability.CaregiverId, availability.Date.Date, slotStart, slotEnd);
                if (exists) continue;
                var slot = new Availability
                {
                    CaregiverId = availability.CaregiverId,
                    Date = availability.Date.Date,
                    StartTime = slotStart,
                    EndTime = slotEnd
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
    [HttpDelete("delete/{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        var availability = await _availabilityRepository.GetAvailabilityById(id);
        if (availability == null)
        {
            return NotFound();
        }

        try
        {
            bool success = await _availabilityRepository.DeleteAvailability(id);
            if (success)
            {
                var availabilities = await _availabilityRepository.GetAvailabilityByCaregiver(availability.CaregiverId);
                var availabilityDtos = availabilities?
                    .OrderBy(a => a.Date)
                    .ThenBy(a => a.StartTime)
                    .Select(AvailabilityDto.FromAvailability) ?? Enumerable.Empty<AvailabilityDto>();
                return Ok(new { message = $"Deleted slot(s)", count = availabilityDtos.Count(), availabilities = availabilityDtos });

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

    [HttpGet("update/{id}")]
    public async Task<IActionResult> Update(int id)
    {
        var availability = await _availabilityRepository.GetAvailabilityById(id);
        if (availability == null)
        {
            return NotFound();
        }

        var roleInt = HttpContext.Session.GetInt32("CurrentUserRole");
        var userId = HttpContext.Session.GetInt32("CurrentUserId");
        if (roleInt.HasValue && (UserRole)roleInt.Value == UserRole.Caregiver && userId.HasValue && availability.CaregiverId != userId.Value)
        {
            return Forbid();
        }

        return View(availability);
    }

    [HttpPost("update")]
    public async Task<IActionResult> Update([Bind("AvailabilityId,Date,StartTime,CaregiverId")] Availability model)
    {
        var availability = await _availabilityRepository.GetAvailabilityById(model.AvailabilityId);
        if (availability == null)
        {
            return NotFound();
        }

        var roleInt = HttpContext.Session.GetInt32("CurrentUserRole");
        var userId = HttpContext.Session.GetInt32("CurrentUserId");
        if (roleInt.HasValue && (UserRole)roleInt.Value == UserRole.Caregiver && userId.HasValue && availability.CaregiverId != userId.Value)
        {
            return Forbid();
        }

        if (!TimeSpan.TryParse(model.StartTime, out var startTs))
        {
            ModelState.AddModelError("StartTime", "Invalid time format. Use HH:mm");
        }
        if (startTs.Minutes != 0)
        {
            ModelState.AddModelError("StartTime", "Start must be on the hour");
        }
        var endTs = startTs.Add(TimeSpan.FromHours(1));

        bool conflict = await _availabilityRepository.AvailabilityConflictExists(
            availability.AvailabilityId,
            availability.CaregiverId,
            model.Date.Date,
            new TimeSpan(startTs.Hours, 0, 0).ToString(@"hh\:mm"),
            new TimeSpan(endTs.Hours, 0, 0).ToString(@"hh\:mm"));
        if (conflict)
        {
            ModelState.AddModelError(string.Empty, "A slot at that time already exists");
        }

        if (!ModelState.IsValid)
        {
            return View(model);
        }

        availability.Date = model.Date.Date;
        availability.StartTime = new TimeSpan(startTs.Hours, 0, 0).ToString(@"hh\:mm");
        availability.EndTime = new TimeSpan(endTs.Hours, 0, 0).ToString(@"hh\:mm");

        var ok = await _availabilityRepository.UpdateAvailability(availability);
        if (!ok)
        {
            ModelState.AddModelError(string.Empty, "Failed to update availability");
            return View(model);
        }

        return RedirectToAction(nameof(Manage), new { caregiverId = availability.CaregiverId });
    }
}
