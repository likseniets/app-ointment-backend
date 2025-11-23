using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using app_ointment_backend.Models;
using app_ointment_backend.DAL;

namespace app_ointment_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChangeRequestController : ControllerBase
{
    private readonly IChangeRequestRepository _changeRequestRepository;
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IAvailabilityRepository _availabilityRepository;
    private readonly ILogger<ChangeRequestController> _logger;

    public ChangeRequestController(
        IChangeRequestRepository changeRequestRepository,
        IAppointmentRepository appointmentRepository,
        IAvailabilityRepository availabilityRepository,
        ILogger<ChangeRequestController> logger)
    {
        _changeRequestRepository = changeRequestRepository;
        _appointmentRepository = appointmentRepository;
        _availabilityRepository = availabilityRepository;
        _logger = logger;
    }

    // GET: api/ChangeRequest/pending
    // Get all pending change requests for the current user (requests they need to approve)
    [HttpGet("pending")]
    [Authorize]
    public async Task<IActionResult> GetPendingRequests()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized("Invalid token");
        }

        var requests = await _changeRequestRepository.GetPendingChangeRequestsForUser(userId);
        if (requests == null)
        {
            _logger.LogError("[ChangeRequestController] Failed to get pending change requests for user {UserId}", userId);
            return NotFound("No pending change requests found");
        }

        var requestDtos = requests.Select(AppointmentChangeRequestDto.FromChangeRequest);
        return Ok(requestDtos);
    }

    [HttpGet("requested")]
    [Authorize]
    public async Task<IActionResult> GetRequested()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized("Invalid token");
        }

        var requests = await _changeRequestRepository.GetChangeRequestsByUser(userId);
        if (requests == null)
        {
            _logger.LogError("[ChangeRequestController] Failed to get pending change requests for user {UserId}", userId);
            return NotFound("No pending change requests found");
        }

        var requestDtos = requests.Select(AppointmentChangeRequestDto.FromChangeRequest);
        return Ok(requestDtos);
    }

    // GET: api/ChangeRequest/appointment/{appointmentId}
    // Get all change requests for a specific appointment
    [HttpGet("appointment/{appointmentId}")]
    public async Task<IActionResult> GetByAppointment(int appointmentId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized("Invalid token");
        }

        if (!Enum.TryParse<UserRole>(roleClaim, out var role))
        {
            return Unauthorized("Invalid role");
        }

        var appointment = await _appointmentRepository.GetAppointmentById(appointmentId);
        if (appointment == null)
        {
            return NotFound("Appointment not found");
        }

        // Only allow the client, caregiver, or admin to view change requests
        if (role != UserRole.Admin &&
            appointment.ClientId != userId &&
            appointment.CaregiverId != userId)
        {
            return Forbid();
        }

        var requests = await _changeRequestRepository.GetChangeRequestsByAppointment(appointmentId);
        if (requests == null)
        {
            return Ok(new List<AppointmentChangeRequestDto>());
        }

        var requestDtos = requests.Select(AppointmentChangeRequestDto.FromChangeRequest);
        return Ok(requestDtos);
    }

    // POST: api/ChangeRequest/create
    // Request a change to an appointment
    [HttpPost("create")]
    [Authorize]
    public async Task<IActionResult> CreateChangeRequest([FromBody] CreateChangeRequestDto requestDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized("Invalid token");
        }

        if (!Enum.TryParse<UserRole>(roleClaim, out var role))
        {
            return Unauthorized("Invalid role");
        }

        var appointment = await _appointmentRepository.GetAppointmentById(requestDto.AppointmentId);
        if (appointment == null)
        {
            return NotFound("Appointment not found");
        }

        // Only the client or caregiver involved can request changes
        if (appointment.ClientId != userId && appointment.CaregiverId != userId)
        {
            _logger.LogWarning("[ChangeRequestController] User {UserId} attempted to request change for appointment {AppointmentId}", userId, requestDto.AppointmentId);
            return Forbid();
        }

        // Check if there's already a pending change request for this appointment
        var existingRequests = await _changeRequestRepository.GetChangeRequestsByAppointment(requestDto.AppointmentId);
        if (existingRequests != null && existingRequests.Any(r => r.Status == ChangeRequestStatus.Pending))
        {
            return BadRequest("There is already a pending change request for this appointment");
        }

        DateTime? newDateTime = null;
        // If requesting a datetime change, get the datetime from the availability slot
        _logger.LogInformation("[ChangeRequestController] User {UserId} is requesting a datetime change using AvailabilityId {AvailabilityId}", userId, requestDto.NewAvailabilityId);
        if (requestDto.NewAvailabilityId.HasValue)
        {
            var availableSlots = await _availabilityRepository.GetAvailabilityByCaregiver(appointment.CaregiverId);
            var matchingSlot = availableSlots?.FirstOrDefault(a => a.AvailabilityId == requestDto.NewAvailabilityId.Value);

            if (matchingSlot == null)
            {
                return BadRequest("The requested availability slot does not exist or does not belong to this caregiver");
            }

            // Parse the time from the availability slot
            var timeParts = matchingSlot.StartTime.Split(':');
            if (timeParts.Length >= 2 && int.TryParse(timeParts[0], out int hour))
            {
                newDateTime = matchingSlot.Date.Date.AddHours(hour);
            }
            else
            {
                return BadRequest("Invalid time format in availability slot");
            }
        }

        // Create the change request
        var changeRequest = new AppointmentChangeRequest
        {
            AppointmentId = requestDto.AppointmentId,
            RequestedByUserId = userId,
            OldTask = appointment.Task,
            OldDateTime = appointment.Date,
            NewTask = requestDto.NewTask,
            NewDateTime = newDateTime,
            Status = ChangeRequestStatus.Pending,
            RequestedAt = DateTime.UtcNow
        };

        bool success = await _changeRequestRepository.CreateChangeRequest(changeRequest);
        if (!success)
        {
            return BadRequest("Failed to create change request");
        }

        _logger.LogInformation("[ChangeRequestController] User {UserId} created change request for appointment {AppointmentId}", userId, requestDto.AppointmentId);

        // Reload to get navigation properties
        var createdRequest = await _changeRequestRepository.GetChangeRequestById(changeRequest.ChangeRequestId);
        if (createdRequest == null)
        {
            return Ok(new { message = "Change request created successfully" });
        }

        return Ok(AppointmentChangeRequestDto.FromChangeRequest(createdRequest));
    }

    // PUT: api/ChangeRequest/approve/{changeRequestId}
    // Approve a change request and apply the changes to the appointment
    [HttpPut("approve/{changeRequestId}")]
    public async Task<IActionResult> ApproveChangeRequest(int changeRequestId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized("Invalid token");
        }

        if (!Enum.TryParse<UserRole>(roleClaim, out var role))
        {
            return Unauthorized("Invalid role");
        }

        var changeRequest = await _changeRequestRepository.GetChangeRequestById(changeRequestId);
        if (changeRequest == null)
        {
            return NotFound("Change request not found");
        }

        if (changeRequest.Status != ChangeRequestStatus.Pending)
        {
            return BadRequest("This change request has already been processed");
        }

        // Use the appointment from the change request (already loaded via Include)
        var appointment = changeRequest.Appointment;
        if (appointment == null)
        {
            return NotFound("Appointment not found");
        }

        // Only the other party (not the requester) can approve
        if (changeRequest.RequestedByUserId == userId)
        {
            return BadRequest("You cannot approve your own change request");
        }

        // Verify the user is the client or caregiver involved
        if (role != UserRole.Admin &&
            appointment.ClientId != userId &&
            appointment.CaregiverId != userId)
        {
            _logger.LogWarning("[ChangeRequestController] User {UserId} attempted to approve change request {ChangeRequestId}", userId, changeRequestId);
            return Forbid();
        }

        // Store the old date/time from the change request
        var oldDateTime = changeRequest.OldDateTime ?? appointment.Date;
        var hasDateTimeChange = changeRequest.NewDateTime.HasValue && changeRequest.NewDateTime.Value != oldDateTime;

        // If datetime is changing, we need to handle availability slots
        if (hasDateTimeChange)
        {
            // Check if the new timeslot is available
            var newDate = changeRequest.NewDateTime!.Value;
            var newStartTime = new TimeSpan(newDate.Hour, 0, 0).ToString(@"hh\:mm");
            var newEndTime = new TimeSpan(newDate.Hour + 1, 0, 0).ToString(@"hh\:mm");

            // Check if there's an available slot for the new time
            var availableSlot = await _availabilityRepository.GetAvailabilityByCaregiver(appointment.CaregiverId);
            var matchingSlot = availableSlot?.FirstOrDefault(a =>
                a.Date.Date == newDate.Date &&
                a.StartTime == newStartTime &&
                a.EndTime == newEndTime);

            if (matchingSlot == null)
            {
                return BadRequest("The requested time slot is not available");
            }

            // Apply the changes to the appointment
            if (changeRequest.NewTask.HasValue)
            {
                appointment.Task = changeRequest.NewTask.Value;
            }
            appointment.Date = changeRequest.NewDateTime.Value;

            bool appointmentUpdated = await _appointmentRepository.UpdateAppointment(appointment);
            if (!appointmentUpdated)
            {
                return BadRequest("Failed to update appointment");
            }

            // Delete the new availability slot (it's now booked)
            await _availabilityRepository.DeleteAvailability(matchingSlot.AvailabilityId);

            // Restore the old availability slot
            var restoredSlot = new Availability
            {
                CaregiverId = appointment.CaregiverId,
                Date = oldDateTime.Date,
                StartTime = new TimeSpan(oldDateTime.Hour, 0, 0).ToString(@"hh\:mm"),
                EndTime = new TimeSpan(oldDateTime.Hour + 1, 0, 0).ToString(@"hh\:mm")
            };
            await _availabilityRepository.CreateAvailability(restoredSlot);
        }
        else
        {
            // Only task is changing, no availability slot changes needed
            if (changeRequest.NewTask.HasValue)
            {
                appointment.Task = changeRequest.NewTask.Value;
            }

            bool appointmentUpdated = await _appointmentRepository.UpdateAppointment(appointment);
            if (!appointmentUpdated)
            {
                return BadRequest("Failed to update appointment");
            }
        }

        // Update the change request status
        changeRequest.Status = ChangeRequestStatus.Approved;
        changeRequest.RespondedAt = DateTime.UtcNow;
        changeRequest.RespondedByUserId = userId;

        bool requestUpdated = await _changeRequestRepository.UpdateChangeRequest(changeRequest);
        if (!requestUpdated)
        {
            _logger.LogWarning("[ChangeRequestController] Failed to update change request {ChangeRequestId} status", changeRequestId);
        }

        _logger.LogInformation("[ChangeRequestController] User {UserId} approved change request {ChangeRequestId}", userId, changeRequestId);

        return Ok(new
        {
            message = "Change request approved and appointment updated",
            appointment = AppointmentDto.FromAppointment(appointment)
        });
    }

    // PUT: api/ChangeRequest/reject/{changeRequestId}
    // Reject a change request
    [HttpPut("reject/{changeRequestId}")]
    public async Task<IActionResult> RejectChangeRequest(int changeRequestId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized("Invalid token");
        }

        if (!Enum.TryParse<UserRole>(roleClaim, out var role))
        {
            return Unauthorized("Invalid role");
        }

        var changeRequest = await _changeRequestRepository.GetChangeRequestById(changeRequestId);
        if (changeRequest == null)
        {
            return NotFound("Change request not found");
        }

        if (changeRequest.Status != ChangeRequestStatus.Pending)
        {
            return BadRequest("This change request has already been processed");
        }

        var appointment = await _appointmentRepository.GetAppointmentById(changeRequest.AppointmentId);
        if (appointment == null)
        {
            return NotFound("Appointment not found");
        }

        // Only the other party (not the requester) can reject
        if (changeRequest.RequestedByUserId == userId)
        {
            return BadRequest("You cannot reject your own change request. Use the cancel endpoint instead.");
        }

        // Verify the user is the client or caregiver involved
        if (role != UserRole.Admin &&
            appointment.ClientId != userId &&
            appointment.CaregiverId != userId)
        {
            _logger.LogWarning("[ChangeRequestController] User {UserId} attempted to reject change request {ChangeRequestId}", userId, changeRequestId);
            return Forbid();
        }

        // Update the change request status
        changeRequest.Status = ChangeRequestStatus.Rejected;
        changeRequest.RespondedAt = DateTime.UtcNow;
        changeRequest.RespondedByUserId = userId;

        bool success = await _changeRequestRepository.UpdateChangeRequest(changeRequest);
        if (!success)
        {
            return BadRequest("Failed to reject change request");
        }

        _logger.LogInformation("[ChangeRequestController] User {UserId} rejected change request {ChangeRequestId}", userId, changeRequestId);

        return Ok(new { message = "Change request rejected" });
    }

    // DELETE: api/ChangeRequest/cancel/{changeRequestId}
    // Cancel a change request (only the requester can cancel their own pending request)
    [HttpDelete("cancel/{changeRequestId}")]
    public async Task<IActionResult> CancelChangeRequest(int changeRequestId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized("Invalid token");
        }

        var changeRequest = await _changeRequestRepository.GetChangeRequestById(changeRequestId);
        if (changeRequest == null)
        {
            return NotFound("Change request not found");
        }

        // Only the requester can cancel their own request
        if (changeRequest.RequestedByUserId != userId)
        {
            _logger.LogWarning("[ChangeRequestController] User {UserId} attempted to cancel change request {ChangeRequestId} by another user", userId, changeRequestId);
            return Forbid();
        }

        if (changeRequest.Status != ChangeRequestStatus.Pending)
        {
            return BadRequest("Only pending change requests can be cancelled");
        }

        bool success = await _changeRequestRepository.DeleteChangeRequest(changeRequestId);
        if (!success)
        {
            return BadRequest("Failed to cancel change request");
        }

        _logger.LogInformation("[ChangeRequestController] User {UserId} cancelled change request {ChangeRequestId}", userId, changeRequestId);

        return Ok(new { message = "Change request cancelled" });
    }
}
