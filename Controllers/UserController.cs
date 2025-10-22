using Microsoft.AspNetCore.Mvc;
using app_ointment_backend.Models;
using app_ointment_backend.ViewModels;
using app_ointment_backend.DAL;
using Microsoft.AspNetCore.Authorization;

namespace app_ointment_backend.Controllers;

public class UserController : Controller
{
    private readonly IUserRepository _userRepository;

    public UserController(IUserRepository userRepository)
    {
        _userRepository = userRepository; 
    }
    public async Task<IActionResult> Table()
    {
        List<User> users = await _userRepository.GetAll();
        var usersViewModel = new UsersViewModel(users, "Table");
        return View(usersViewModel);
    }

    public async Task<IActionResult> Details(int userId)
    {
        var users = await _userRepository.GetUserById(userId);
        if (users == null)
            return NotFound();
        return View(users);
    }

     [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(User user)
    {
        if (ModelState.IsValid)
        {
            await _userRepository.CreateUser(user);
            return RedirectToAction(nameof(Table));
        }
        return View(user);
    }

    [HttpGet]
    public async Task<IActionResult> Update(int id)
    {
        var user = await _userRepository.GetUserById(id);
        if (user == null)
        {
            return NotFound();
        }
        return View(user);
    }

    [HttpPost]
    public async Task<IActionResult> Update(User user)
    {
        if (ModelState.IsValid)
        {
            await _userRepository.UpdateUser(user);
            return RedirectToAction(nameof(Table));
        }
        return View(user);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _userRepository.GetUserById(id);
        if (user == null)
        {
            return NotFound();
        }
        return View(user);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        await _userRepository.DeleteUser(id);
        return RedirectToAction(nameof(Table));
    }
}    
