using Microsoft.EntityFrameworkCore;
using app_ointment_backend.Models;

namespace app_ointment_backend.DAL;

public class AppointmentRepository : IAppointmentRepository
{
    private readonly UserDbContext _context;

    private readonly ILogger<AppointmentRepository> _logger;

    public AppointmentRepository(UserDbContext context, ILogger<AppointmentRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Appointment>?> GetAll()
    {
        try
        {
            return await _context.Appointments
                .Include(a => a.Client)
                .Include(a => a.Caregiver)
                .AsNoTracking()
                .ToListAsync();
        }
        catch (Exception e)
        {
            _logger.LogError("[AppointmentRepository] appointments query failed when GetAll(), error message: {e}", e.Message);
            return null;
        }

    }
    
    public async Task<IEnumerable<Appointment>?> GetClientAppointment(int id)
    {
        try
        {
            return await _context.Appointments
                .Include(a => a.Client)
                .Include(a => a.Caregiver)
                .Where(a => a.ClientId == id)
                .AsNoTracking()
                .ToListAsync();
        }
        catch (Exception e)
        {
            _logger.LogError("[AppointmentRepository] appointments query failed when GetAll(), error message: {e}", e.Message);
            return null;
        }
        
    }

    public async Task<Appointment?> GetAppointmentById(int id)
    {
        try
        {
            return await _context.Appointments
                .Include(a => a.Client)
                .Include(a => a.Caregiver)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.AppointmentId == id);
        }
        catch (Exception e)
        {
            _logger.LogError("[AppointmentRepository] appointments query failed when GetAppointmentById() for AppointmentId {AppointmentId:0000}, error message: {e}", id, e.Message);
            return null;
        }
        
    }

    public async Task<bool> CreateAppointment(Appointment appointment)
    {
        try
        {
            _context.Appointments.Add(appointment);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError("[AppointmentRepository] appointment creation failed for appointment {@appointment}, error message: {e}", appointment, e.Message);
            return false;
        }
        
    }

    public async Task<bool> UpdateAppointment(Appointment appointment)
    {
        try
        {
            _context.Appointments.Update(appointment);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError("[AppointmentRepository] appointment FindAsync(id) failed when updating the AppointmentId {AppointmentId:0000}, error message: {e}", appointment, e.Message);
            return false;
        }

    }

    public async Task<bool> DeleteAppointment(int id)
    {
        try
        {
            var appointment = await _context.Appointments.FindAsync(id);
            if (appointment == null)
            {
                _logger.LogError("[AppointmentRepository] appointment not found for AppointmentId {AppointmentId:0000}", id);
                return false;
            }

            _context.Appointments.Remove(appointment);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError("[AppointmentRepository] appointment deletion failed for AppointmentId {AppointmentId:0000}, error message: {e}", id, e.Message);
                return false;
        }
        
    }
}
