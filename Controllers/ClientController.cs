using Microsoft.AspNetCore.Mvc;
using app_ointment_backend.Models;
using app_ointment_backend.DAL;
using Microsoft.EntityFrameworkCore;

namespace app_ointment_backend.Controllers;

/// <summary>
/// NY FIL: ClientController - Opprettet for å håndtere klient-funksjonalitet
/// Dette er en ny controller som lar klienter booke avtaler basert på omsorgspersonenes tilgjengelighet
/// </summary>
public class ClientController : Controller
{
    private readonly UserDbContext _context;

    public ClientController(UserDbContext context)
    {
        _context = context;
    }

    /// <summary>
    /// ENDRET: Index - Viser både tilgjengelige omsorgspersoner og klientens avtaler
    /// Kombinerer booking-funksjonalitet med oversikt over egne avtaler
    /// </summary>
    public async Task<IActionResult> Index()
    {
        // Hent tilgjengelige omsorgspersoner
        var caregivers = await _context.Caregivers
            .Include(c => c.Availability)
            .ToListAsync();

        // Finn første tilgjengelig klient for å vise deres avtaler
        var client = await _context.Clients.FirstOrDefaultAsync();
        var appointments = new List<Appointment>();
        
        if (client != null)
        {
            appointments = await _context.Appointments
                .Where(a => a.ClientId == client.UserId)
                .Include(a => a.Caregiver)
                .OrderBy(a => a.Date)
                .ToListAsync();
        }

        // Opprett ViewModel for å sende begge deler til view
        var viewModel = new ClientDashboardViewModel
        {
            Caregivers = caregivers,
            Appointments = appointments
        };

        return View(viewModel);
    }

    /// <summary>
    /// ENDRET: Book - Deler opp tilgjengelighet i mindre tidsrom (1-timers intervaller)
    /// Klienter kan nå booke spesifikke tidsrom i stedet for hele dagen
    /// </summary>
    public async Task<IActionResult> Book(int caregiverId)
    {
        var caregiver = await _context.Caregivers
            .Where(c => c.UserId == caregiverId)
            .Include(c => c.Availability)
            .FirstOrDefaultAsync();

        if (caregiver == null)
        {
            return NotFound();
        }

        // Hent tilgjengelighet for fremtidige datoer
        var futureAvailability = caregiver.Availability?
            .Where(a => a.Date >= DateTime.Today)
            .OrderBy(a => a.Date)
            .ThenBy(a => a.StartTime)
            .ToList() ?? new List<Availability>();

        // LAGT TIL: Del opp hver tilgjengelighet i 1-timers intervaller
        var timeSlots = new List<TimeSlotViewModel>();
        
        foreach (var availability in futureAvailability)
        {
            var startTime = TimeSpan.Parse(availability.StartTime);
            var endTime = TimeSpan.Parse(availability.EndTime);
            
            // Opprett 1-timers intervaller
            var currentTime = startTime;
            while (currentTime < endTime)
            {
                var slotEndTime = currentTime.Add(TimeSpan.FromHours(1));
                if (slotEndTime > endTime)
                {
                    slotEndTime = endTime;
                }
                
                timeSlots.Add(new TimeSlotViewModel
                {
                    Date = availability.Date,
                    StartTime = currentTime.ToString(@"hh\:mm"),
                    EndTime = slotEndTime.ToString(@"hh\:mm"),
                    Description = availability.Description,
                    AvailabilityId = availability.AvailabilityId
                });
                
                currentTime = currentTime.Add(TimeSpan.FromHours(1));
            }
        }

        ViewBag.Caregiver = caregiver;
        return View(timeSlots);
    }

    /// <summary>
    /// ENDRET: BookAppointment - Oppdatert for å håndtere dropdown-valg
    /// Parser nå timeSlot-parameter fra dropdown-meny
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> BookAppointment(int caregiverId, string timeSlot, string location)
    {
        try
        {
            // LAGT TIL: Validering av timeSlot-parameter
            if (string.IsNullOrEmpty(timeSlot))
            {
                TempData["Error"] = "Please select a time slot.";
                return RedirectToAction("Book", new { caregiverId });
            }

            // LAGT TIL: Parse timeSlot-parameter (format: "date|startTime|endTime")
            var timeSlotParts = timeSlot.Split('|');
            if (timeSlotParts.Length != 3)
            {
                TempData["Error"] = "Invalid time slot format.";
                return RedirectToAction("Book", new { caregiverId });
            }

            var date = DateTime.Parse(timeSlotParts[0]);
            var startTime = timeSlotParts[1];
            var endTime = timeSlotParts[2];

            Console.WriteLine($"Booking attempt - CaregiverId: {caregiverId}, Date: {date}, StartTime: {startTime}, EndTime: {endTime}");
            
            // Sjekk om det valgte tidsrommet er innenfor tilgjengelighet
            var availability = await _context.Availabilities
                .Where(a => a.CaregiverId == caregiverId && 
                           a.Date.Date == date.Date)
                .FirstOrDefaultAsync();

            if (availability == null)
            {
                TempData["Error"] = "Selected time slot is not available.";
                return RedirectToAction("Book", new { caregiverId });
            }

            // Sjekk om det valgte tidsrommet er innenfor tilgjengelighet
            var requestedStart = TimeSpan.Parse(startTime);
            var requestedEnd = TimeSpan.Parse(endTime);
            var availableStart = TimeSpan.Parse(availability.StartTime);
            var availableEnd = TimeSpan.Parse(availability.EndTime);

            if (requestedStart < availableStart || requestedEnd > availableEnd)
            {
                TempData["Error"] = "Selected time slot is outside available hours.";
                return RedirectToAction("Book", new { caregiverId });
            }

            // Sjekk for overlappende avtaler med spesifikk tid
            var appointmentStart = date.Date.Add(requestedStart);
            var appointmentEnd = date.Date.Add(requestedEnd);
            
            var existingAppointment = await _context.Appointments
                .Where(a => a.CaregiverId == caregiverId && 
                           a.Date >= appointmentStart && 
                           a.Date < appointmentEnd)
                .FirstOrDefaultAsync();

            if (existingAppointment != null)
            {
                TempData["Error"] = "This time slot is already booked.";
                return RedirectToAction("Book", new { caregiverId });
            }

            // Finn første tilgjengelig klient
            var client = await _context.Clients.FirstOrDefaultAsync();
            if (client == null)
            {
                TempData["Error"] = "No clients available for booking.";
                return RedirectToAction("Book", new { caregiverId });
            }

            Console.WriteLine($"Using client: {client.Name} (ID: {client.UserId})");

            // Opprett ny avtale med spesifikk tid
            var appointment = new Appointment
            {
                CaregiverId = caregiverId,
                ClientId = client.UserId,
                Date = appointmentStart,
                Location = location ?? "TBD"
            };

            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Appointment booked successfully for {startTime}-{endTime}!";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Booking error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            TempData["Error"] = $"An error occurred while booking the appointment: {ex.Message}";
            return RedirectToAction("Book", new { caregiverId });
        }
    }

    /// <summary>
    /// NY METODE: MyAppointments - Viser klientens egne avtaler
    /// ENDRET: Lagt til logikk for å finne første tilgjengelige klient
    /// </summary>
    public async Task<IActionResult> MyAppointments(int clientId = 1) // For nå, hardkodet
    {
        // LAGT TIL: Finn første tilgjengelig klient hvis ingen spesifisert
        if (clientId == 1)
        {
            var client = await _context.Clients.FirstOrDefaultAsync();
            if (client != null)
            {
                clientId = client.UserId;
            }
        }

        var appointments = await _context.Appointments
            .Where(a => a.ClientId == clientId)
            .Include(a => a.Caregiver)
            .OrderBy(a => a.Date)
            .ToListAsync();

        return View(appointments);
    }

    /// <summary>
    /// NY METODE: DeleteAppointment - Lar klienter slette sine egne avtaler
    /// Sjekker at avtalen tilhører klienten før sletting
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> DeleteAppointment(int appointmentId)
    {
        try
        {
            // Finn første tilgjengelig klient (for testing)
            var client = await _context.Clients.FirstOrDefaultAsync();
            if (client == null)
            {
                TempData["Error"] = "No client found.";
                return RedirectToAction("Index");
            }

            // Finn avtalen og sjekk at den tilhører klienten
            var appointment = await _context.Appointments
                .Where(a => a.AppointmentId == appointmentId && a.ClientId == client.UserId)
                .FirstOrDefaultAsync();

            if (appointment == null)
            {
                TempData["Error"] = "Appointment not found or you don't have permission to delete it.";
                return RedirectToAction("Index");
            }

            // Slett avtalen
            _context.Appointments.Remove(appointment);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Appointment deleted successfully!";
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Delete appointment error: {ex.Message}");
            TempData["Error"] = $"An error occurred while deleting the appointment: {ex.Message}";
            return RedirectToAction("Index");
        }
    }
}
