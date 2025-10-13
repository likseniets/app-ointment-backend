using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using app_ointment_backend.Data;
using app_ointment_backend.Models;

namespace app_ointment_backend.Controllers;

public class AppointmentTasksController : Controller
{
    private readonly ApplicationDbContext _context;

    public AppointmentTasksController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: AppointmentTasks/Create?appointmentId=5
    public IActionResult Create(int appointmentId)
    {
        ViewBag.AppointmentId = appointmentId;
        ViewBag.Appointment = _context.Appointments
            .Include(a => a.ElderlyUser)
            .Include(a => a.HealthcarePersonnel)
            .FirstOrDefault(a => a.Id == appointmentId);
        return View();
    }

    // POST: AppointmentTasks/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("AppointmentId,TaskName,TaskType,Description")] AppointmentTask appointmentTask)
    {
        if (ModelState.IsValid)
        {
            _context.Add(appointmentTask);
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", "Appointments", new { id = appointmentTask.AppointmentId });
        }
        
        ViewBag.AppointmentId = appointmentTask.AppointmentId;
        ViewBag.Appointment = _context.Appointments
            .Include(a => a.ElderlyUser)
            .Include(a => a.HealthcarePersonnel)
            .FirstOrDefault(a => a.Id == appointmentTask.AppointmentId);
        return View(appointmentTask);
    }

    // POST: AppointmentTasks/ToggleComplete/5
    [HttpPost]
    public async Task<IActionResult> ToggleComplete(int id)
    {
        var task = await _context.AppointmentTasks.FindAsync(id);
        if (task != null)
        {
            task.IsCompleted = !task.IsCompleted;
            await _context.SaveChangesAsync();
        }
        
        return RedirectToAction("Details", "Appointments", new { id = task?.AppointmentId });
    }

    // GET: AppointmentTasks/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var appointmentTask = await _context.AppointmentTasks
            .Include(a => a.Appointment)
            .FirstOrDefaultAsync(m => m.Id == id);
        
        if (appointmentTask == null)
        {
            return NotFound();
        }

        return View(appointmentTask);
    }

    // POST: AppointmentTasks/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var appointmentTask = await _context.AppointmentTasks.FindAsync(id);
        var appointmentId = appointmentTask?.AppointmentId;
        
        if (appointmentTask != null)
        {
            _context.AppointmentTasks.Remove(appointmentTask);
            await _context.SaveChangesAsync();
        }

        return RedirectToAction("Details", "Appointments", new { id = appointmentId });
    }
}

