using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
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
    public async Task<IActionResult> Table()
    {
        List<User> users = await _userDbContext.Users.ToListAsync();
        var usersViewModel = new UsersViewModel(users, "Table");
        return View(usersViewModel);
    }

    public async Task<IActionResult> Details(int id)
    {
        List<User> users = await _userDbContext.Users.ToListAsync();
        var user = users.FirstOrDefault(i => i.UserId == id);
        if (user == null)
            return NotFound();
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
            try
            {
                _userDbContext.Users.Add(user);
                await _userDbContext.SaveChangesAsync();
                return RedirectToAction(nameof(Table));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Unable to save user. Try again.");
            }
        }
        return View(user);
    }

    [HttpGet]
    public async Task<IActionResult> Update(int id)
    {
        var user = await _userDbContext.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }
        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(User user)
    {
        if (ModelState.IsValid)
        {
            try
            {
                var existing = await _userDbContext.Users.FindAsync(user.UserId);
                if (existing == null)
                {
                    return NotFound();
                }
                // Update selected fields to avoid unintended overwrites
                existing.Name = user.Name;
                existing.Role = user.Role;
                existing.Phone = user.Phone;
                existing.Email = user.Email;
                existing.Adress = user.Adress;
                await _userDbContext.SaveChangesAsync();
                return RedirectToAction(nameof(Table));
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError(string.Empty, "The user was modified by another process. Reload and try again.");
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(string.Empty, "Unable to save changes. Try again.");
            }
        }
        return View(user);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var user = await _userDbContext.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }
        return View(user);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var user = await _userDbContext.Users.FindAsync(id);
        if (user == null)
        {
            return NotFound();
        }
        _userDbContext.Users.Remove(user);
        await _userDbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Table));
    }
}    
