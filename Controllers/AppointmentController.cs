using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using app_ointment_backend.Models;
using app_ointment_backend.DAL;
using Microsoft.EntityFrameworkCore;

namespace app_ointment_backend.Controllers;

public class AppointmentController : Controller
{
    private readonly UserDbContext _userDbContext;

    public AppointmentController(UserDbContext userDbContext)
    {
        _userDbContext = userDbContext; 
    }
    // FIKSET: Fjernet duplikate .Appointments.ToListAsync() kall som forårsaket syntax-feil
    public async Task<IActionResult> Table()
    {
        var appointments = await _userDbContext.Appointments.ToListAsync();
        return View(appointments);
    }
    //{
      //  List<Appointment> appointment = await _userDbContext.Appointments.ToList();
       // return View(appointment);
    //}
    
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
}