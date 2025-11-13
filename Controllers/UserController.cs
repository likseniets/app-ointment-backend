using Microsoft.AspNetCore.Mvc;
using app_ointment_backend.Models;
using app_ointment_backend.ViewModels;
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
            _logger.LogWarning("[UserController] Login attempt failed - user not found for email {Email}", loginDto.Email);
            return Unauthorized("Invalid email or password");
        }

        // Verify password
        bool isPasswordValid = BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            _logger.LogWarning("[UserController] Login attempt failed - invalid password for email {Email}", loginDto.Email);
            return Unauthorized("Invalid email or password");
        }

        _logger.LogInformation("[UserController] User {UserId} logged in successfully", user.UserId);
        return Ok(UserDto.FromUser(user));
    }

    [HttpGet("table")]
    public async Task<IActionResult> Table()
    {
        var users = await _userRepository.GetAll();
        if (users == null)
        {
            _logger.LogError("[UserController] User list not found while executing _userRepository.GetAll()");
            return NotFound("User list not found");
        }
        var usersViewModel = new UsersViewModel(users, "Table");
        return View(usersViewModel);
    }

    [HttpGet("{userId:int}")]
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

    [HttpGet("details/{userId:int}")]
    public async Task<IActionResult> Details(int userId)
    {
        var user = await _userRepository.GetUserById(userId);
        if (user == null)
        {
            _logger.LogError("[UserController] User not found for the UserId {UserId:0000}", userId);
            return NotFound("User not found");
        }
        return View(user);
    }

    [HttpGet("create")]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost("create/new")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(CreateUserDto userDto)
    {
        if (ModelState.IsValid)
        {
            // Hash the password before creating the user
            var user = new User
            {
                Name = userDto.Name,
                Role = userDto.Role,
                Adress = userDto.Adress,
                Phone = userDto.Phone,
                Email = userDto.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(userDto.Password),
                ImageUrl = userDto.ImageUrl
            };

            bool returnOk = await _userRepository.CreateUser(user);
            if (returnOk)
                return RedirectToAction(nameof(Table));
        }
        _logger.LogWarning("[UserController] user creation failed {@userDto}", userDto);
        return View(userDto);
    }

    [HttpGet("update/{id:int}")]
    public async Task<IActionResult> Update(int id)
    {
        var user = await _userRepository.GetUserById(id);
        if (user == null)
        {
            _logger.LogError("[UserController] User not found when updating the UserId {UserId:0000}", id);
            return BadRequest("User not found");
        }
        return View(user);
    }

    [HttpPost("update")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(User user)
    {
        if (ModelState.IsValid)
        {
            bool returnOk = await _userRepository.UpdateUser(user);
            if (returnOk)
                return RedirectToAction(nameof(Table));
        }
        _logger.LogWarning("[UserController] User Update failed {@user}", user);
        return View(user);
    }

    [HttpGet("delete/{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _userRepository.GetUserById(id);
        if (user == null)
        {
            _logger.LogError("[UserController] User not found for the UserId {UserId:0000}", id);
            return BadRequest("User not found");
        }
        return View(user);
    }

    [HttpPost("delete/{id:int}")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        bool returnOk = await _userRepository.DeleteUser(id);
        if (!returnOk)
        {
            _logger.LogError("[UserController] User deletion failed for the UserId {UserId:0000}", id);
            return BadRequest("User deletion failed");
        }
        return RedirectToAction(nameof(Table));
    }
}
