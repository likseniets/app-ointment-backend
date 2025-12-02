using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using app_ointment_backend.Models;
using app_ointment_backend.Services;

namespace app_ointment_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AvailabilityController : Controller
{
    private readonly IAvailabilityService _availabilityService;
    private readonly ILogger<AvailabilityController> _logger;

    public AvailabilityController(
        IAvailabilityService availabilityService,
        ILogger<AvailabilityController> logger)
    {
        _availabilityService = availabilityService;
        _logger = logger;
    }

    // HttpGet for getting availabilities for the current caregiver,
    // uses claims from JWT to determine caregiver ID and role and return their availabilities
    [HttpGet]
    [ProducesResponseType(typeof(IEnumerable<AvailabilityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAvailabilities()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

        if (!string.IsNullOrEmpty(userIdClaim) && int.TryParse(userIdClaim, out int userId) &&
            Enum.TryParse<UserRole>(roleClaim, out var role) && role == UserRole.Caregiver)
        {
            var availabilities = await _availabilityService.GetAvailabilitiesByCaregiver(userId);
            if (availabilities == null)
            {
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

    // HttpGet for getting all availabilities, used by admins to fetch all availabilities
    [HttpGet("all")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<AvailabilityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAllAvailabilities()
    {
        var availabilities = await _availabilityService.GetAllAvailabilities();
        if (availabilities == null)
        {
            return NotFound("Availabilities not found");
        }
        var availabilityDtos = availabilities
            .OrderBy(a => a.Date)
            .ThenBy(a => a.StartTime)
            .Select(AvailabilityDto.FromAvailability);
        return Ok(availabilityDtos);
    }

    // HttpGet for getting availabilities by caregiver ID
    [HttpGet("{caregiverId}")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<AvailabilityDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAvailabilitiesByCaregiver(int caregiverId)
    {
        var availabilities = await _availabilityService.GetAvailabilitiesByCaregiver(caregiverId);
        if (availabilities == null)
        {
            return NotFound("Availabilities not found");
        }
        var availabilityDtos = availabilities
            .OrderBy(a => a.Date)
            .ThenBy(a => a.StartTime)
            .Select(AvailabilityDto.FromAvailability);
        return Ok(availabilityDtos);
    }

    // HttpPost for creating availability slots
    [HttpPost("create")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateAvailabilityDto availabilityDto)
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

        if (currentRole != UserRole.Admin && availabilityDto.CaregiverId != currentUserId)
        {
            _logger.LogWarning("[AvailabilityController] User {UserId} attempted to create availability for caregiver {CaregiverId}", currentUserId, availabilityDto.CaregiverId);
            return Forbid();
        }

        var (success, message, slotsCreated) = await _availabilityService.CreateAvailabilitySlots(availabilityDto);
        
        if (success)
        {
            var availabilities = await _availabilityService.GetAvailabilitiesByCaregiver(availabilityDto.CaregiverId);
            var availabilityDtos = availabilities?
                .OrderBy(a => a.Date)
                .ThenBy(a => a.StartTime)
                .Select(AvailabilityDto.FromAvailability) ?? Enumerable.Empty<AvailabilityDto>();
            return Ok(new { message, count = slotsCreated, availabilities = availabilityDtos });
        }

        return BadRequest(new { message });
    }

    // HttpPut for updating an availability
    [HttpPut("update/{id}")]
    [Authorize]
    [ProducesResponseType(typeof(AvailabilityDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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

        var availability = await _availabilityService.GetAvailabilityById(id);
        if (availability == null)
        {
            return NotFound("Availability not found");
        }

        if (currentRole != UserRole.Admin && availability.CaregiverId != currentUserId)
        {
            _logger.LogWarning("[AvailabilityController] User {UserId} attempted to update availability {AvailabilityId} owned by caregiver {CaregiverId}", currentUserId, id, availability.CaregiverId);
            return Forbid();
        }

        var updateAvailabilityDto = new UpdateAvailabilityDto
        {
            Date = updateDto.Date,
            StartTime = updateDto.StartTime,
            EndTime = updateDto.EndTime
        };

        var (success, message) = await _availabilityService.UpdateAvailability(id, updateAvailabilityDto);
        
        if (success)
        {
            var updatedAvailability = await _availabilityService.GetAvailabilityById(id);
            return Ok(updatedAvailability != null ? AvailabilityDto.FromAvailability(updatedAvailability) : null);
        }

        return BadRequest(new { message });
    }

    // HttpDelete for deleting an availability
    [HttpDelete("delete/{id}")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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

        var availability = await _availabilityService.GetAvailabilityById(id);
        if (availability == null)
        {
            return NotFound("Availability not found");
        }

        if (currentRole != UserRole.Admin && availability.CaregiverId != currentUserId)
        {
            _logger.LogWarning("[AvailabilityController] User {UserId} attempted to delete availability {AvailabilityId} owned by caregiver {CaregiverId}", currentUserId, id, availability.CaregiverId);
            return Forbid();
        }

        var (success, message) = await _availabilityService.DeleteAvailability(id);
        
        if (success)
        {
            var availabilities = await _availabilityService.GetAvailabilitiesByCaregiver(availability.CaregiverId);
            var availabilityDtos = availabilities?
                .OrderBy(a => a.Date)
                .ThenBy(a => a.StartTime)
                .Select(AvailabilityDto.FromAvailability) ?? Enumerable.Empty<AvailabilityDto>();
            return Ok(new { message, count = availabilityDtos.Count(), availabilities = availabilityDtos });
        }

        return BadRequest(new { message });
    }


}
