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
        var user1 = new User
        {
            UserId = 1,
            Name = "Artur",
            Role = UserRole.Admin
        };

        var user2 = new User
        {
            UserId = 2,
            Name = "Eskil",
            Role = UserRole.Caregiver
        };
        
        Appointment newAppointment = new Appointment
        {
            AppointmentId = 1,
            Location = "location",
            Date = DateTime.Now,
            Client = user1,
            Caretaker = user2,
        };
        _appointmentDbContext.Appointment.Add(newAppointment);
        _appointmentDbContext.SaveChanges();
        _appointmentDbContext.Database.EnsureCreated();
        List<Appointment> appointment = _appointmentDbContext.Appointment.ToList();
        return View(appointment);
    }
    
    public void CreateAppointment(int id, string location, DateTime date, User client, User caretaker)
    {
        Appointment newAppointment = new Appointment
        {
            AppointmentId = id,
            Location = location,
            Date = date,
            Client = client,
            Caretaker = caretaker,
        };
        _appointmentDbContext.Appointment.Add(newAppointment);
        _appointmentDbContext.SaveChanges();
    }
}