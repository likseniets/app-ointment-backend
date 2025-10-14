using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using app_ointment_backend.Models;

namespace app_ointment_backend.Controllers;

public class AppointmentController : Controller
{
    private readonly AppointmentDbContext _appointmentDbContext;

    public AppointmentController(AppointmentDbContext appointmentDbContext)
    {
        _appointmentDbContext = appointmentDbContext;
    }
    public IActionResult Table()
    {
        List<Appointment> appointment = _appointmentDbContext.Appointment.ToList();
        return View(appointment);
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