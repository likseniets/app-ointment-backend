using Microsoft.AspNetCore.Mvc;
using app_ointment_backend.Models;
using app_ointment_backend.DAL;
using app_ointment_backend.Services;
using BCrypt.Net;

namespace app_ointment_backend.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly ILogger<AuthController> _logger;

    public AuthController(IUserRepository userRepository, IJwtService jwtService, ILogger<AuthController> logger)
    {
        _userRepository = userRepository;
        _jwtService = jwtService;
        _logger = logger;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
    {
        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var user = await _userRepository.GetUserByEmail(loginDto.Email);
        if (user == null)
        {
            _logger.LogWarning("[AuthController] Login attempt failed - user not found for email {Email}", loginDto.Email);
            return Unauthorized("Invalid email or password");
        }

        // Verify password
        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            _logger.LogWarning("[AuthController] Login attempt failed - invalid password for email {Email}", loginDto.Email);
            return Unauthorized("Invalid email or password");
        }

        // Generate JWT token
        var token = _jwtService.GenerateToken(user);

        _logger.LogInformation("[AuthController] User {UserId} logged in successfully", user.UserId);
        return Ok(new 
        { 
            token = token,
            user = UserDto.FromUser(user)
        });
    }
}
