using Microsoft.AspNetCore.Mvc;
using app_ointment_backend.Models;
using app_ointment_backend.ViewModels;
using app_ointment_backend.DAL;

namespace app_ointment_backend.Controllers;

// UserController setup based on course demos for ItemController
public class UserController : Controller
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserController> _logger;

    public UserController(IUserRepository userRepository, ILogger<UserController> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }
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

    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(User user)
    {
        if (ModelState.IsValid)
        {
            // Create the appropriate user type based on role
            User userToCreate;
            switch (user.Role)
            {
                case UserRole.Caregiver:
                    userToCreate = new Caregiver
                    {
                        Name = user.Name,
                        Role = user.Role,
                        Adress = user.Adress,
                        Phone = user.Phone,
                        Email = user.Email,
                        ImageUrl = user.ImageUrl
                    };
                    break;
                case UserRole.Client:
                    userToCreate = new Client
                    {
                        Name = user.Name,
                        Role = user.Role,
                        Adress = user.Adress,
                        Phone = user.Phone,
                        Email = user.Email,
                        ImageUrl = user.ImageUrl
                    };
                    break;
                default:
                    userToCreate = new User
                    {
                        Name = user.Name,
                        Role = user.Role,
                        Adress = user.Adress,
                        Phone = user.Phone,
                        Email = user.Email,
                        ImageUrl = user.ImageUrl
                    };
                    break;
            }

            bool returnOk = await _userRepository.CreateUser(userToCreate);
            if (returnOk)
                return RedirectToAction(nameof(Table));
        }
        _logger.LogWarning("[UserController] user creation failed {@user}", user);
        return View(user);
    }

    [HttpGet]
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

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(User user)
    {
        if (ModelState.IsValid)
        {
            // Create the appropriate user type based on role for update
            User userToUpdate;
            switch (user.Role)
            {
                case UserRole.Caregiver:
                    userToUpdate = new Caregiver
                    {
                        UserId = user.UserId,
                        Name = user.Name,
                        Role = user.Role,
                        Adress = user.Adress,
                        Phone = user.Phone,
                        Email = user.Email,
                        ImageUrl = user.ImageUrl
                    };
                    break;
                case UserRole.Client:
                    userToUpdate = new Client
                    {
                        UserId = user.UserId,
                        Name = user.Name,
                        Role = user.Role,
                        Adress = user.Adress,
                        Phone = user.Phone,
                        Email = user.Email,
                        ImageUrl = user.ImageUrl
                    };
                    break;
                default:
                    userToUpdate = new User
                    {
                        UserId = user.UserId,
                        Name = user.Name,
                        Role = user.Role,
                        Adress = user.Adress,
                        Phone = user.Phone,
                        Email = user.Email,
                        ImageUrl = user.ImageUrl
                    };
                    break;
            }

            bool returnOk = await _userRepository.UpdateUser(userToUpdate);
            if (returnOk)
                return RedirectToAction(nameof(Table));
        }
        _logger.LogWarning("[UserController] User Update failed {@user}", user);
        return View(user);
    }

    [HttpGet]
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

    [HttpPost]
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
