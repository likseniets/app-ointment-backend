using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using app_ointment_backend.Models;
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
    [Authorize]
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
            appointments = await _appointmentRepository.GetClientAppointment(userId);
            if (appointments == null)
            {
                _logger.LogError("[AppointmentController] Appointment list not found for client {ClientId}", userId);
                return NotFound("Appointment list not found");
            }
            var appointmentDtos = appointments.Select(AppointmentDto.FromAppointment);
            return Ok(appointmentDtos);
        }
        else if (role == UserRole.Caregiver)
        {
            appointments = await _appointmentRepository.GetCaregiverAppointments(userId);
            if (appointments == null)
            {
                _logger.LogError("[AppointmentController] Appointment list not found for caregiver {CaregiverId}", userId);
                return NotFound("Appointment list not found");
            }
            var appointmentDtos = appointments.Select(AppointmentDto.FromAppointment);
            return Ok(appointmentDtos);
        }

        // For admin or other roles, return all appointments
        appointments = await _appointmentRepository.GetAll();
        if (appointments == null)
        {
            _logger.LogError("[AppointmentController] Appointment list not found while executing _appointmentRepository.GetAll()");
            return NotFound("Appointment list not found");
        }

        var allAppointmentDtos = appointments.Select(AppointmentDto.FromAppointment);
        return Ok(allAppointmentDtos);
    }

    [HttpGet("all")]
    [Authorize]
    public async Task<IActionResult> GetAll()
    {
        var appointments = await _appointmentRepository.GetAll();
        if (appointments == null)
        {
            _logger.LogError("[AppointmentController] Appointment list not found while executing _appointmentRepository.GetAll()");
            return NotFound("Appointment list not found");
        }

        var appointmentDtos = appointments.Select(AppointmentDto.FromAppointment);
        return Ok(appointmentDtos);
    }

    [HttpGet("byclient/{clientId:int}")]
    [Authorize]
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
    [Authorize]
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
    [Authorize]
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
                Task = appointmentDto.Task
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

    [HttpPut("update/{id}")]
    [Authorize]
    public async Task<IActionResult> Update(int id, [FromBody] CreateAppointmentDto updateDto)
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

        var appointment = await _appointmentRepository.GetAppointmentById(id);
        if (appointment == null)
        {
            return NotFound("Appointment not found");
        }

        // Only the client, caregiver, or admin can update the appointment
        if (currentRole != UserRole.Admin &&
            appointment.ClientId != currentUserId &&
            appointment.CaregiverId != currentUserId)
        {
            _logger.LogWarning("[AppointmentController] User {UserId} attempted to update appointment {AppointmentId}", currentUserId, id);
            return Forbid();
        }

        // Update appointment task
        appointment.Task = updateDto.Task;

        try
        {
            bool success = await _appointmentRepository.UpdateAppointment(appointment);
            if (success)
            {
                _logger.LogInformation("[AppointmentController] Appointment {AppointmentId} updated successfully", id);
                return Ok(AppointmentDto.FromAppointment(appointment));
            }
            return BadRequest("Failed to update appointment");
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError("[AppointmentController] Failed to update appointment {AppointmentId}: {Error}", id, ex.Message);
            return BadRequest("Unable to update appointment. Please try again.");
        }
    }

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

        var appointment = await _appointmentRepository.GetAppointmentById(id);
        if (appointment == null)
        {
            return NotFound("Appointment not found");
        }

        // Only the client, caregiver, or admin can delete the appointment
        if (currentRole != UserRole.Admin &&
            appointment.ClientId != currentUserId &&
            appointment.CaregiverId != currentUserId)
        {
            _logger.LogWarning("[AppointmentController] User {UserId} attempted to delete appointment {AppointmentId}", currentUserId, id);
            return Forbid();
        }

        try
        {
            bool success = await _appointmentRepository.DeleteAppointment(id);
            if (success)
            {
                _logger.LogInformation("[AppointmentController] Appointment {AppointmentId} deleted successfully", id);

                // Return updated appointments list based on user role
                IEnumerable<Appointment>? appointments = null;
                if (currentRole == UserRole.Client || appointment.ClientId == currentUserId)
                {
                    appointments = await _appointmentRepository.GetClientAppointment(currentUserId);
                }
                else if (currentRole == UserRole.Caregiver || appointment.CaregiverId == currentUserId)
                {
                    appointments = await _appointmentRepository.GetCaregiverAppointments(currentUserId);
                }

                if (appointments != null)
                {
                    var appointmentDtos = appointments
                        .OrderBy(a => a.Date)
                        .Select(AppointmentDto.FromAppointment);
                    return Ok(new { message = "Appointment deleted successfully", appointments = appointmentDtos });
                }

                return Ok(new { message = "Appointment deleted successfully" });
            }

            return BadRequest("Failed to delete appointment");
        }
        catch (DbUpdateException ex)
        {
            _logger.LogError("[AppointmentController] Failed to delete appointment {AppointmentId}: {Error}", id, ex.Message);
            return BadRequest("Unable to delete appointment. Please try again.");
        }
    }
}
