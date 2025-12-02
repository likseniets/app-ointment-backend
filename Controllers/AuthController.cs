using Microsoft.AspNetCore.Mvc;
using app_ointment_backend.Models;
using app_ointment_backend.Services;

namespace app_ointment_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IUserService userService, IJwtService jwtService, ILogger<AuthController> logger)
    {
        _userService = userService;
        _jwtService = jwtService;
        _logger = logger;
    }

    // HttpPost for user login
    [HttpPost("login")]
    [ProducesResponseType(typeof(object), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        // Get user by email
        var user = await _userService.GetUserByEmail(loginDto.Email);
        if (user == null)
        {
            _logger.LogWarning("[AuthController] Login attempt failed - user not found for email {Email}", loginDto.Email);
            return Unauthorized("Invalid email or password");
        }

        // Verify password using service
        var isValid = await _userService.ValidatePassword(user.UserId, loginDto.Password);
        if (!isValid)
        {
            _logger.LogWarning("[AuthController] Login attempt failed - invalid password for email {Email}", loginDto.Email);
            return Unauthorized("Invalid email or password");
        }

        // Generate JWT token
        var token = _jwtService.GenerateToken(user);

        _logger.LogInformation("[AuthController] User {UserId} logged in successfully", user.UserId);
        // Return token and user info
        return Ok(new 
        { 
            token = token,
            user = UserDto.FromUser(user)
        });
    }
}
