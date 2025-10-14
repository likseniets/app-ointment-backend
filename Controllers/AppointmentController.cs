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
    private readonly AppointmentDbContext _appointmentDbContext;

    public AppointmentController(AppointmentDbContext appointmentDbContext)
    {
        _appointmentDbContext = appointmentDbContext;
    }
    public async Task<IActionResult> Table()
    {
        List<Appointment> appointments = await _appointmentDbContext.Appointment.ToListAsync();
        var appointmentsViewModel = new AppointmentsViewModel(appointments, "Table");
        return View(appointmentsViewModel);
    }
    
    [HttpGet]
    public IActionResult Create()
    {
        return View();
    }

    [HttpPost]
    public IActionResult Create(Appointment appointment)
    {
        if (ModelState.IsValid)
        {
            _appointmentDbContext.Appointment.Add(appointment);
            _appointmentDbContext.SaveChanges();
            return RedirectToAction(nameof(Table));
        }
        return View(appointment);
    }
}