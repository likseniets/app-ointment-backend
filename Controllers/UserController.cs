using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using app_ointment_backend.Models;
using app_ointment_backend.Services;

namespace app_ointment_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : Controller
{
    private readonly IUserService _userService;
    private readonly ILogger<UserController> _logger;

    public UserController(IUserService userService, ILogger<UserController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    // HttpGet for getting the current user based on JWT claims
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            _logger.LogError("[UserController] Unable to get user ID from JWT claims");
            return Unauthorized("Invalid token");
        }

        var user = await _userService.GetUserById(userId);
        if (user == null)
        {
            _logger.LogError("[UserController] User not found for UserId {UserId}", userId);
            return NotFound("User not found");
        }

        return Ok(UserDto.FromUser(user));
    }

    // HttpGet for getting all users
    [HttpGet("all")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _userService.GetAllUsers();
        if (users == null)
        {
            _logger.LogError("[UserController] User list not found");
            return NotFound("User list not found");
        }
        var userDtos = users.Select(UserDto.FromUser);
        return Ok(userDtos);
    }

    // HttpGet for getting user details by user ID
    [HttpGet("{userId:int}")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> userDetails(int userId)
    {
        var user = await _userService.GetUserById(userId);
        if (user == null)
        {
            _logger.LogError("[UserController] User not found for the UserId {UserId:0000}", userId);
            return NotFound("User not found");
        }
        return Ok(UserDto.FromUser(user));
    }

    // HttpGet for getting all caregivers with their availability
    [HttpGet("caregivers")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<GetCaregiverWithAvailability>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetCaregivers()
    {
        var caregivers = await _userService.GetCaregivers();
        if (caregivers == null)
        {
            _logger.LogError("[UserController] Caregiver list not found");
            return NotFound("Caregiver list not found");
        }
        var caregiverDtos = caregivers
            .OfType<Caregiver>()
            .Select(GetCaregiverWithAvailability.FromCaregiver);
        return Ok(caregiverDtos);
    }

    // HttpGet for getting all clients
    [HttpGet("clients")]
    [Authorize]
    [ProducesResponseType(typeof(IEnumerable<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetClients()
    {
        var clients = await _userService.GetClients();
        if (clients == null)
        {
            _logger.LogError("[UserController] Client list not found");
            return NotFound("Client list not found");
        }
        var clientDtos = clients.Select(UserDto.FromUser);
        return Ok(clientDtos);
    }

    // HttpPost for registering a new client
    [HttpPost("create/client")]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> RegisterClient([FromBody] CreateUserDto newUserDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var result = await _userService.CreateClient(newUserDto);
        
        if (!result.Success)
        {
            return BadRequest(result.Message);
        }

        return CreatedAtAction(nameof(userDetails), new { userId = result.User!.UserId }, UserDto.FromUser(result.User));
    }

    // HttpPost for creating a new caregiver
    [HttpPost("create/caregiver")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateCaregiver([FromBody] CreateUserDto newUserDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
        if (!Enum.TryParse<UserRole>(roleClaim, out var currentRole) || currentRole != UserRole.Admin)
        {
            _logger.LogWarning("[UserController] Non-admin user attempted to create caregiver");
            return Forbid();
        }

        var result = await _userService.CreateCaregiver(newUserDto);
        
        if (!result.Success)
        {
            return BadRequest(result.Message);
        }

        return CreatedAtAction(nameof(userDetails), new { userId = result.User!.UserId }, UserDto.FromUser(result.User));
    }

    // HttpPost for creating a new admin
    [HttpPost("create/admin")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    public async Task<IActionResult> CreateAdmin([FromBody] CreateUserDto newUserDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;
        if (!Enum.TryParse<UserRole>(roleClaim, out var currentRole) || currentRole != UserRole.Admin)
        {
            _logger.LogWarning("[UserController] Non-admin user attempted to create admin");
            return Forbid();
        }

        var result = await _userService.CreateAdmin(newUserDto);
        
        if (!result.Success)
        {
            return BadRequest(result.Message);
        }

        return CreatedAtAction(nameof(userDetails), new { userId = result.User!.UserId }, UserDto.FromUser(result.User));
    }

    // HttpPut for updating a user
    [HttpPut("update/{userId:int}")]
    [Authorize]
    [ProducesResponseType(typeof(UserDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateUser(int userId, [FromBody] UpdateUserDto updateDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int currentUserId))
        {
            _logger.LogError("[UserController] Unable to get user ID from JWT claims");
            return Unauthorized("Invalid token");
        }

        if (!Enum.TryParse<UserRole>(roleClaim, out var currentRole))
        {
            return Unauthorized("Invalid role");
        }

        // Users can only update their own profile unless they're admin
        if (currentRole != UserRole.Admin && userId != currentUserId)
        {
            _logger.LogWarning("[UserController] User {CurrentUserId} attempted to update user {UserId}", currentUserId, userId);
            return Forbid();
        }

        var result = await _userService.UpdateUser(userId, updateDto);
        
        if (!result.Success)
        {
            return BadRequest(result.Message);
        }

        var user = await _userService.GetUserById(userId);
        return Ok(UserDto.FromUser(user!));
    }

    // HttpPost for changing password
    [HttpPost("change-password")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            _logger.LogError("[UserController] Unable to get user ID from JWT claims");
            return Unauthorized("Invalid token");
        }

        var result = await _userService.ChangePassword(userId, changePasswordDto.CurrentPassword, changePasswordDto.NewPassword);
        
        if (!result.Success)
        {
            return BadRequest(result.Message);
        }

        return Ok(new { message = result.Message });
    }

    // HttpDelete for deleting a user
    [HttpDelete("delete/{userId:int}")]
    [Authorize]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteUser(int userId)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        var roleClaim = User.FindFirst(ClaimTypes.Role)?.Value;

        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int currentUserId))
        {
            _logger.LogError("[UserController] Unable to get user ID from JWT claims");
            return Unauthorized("Invalid token");
        }

        if (!Enum.TryParse<UserRole>(roleClaim, out var currentRole))
        {
            return Unauthorized("Invalid role");
        }

        // Users can only delete their own account unless they're admin
        if (currentRole != UserRole.Admin && userId != currentUserId)
        {
            _logger.LogWarning("[UserController] User {CurrentUserId} attempted to delete user {UserId}", currentUserId, userId);
            return Forbid();
        }

        var result = await _userService.DeleteUser(userId);
        
        if (!result.Success)
        {
            return BadRequest(result.Message);
        }

        return Ok(new { message = result.Message });
    }
}
