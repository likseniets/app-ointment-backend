using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using app_ointment_backend.Data;
using app_ointment_backend.Models;

namespace app_ointment_backend.Controllers;

public class AvailableDaysController : Controller
{
    private readonly ApplicationDbContext _context;

    public AvailableDaysController(ApplicationDbContext context)
    {
        _context = context;
    }

    // GET: AvailableDays
    public async Task<IActionResult> Index()
    {
        var availableDays = await _context.AvailableDays
            .Include(a => a.HealthcarePersonnel)
            .Where(a => a.Date >= DateTime.Today)
            .OrderBy(a => a.Date)
            .ToListAsync();
        
        // Sort by time on client side since SQLite doesn't support TimeSpan ordering
        availableDays = availableDays
            .OrderBy(a => a.Date)
            .ThenBy(a => a.StartTime)
            .ToList();
        
        return View(availableDays);
    }

    // GET: AvailableDays/Create
    public IActionResult Create()
    {
        ViewBag.HealthcarePersonnel = _context.Users.Where(u => u.Role == UserRole.HealthcarePersonnel).ToList();
        return View();
    }

    // POST: AvailableDays/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("HealthcarePersonnelId,Date,StartTime,EndTime,Notes")] AvailableDay availableDay)
    {
        if (ModelState.IsValid)
        {
            _context.Add(availableDay);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        
        ViewBag.HealthcarePersonnel = _context.Users.Where(u => u.Role == UserRole.HealthcarePersonnel).ToList();
        return View(availableDay);
    }

    // GET: AvailableDays/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var availableDay = await _context.AvailableDays.FindAsync(id);
        if (availableDay == null)
        {
            return NotFound();
        }
        
        ViewBag.HealthcarePersonnel = _context.Users.Where(u => u.Role == UserRole.HealthcarePersonnel).ToList();
        return View(availableDay);
    }

    // POST: AvailableDays/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,HealthcarePersonnelId,Date,StartTime,EndTime,IsBooked,Notes")] AvailableDay availableDay)
    {
        if (id != availableDay.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(availableDay);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!AvailableDayExists(availableDay.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        
        ViewBag.HealthcarePersonnel = _context.Users.Where(u => u.Role == UserRole.HealthcarePersonnel).ToList();
        return View(availableDay);
    }

    // GET: AvailableDays/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var availableDay = await _context.AvailableDays
            .Include(a => a.HealthcarePersonnel)
            .FirstOrDefaultAsync(m => m.Id == id);
        
        if (availableDay == null)
        {
            return NotFound();
        }

        return View(availableDay);
    }

    // POST: AvailableDays/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var availableDay = await _context.AvailableDays.FindAsync(id);
        if (availableDay != null)
        {
            _context.AvailableDays.Remove(availableDay);
        }

        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    private bool AvailableDayExists(int id)
    {
        return _context.AvailableDays.Any(e => e.Id == id);
    }
}

