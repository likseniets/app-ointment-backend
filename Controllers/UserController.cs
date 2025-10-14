using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using app_ointment_backend.Models;
using app_ointment_backend.ViewModels;

namespace app_ointment_backend.Controllers;

public class UserController : Controller
{
    private readonly UserDbContext _userDbContext;

    public UserController(UserDbContext userDbContext)
    {
        _userDbContext = userDbContext; 
    }
    public IActionResult Table()
    {
        List<User> users = _userDbContext.Users.ToList();
        var usersViewModel = new UsersViewModel(users, "Table");
        return View(usersViewModel);
    }

     [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Create(User user)
    {
        if (ModelState.IsValid)
        {
            _userDbContext.Users.Add(user);
            _userDbContext.SaveChanges();
            return RedirectToAction(nameof(Table));
        }
        return View(user);
    }    
}