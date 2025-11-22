using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
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
    // Returns availabilities for the logged-in caregiver (or all if not authenticated)
    [HttpGet]
    public async Task<IActionResult> GetAvailabilities()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

        // If authenticated and is a caregiver, return only their availabilities
        if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId) &&
            Enum.TryParse<UserRole>(roleClaim, out var role) && role == UserRole.Caregiver)
        {
            var availabilities = await _availabilityRepository.GetAvailabilityByCaregiver(userId);
            if (availabilities == null)
            {
                _logger.LogError("[AvailabilityController] Failed to get availabilities for caregiver {CaregiverId}", userId);
                return NotFound("Availabilities not found");
            }
            var availabilityDtos = availabilities
                .OrderBy(a => a.Date)
                .ThenBy(a => a.StartTime)
                .Select(AvailabilityDto.FromAvailability);
            return Ok(availabilityDtos);
        }
        return BadRequest("Not a caregiver");
    }

    // GET: api/Availability/caregiver/{caregiverId}
    // Returns only the availabilities for the specified caregiver
    [HttpGet("caregiver/{caregiverId}")]
    [Authorize]
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
    [Authorize]
    public async Task<IActionResult> Create([FromBody] CreateAvailabilityDto availabilityDto)
    {
        if (ModelState.IsValid)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int currentUserId))
            {
                return Unauthorized("Invalid token");
            }

            if (!Enum.TryParse<UserRole>(roleClaim, out var currentRole))
            {
                return Unauthorized("Invalid role");
            }

            // Only the caregiver themselves or an admin can create availability
            if (currentRole != UserRole.Admin && availabilityDto.CaregiverId != currentUserId)
            {
                _logger.LogWarning("[AvailabilityController] User {UserId} attempted to create availability for caregiver {CaregiverId}", currentUserId, availabilityDto.CaregiverId);
                return Forbid();
            }

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

    // PUT: Availability/Update/{id}
    [HttpPut("update/{id}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, [FromBody] CreateAvailabilityDto updateDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int currentUserId))
        {
            return Unauthorized("Invalid token");
        }

        if (!Enum.TryParse<UserRole>(roleClaim, out var currentRole))
        {
            return Unauthorized("Invalid role");
        }

        var availability = await _availabilityRepository.GetAvailabilityById(id);
        if (availability == null)
        {
            return NotFound("Availability not found");
        }

        // Only the caregiver who owns the availability or an admin can update it
        if (currentRole != UserRole.Admin && availability.CaregiverId != currentUserId)
        {
            _logger.LogWarning("[AvailabilityController] User {UserId} attempted to update availability {AvailabilityId} owned by caregiver {CaregiverId}", currentUserId, id, availability.CaregiverId);
            return Forbid();
        }

        // Parse and validate times
        if (!TimeSpan.TryParse(updateDto.StartTime, out var startTs) ||
            !TimeSpan.TryParse(updateDto.EndTime, out var endTs))
        {
            return BadRequest("Invalid time format. Please use HH:mm");
        }

        if (startTs >= endTs)
        {
            return BadRequest("End time must be after start time");
        }

        if (startTs.Minutes != 0 || endTs.Minutes != 0)
        {
            return BadRequest("Times must be on the hour (e.g., 09:00)");
        }

        var slotStart = new TimeSpan(startTs.Hours, 0, 0).ToString(@"hh\:mm");
        var slotEnd = new TimeSpan(endTs.Hours, 0, 0).ToString(@"hh\:mm");

        // Check if the new time slot conflicts with other availabilities (excluding current one)
        bool conflicts = await _availabilityRepository.AvailabilityConflictExists(id, availability.CaregiverId, updateDto.Date.Date, slotStart, slotEnd);
        if (conflicts)
        {
            return BadRequest("This time slot conflicts with an existing availability");
        }

        // Update the availability
        availability.Date = updateDto.Date.Date;
        availability.StartTime = slotStart;
        availability.EndTime = slotEnd;

        try
        {
            bool success = await _availabilityRepository.UpdateAvailability(availability);
            if (success)
            {
                _logger.LogInformation("[AvailabilityController] Availability {AvailabilityId} updated successfully", id);
                return Ok(AvailabilityDto.FromAvailability(availability));
            }
            return BadRequest("Failed to update availability");
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError("[AvailabilityController] Failed to update availability {AvailabilityId}: {Error}", id, ex.Message);
            return BadRequest("Unable to update availability. Please try again.");
        }
    }

    // DELETE: Availability/Delete/{id}
    [HttpDelete("delete/{id}")]
    [Authorize]
    public async Task<IActionResult> Delete(int id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int currentUserId))
        {
            return Unauthorized("Invalid token");
        }

        if (!Enum.TryParse<UserRole>(roleClaim, out var currentRole))
        {
            return Unauthorized("Invalid role");
        }

        var availability = await _availabilityRepository.GetAvailabilityById(id);
        if (availability == null)
        {
            return NotFound("Availability not found");
        }

        // Only the caregiver who owns the availability or an admin can delete it
        if (currentRole != UserRole.Admin && availability.CaregiverId != currentUserId)
        {
            _logger.LogWarning("[AvailabilityController] User {UserId} attempted to delete availability {AvailabilityId} owned by caregiver {CaregiverId}", currentUserId, id, availability.CaregiverId);
            return Forbid();
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


}
