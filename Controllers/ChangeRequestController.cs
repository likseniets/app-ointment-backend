using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using app_ointment_backend.Models;
using app_ointment_backend.Services;

namespace app_ointment_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ChangeRequestController : ControllerBase
{
    private readonly IChangeRequestService _changeRequestService;
    private readonly ILogger<ChangeRequestController> _logger;

    public ChangeRequestController(
        IChangeRequestService changeRequestService,
        ILogger<ChangeRequestController> logger)
    {
        _changeRequestService = changeRequestService;
        _logger = logger;
    }

    // HttpGet for getting pending change requests for the current user
    [HttpGet("pending")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<AppointmentChangeRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPendingRequests()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized("Invalid token");
        }

        var requests = await _changeRequestService.GetPendingChangeRequestsForUser(userId);
        if (requests == null)
        {
            return NotFound("No pending change requests found");
        }

        var requestDtos = requests.Select(AppointmentChangeRequestDto.FromChangeRequest);
        return Ok(requestDtos);
    }

    // HttpGet for getting change requests made by the current user
    [HttpGet("requested")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<AppointmentChangeRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetRequested()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized("Invalid token");
        }

        var requests = await _changeRequestService.GetChangeRequestsByUser(userId);
        if (requests == null)
        {
            return NotFound("No pending change requests found");
        }

        var requestDtos = requests.Select(AppointmentChangeRequestDto.FromChangeRequest);
        return Ok(requestDtos);
    }

    // HttpGet for getting change requests by appointment ID
    [HttpGet("appointment/{appointmentId}")]
    [ProducesResponseType(typeof(IEnumerable<AppointmentChangeRequestDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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

        var (requests, success, message) = await _changeRequestService.GetChangeRequestsByAppointment(appointmentId, userId, role);
        if (!success)
        {
            if (message == "Access denied")
            {
                return Forbid();
            }
            return NotFound(message);
        }

        var requestDtos = requests?.Select(AppointmentChangeRequestDto.FromChangeRequest) ?? Enumerable.Empty<AppointmentChangeRequestDto>();
        return Ok(requestDtos);
    }

    // HttpPost for creating a change request
    [HttpPost("create")]
    [Authorize]
    [ProducesResponseType(typeof(AppointmentChangeRequestDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CreateChangeRequest([FromBody] CreateChangeRequestDto requestDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized("Invalid token");
        }

        var (success, message, createdRequest) = await _changeRequestService.CreateChangeRequest(requestDto, userId);
        
        if (!success)
        {
            if (message == "Access denied")
            {
                return Forbid();
            }
            if (message == "Appointment not found")
            {
                return NotFound(message);
            }
            return BadRequest(message);
        }

        if (createdRequest != null)
        {
            return Ok(AppointmentChangeRequestDto.FromChangeRequest(createdRequest));
        }

        return Ok(new { message });
    }

    // HttpPut for approving a change request
    [HttpPut("approve/{changeRequestId}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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

        var (success, message) = await _changeRequestService.ApproveChangeRequest(changeRequestId, userId, role);
        
        if (!success)
        {
            if (message == "Change request not found" || message == "Appointment not found")
            {
                return NotFound(message);
            }
            if (message == "Access denied")
            {
                return Forbid();
            }
            return BadRequest(message);
        }

        return Ok(new { message });
    }

    // HttpPut for rejecting a change request
    [HttpPut("reject/{changeRequestId}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
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

        var (success, message) = await _changeRequestService.RejectChangeRequest(changeRequestId, userId, role, null);
        
        if (!success)
        {
            if (message == "Change request not found" || message == "Appointment not found")
            {
                return NotFound(message);
            }
            if (message == "Access denied")
            {
                return Forbid();
            }
            return BadRequest(message);
        }

        return Ok(new { message });
    }

    // HttpDelete for cancelling a change request
    [HttpDelete("cancel/{changeRequestId}")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> CancelChangeRequest(int changeRequestId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            return Unauthorized("Invalid token");
        }

        var (success, message) = await _changeRequestService.CancelChangeRequest(changeRequestId, userId);
        
        if (!success)
        {
            if (message == "Change request not found")
            {
                return NotFound(message);
            }
            if (message == "Access denied")
            {
                return Forbid();
            }
            return BadRequest(message);
        }

        return Ok(new { message });
    }
}
