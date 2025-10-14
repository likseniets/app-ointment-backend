using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using app_ointment_backend.Models;
using app_ointment_backend.ViewModels;

namespace app_ointment_backend.Controllers;

public class AppointmentController : Controller
{
    private readonly UserDbContext _userDbContext;

    public AppointmentController(UserDbContext userDbContext)
    {
        _userDbContext = userDbContext; 
    }
    public async Task<IActionResult> Table()
    {
        List<Appointment> appointment = await _userDbContext.Appointments.ToListAsync();
        var appointmentsViewModel = new AppointmentsViewModel(appointment, "Table");
        return View(appointmentsViewModel);
    }
    
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Create(Appointment appointment)
    {
        if (ModelState.IsValid)
        {
            _userDbContext.Appointments.Add(appointment);
            await _userDbContext.SaveChangesAsync();
            return RedirectToAction(nameof(Table));
        }
        return View(appointment);
    }

    [HttpGet]
    public async Task<IActionResult> Update(int id)
    {
        var appointment = await _userDbContext.Appointments.FindAsync(id);
        if (appointment == null)
        {
            return NotFound();
        }
        return View(appointment);
    }

    [HttpPost]
    public async Task<IActionResult> Update(Appointment appointment)
    {
        if (ModelState.IsValid)
        {
            _userDbContext.Appointments.Update(appointment);
            await _userDbContext.SaveChangesAsync();
            return RedirectToAction(nameof(Table));
        }
        return View(appointment);
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id)
    {
        var appointment = await _userDbContext.Appointments.FindAsync(id);
        if (appointment == null)
        {
            return NotFound();
        }
        return View(appointment);
    }

    [HttpPost]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var appointment = await _userDbContext.Appointments.FindAsync(id);
        if (appointment == null)
        {
            return NotFound();
        }
        _userDbContext.Appointments.Remove(appointment);
        await _userDbContext.SaveChangesAsync();
        return RedirectToAction(nameof(Table));
    }
}    
