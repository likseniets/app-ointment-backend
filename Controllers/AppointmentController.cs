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
public class AppointmentController : Controller
{
    private readonly IAppointmentService _appointmentService;
    private readonly ILogger<AppointmentController> _logger;

    public AppointmentController(IAppointmentService appointmentService, ILogger<AppointmentController> logger)
    {
        _appointmentService = appointmentService;
        _logger = logger;
    }

    // HttpGet for getting appointments based on user role, 
    // uses claims from JWT to determine role and fetches user specific appointments
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<AppointmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IEnumerable<Appointment>>> GetSpecified()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            _logger.LogError("[AppointmentController] Unable to get user ID from JWT claims");
            return Unauthorized("Invalid token");
        }

        if (!Enum.TryParse<UserRole>(roleClaim, out var role))
        {
            _logger.LogError("[AppointmentController] Unable to parse user role from JWT claims");
            return Unauthorized("Invalid token");
        }

        IEnumerable<Appointment>? appointments;

        if (role == UserRole.Client)
        {
            appointments = await _appointmentService.GetClientAppointments(userId);
        }
        else if (role == UserRole.Caregiver)
        {
            appointments = await _appointmentService.GetCaregiverAppointments(userId);
        }
        else // Admin gets all appointments
        {
            appointments = await _appointmentService.GetAllAppointments();
        }

        if (appointments == null)
        {
            return NotFound("Appointment list not found");
        }

        var appointmentDtos = appointments.Select(AppointmentDto.FromAppointment);
        return Ok(appointmentDtos);
    }

    // HttpGet for getting all appointments, used by admins to fetch all appointments
    [HttpGet("all")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<AppointmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetAll()
    {
        var appointments = await _appointmentService.GetAllAppointments();
        if (appointments == null)
        {
            return NotFound("Appointment list not found");
        }

        var appointmentDtos = appointments.Select(AppointmentDto.FromAppointment);
        return Ok(appointmentDtos);
    }

    // HttpGet for getting appointments by client ID
    [HttpGet("byclient/{clientId:int}")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<AppointmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(int clientId)
    {
        var appointments = await _appointmentService.GetClientAppointments(clientId);
        if (appointments == null)
        {
            return NotFound("Appointment list for user not found");
        }

        var appointmentDtos = appointments.Select(AppointmentDto.FromAppointment);
        return Ok(appointmentDtos);
    }

    // HttpGet for getting appointments by caregiver ID
    [HttpGet("bycaregiver/{caregiverId:int}")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<AppointmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetByCaregiver(int caregiverId)
    {
        var appointments = await _appointmentService.GetCaregiverAppointments(caregiverId);
        if (appointments == null)
        {
            return NotFound("Appointment list for caregiver not found");
        }

        var appointmentDtos = appointments.Select(AppointmentDto.FromAppointment);
        return Ok(appointmentDtos);
    }

    // HttpPost for creating a new appointment
    [HttpPost("create")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<AppointmentDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> Create([FromBody] CreateAppointmentDto appointmentDto)
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

        // Only the client themselves or an admin can create an appointment for that client
        if (currentRole != UserRole.Admin && appointmentDto.ClientId != currentUserId)
        {
            _logger.LogWarning("[AppointmentController] User {UserId} attempted to create appointment for client {ClientId}", currentUserId, appointmentDto.ClientId);
            return Forbid();
        }

        var result = await _appointmentService.CreateAppointment(appointmentDto);
        
        if (!result.Success)
        {
            return BadRequest(new { message = result.Message });
        }

        // Return the created appointment
        var appointments = await _appointmentService.GetClientAppointments(appointmentDto.ClientId);
        if (appointments != null)
        {
            var appointmentDtos = appointments
                .OrderBy(a => a.Date)
                .Select(AppointmentDto.FromAppointment);
            return Ok(appointmentDtos);
        }

        return Ok(new { message = result.Message });
    }

    // HttpPut for updating an appointment
    [HttpPut("update/{id}")]
    [Authorize]
    [ProducesResponseType(typeof(AppointmentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateAppointmentDto updateDto)
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

        var appointment = await _appointmentService.GetAppointmentById(id);
        if (appointment == null)
        {
            return NotFound("Appointment not found");
        }

        // Only the correct client or caregiver can update, or an admin can update the appointment
        if (currentRole != UserRole.Admin &&
            appointment.ClientId != currentUserId &&
            appointment.CaregiverId != currentUserId)
        {
            _logger.LogWarning("[AppointmentController] User {UserId} attempted to update appointment {AppointmentId}", currentUserId, id);
            return Forbid();
        }

        var result = await _appointmentService.UpdateAppointment(id, updateDto);
        
        if (!result.Success)
        {
            return BadRequest(result.Message);
        }

        var updatedAppointment = await _appointmentService.GetAppointmentById(id);
        return Ok(AppointmentDto.FromAppointment(updatedAppointment!));
    }

    // HttpDelete for deleting an appointment
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

        var appointment = await _appointmentService.GetAppointmentById(id);
        if (appointment == null)
        {
            return NotFound("Appointment not found");
        }

        // Only the correct client or caregiver can delete the appointment,
        // or an admin can delete the appointment
        if (currentRole != UserRole.Admin &&
            appointment.ClientId != currentUserId &&
            appointment.CaregiverId != currentUserId)
        {
            _logger.LogWarning("[AppointmentController] User {UserId} attempted to delete appointment {AppointmentId}", currentUserId, id);
            return Forbid();
        }

        var result = await _appointmentService.DeleteAppointment(id);
        
        if (!result.Success)
        {
            return BadRequest(result.Message);
        }

        // Return updated appointments list based on user role
        IEnumerable<Appointment>? appointments = null;
        if (currentRole == UserRole.Client || appointment.ClientId == currentUserId)
        {
            appointments = await _appointmentService.GetClientAppointments(currentUserId);
        }
        else if (currentRole == UserRole.Caregiver || appointment.CaregiverId == currentUserId)
        {
            appointments = await _appointmentService.GetCaregiverAppointments(currentUserId);
        }

        if (appointments != null)
        {
            var appointmentDtos = appointments
                .OrderBy(a => a.Date)
                .Select(AppointmentDto.FromAppointment);
            return Ok(new { message = result.Message, appointments = appointmentDtos });
        }

        return Ok(new { message = result.Message });
    }
}
