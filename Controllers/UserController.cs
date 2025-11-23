using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using app_ointment_backend.Models;
using app_ointment_backend.DAL;
using BCrypt.Net;

namespace app_ointment_backend.Controllers;

// UserController setup based on course demos for ItemController
[ApiController]
[Route("api/[controller]")]
public class UserController : Controller
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserController> _logger;

    public UserController(IUserRepository userRepository, ILogger<UserController> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetCurrentUser()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
        {
            _logger.LogError("[UserController] Unable to get user ID from JWT claims");
            return Unauthorized("Invalid token");
        }

        var user = await _userRepository.GetUserById(userId);
        if (user == null)
        {
            _logger.LogError("[UserController] User not found for UserId {UserId}", userId);
            return NotFound("User not found");
        }

        return Ok(UserDto.FromUser(user));
    }

    [HttpGet("all")]
    [Authorize]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _userRepository.GetAll();
        if (users == null)
        {
            _logger.LogError("[UserController] User list not found while executing _userRepository.GetAll()");
            return NotFound("User list not found");
        }
        var userDtos = users.Select(UserDto.FromUser);
        return Ok(userDtos);
    }

    [HttpGet("{userId:int}")]
    [Authorize]
    public async Task<IActionResult> userDetails(int userId)
    {
        var user = await _userRepository.GetUserById(userId);
        if (user == null)
        {
            _logger.LogError("[UserController] User not found for the UserId {UserId:0000}", userId);
            return NotFound("User not found");
        }
        return Ok(UserDto.FromUser(user));
    }

    [HttpGet("caregivers")]
    [Authorize]
    public async Task<IActionResult> GetCaregivers()
    {
        var caregivers = await _userRepository.GetCaregivers();
        if (caregivers == null)
        {
            _logger.LogError("[UserController] Caregiver list not found while executing _userRepository.GetCaregivers()");
            return NotFound("Caregiver list not found");
        }
        var caregiverDtos = caregivers.Select(GetCaregiverWithAvailability.FromCaregiver);
        return Ok(caregiverDtos);
    }

    [HttpGet("clients")]
    [Authorize]
    public async Task<IActionResult> GetClients()
    {
        var clients = await _userRepository.GetClients();
        if (clients == null)
        {
            _logger.LogError("[UserController] Client list not found while executing _userRepository.GetClients()");
            return NotFound("Client list not found");
        }
        var clientDtos = clients.Select(UserDto.FromUser);
        return Ok(clientDtos);
    }

    [HttpPost("create/client")]
    public async Task<IActionResult> RegisterClient([FromBody] CreateUserDto newUserDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Check if email already exists
        var existingUser = await _userRepository.GetUserByEmail(newUserDto.Email);
        if (existingUser != null)
        {
            _logger.LogWarning("[UserController] Email {Email} already exists", newUserDto.Email);
            return BadRequest("Email already registered");
        }

        // Hash the password
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(newUserDto.Password);

        // Create client
        var newUser = new Client
        {
            Name = newUserDto.Name,
            Adress = newUserDto.Adress,
            Phone = newUserDto.Phone,
            Email = newUserDto.Email,
            PasswordHash = passwordHash,
            ImageUrl = newUserDto.ImageUrl ?? string.Empty
        };

        await _userRepository.CreateUser(newUser);
        _logger.LogInformation("[UserController] Client {Email} registered successfully", newUser.Email);

        return CreatedAtAction(nameof(userDetails), new { userId = newUser.UserId }, UserDto.FromUser(newUser));
    }

    [HttpPost("create/caregiver")]
    [Authorize]
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

        // Check if email already exists
        var existingUser = await _userRepository.GetUserByEmail(newUserDto.Email);
        if (existingUser != null)
        {
            _logger.LogWarning("[UserController] Email {Email} already exists", newUserDto.Email);
            return BadRequest("Email already registered");
        }

        // Hash the password
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(newUserDto.Password);

        // Create caregiver
        var newUser = new Caregiver
        {
            Name = newUserDto.Name,
            Adress = newUserDto.Adress,
            Phone = newUserDto.Phone,
            Email = newUserDto.Email,
            PasswordHash = passwordHash,
            ImageUrl = newUserDto.ImageUrl ?? string.Empty
        };

        await _userRepository.CreateUser(newUser);
        _logger.LogInformation("[UserController] Caregiver {Email} created successfully by admin", newUser.Email);

        return CreatedAtAction(nameof(userDetails), new { userId = newUser.UserId }, UserDto.FromUser(newUser));
    }

    [HttpPost("create/admin")]
    [Authorize]
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

        // Check if email already exists
        var existingUser = await _userRepository.GetUserByEmail(newUserDto.Email);
        if (existingUser != null)
        {
            _logger.LogWarning("[UserController] Email {Email} already exists", newUserDto.Email);
            return BadRequest("Email already registered");
        }

        // Hash the password
        var passwordHash = BCrypt.Net.BCrypt.HashPassword(newUserDto.Password);

        // Create admin user (using Client as base since we don't have Admin-specific class)
        var newUser = new Client
        {
            Name = newUserDto.Name,
            Adress = newUserDto.Adress,
            Phone = newUserDto.Phone,
            Email = newUserDto.Email,
            PasswordHash = passwordHash,
            ImageUrl = newUserDto.ImageUrl ?? string.Empty,
            Role = UserRole.Admin
        };

        await _userRepository.CreateUser(newUser);
        _logger.LogInformation("[UserController] Admin {Email} created successfully", newUser.Email);

        return CreatedAtAction(nameof(userDetails), new { userId = newUser.UserId }, UserDto.FromUser(newUser));
    }

    [HttpPut("update/{userId:int}")]
    [Authorize]
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

        var user = await _userRepository.GetUserById(userId);
        if (user == null)
        {
            _logger.LogError("[UserController] User not found for UserId {UserId}", userId);
            return NotFound("User not found");
        }

        // Check if email is being changed to an existing email
        if (user.Email != updateDto.Email)
        {
            var existingUser = await _userRepository.GetUserByEmail(updateDto.Email);
            if (existingUser != null)
            {
                _logger.LogWarning("[UserController] Email {Email} already exists", updateDto.Email);
                return BadRequest("Email already registered");
            }
        }

        // Update user properties
        user.Name = updateDto.Name;
        user.Adress = updateDto.Adress;
        user.Phone = updateDto.Phone;
        user.Email = updateDto.Email;
        user.ImageUrl = updateDto.ImageUrl ?? user.ImageUrl;

        await _userRepository.UpdateUser(user);
        _logger.LogInformation("[UserController] User {UserId} updated successfully", userId);

        return Ok(UserDto.FromUser(user));
    }

    [HttpPost("change-password")]
    [Authorize]
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

        var user = await _userRepository.GetUserById(userId);
        if (user == null)
        {
            _logger.LogError("[UserController] User not found for UserId {UserId}", userId);
            return NotFound("User not found");
        }

        // Verify current password
        bool isCurrentPasswordValid = BCrypt.Net.BCrypt.Verify(changePasswordDto.CurrentPassword, user.PasswordHash);
        if (!isCurrentPasswordValid)
        {
            _logger.LogWarning("[UserController] Invalid current password for user {UserId}", userId);
            return BadRequest("Current password is incorrect");
        }

        // Update to new password
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(changePasswordDto.NewPassword);

        await _userRepository.UpdateUser(user);
        _logger.LogInformation("[UserController] Password changed successfully for user {UserId}", userId);

        return Ok(new { message = "Password changed successfully" });
    }

    [HttpDelete("delete/{userId:int}")]
    [Authorize]
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

        var user = await _userRepository.GetUserById(userId);
        if (user == null)
        {
            _logger.LogError("[UserController] User not found for UserId {UserId}", userId);
            return NotFound("User not found");
        }

        try
        {
            bool success = await _userRepository.DeleteUser(userId);
            if (success)
            {
                _logger.LogInformation("[UserController] User {UserId} deleted successfully", userId);
                return Ok(new { message = "User deleted successfully" });
            }

            return BadRequest("Failed to delete user");
        }
        catch (Exception ex)
        {
            _logger.LogError("[UserController] Failed to delete user {UserId}: {Error}", userId, ex.Message);
            return BadRequest("Unable to delete user. Please try again.");
        }
    }
}
