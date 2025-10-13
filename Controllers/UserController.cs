using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using app_ointment_backend.Models;

namespace app_ointment_backend.Controllers;

public class UserController : Controller
{
    public IActionResult Table()
    {
        var users = new List<User>();
        var user1 = new User();
        user1.UserId = 1;
        user1.Name = "Artur";
        user1.Role = UserRole.Admin;

        var user2 = new User
        {
            UserId = 2,
            Name = "Eskil",
            Role = UserRole.Caregiver
        };

        users.Add(user1);
        users.Add(user2);

        ViewBag.CurrentViewName = "List of Users";
        return View(users);
    }
}