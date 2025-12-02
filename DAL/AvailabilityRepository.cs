using Microsoft.EntityFrameworkCore;
using app_ointment_backend.Models;

namespace app_ointment_backend.DAL;

//handle database operations for availabilities

public class AvailabilityRepository : IAvailabilityRepository
{
    private readonly UserDbContext _context;
    private readonly ILogger<AvailabilityRepository> _logger;

    public AvailabilityRepository(UserDbContext context, ILogger<AvailabilityRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Availability>?> GetAll()
    {
        try
        {
            return await _context.Availabilities
                .Include(a => a.Caregiver)
                .AsNoTracking()
                .ToListAsync();
        }
        catch (Exception e)
        {
            _logger.LogError("[AvailabilityRepository] availabilities query failed when GetAll(), error message: {e}", e.Message);
            return null;
        }
    }

    public async Task<IEnumerable<Availability>?> GetAvailabilityByCaregiver(int caregiverId)
    {
        try
        {
            return await _context.Availabilities
                .Where(a => a.CaregiverId == caregiverId)
                .AsNoTracking()
                .ToListAsync();
        }
        catch (Exception e)
        {
            _logger.LogError("[AvailabilityRepository] availabilities query failed when GetAvailabilityByCaregiver() for CaregiverId {CaregiverId:0000}, error message: {e}", caregiverId, e.Message);
            return null;
        }
    }

    public async Task<Availability?> GetAvailabilityById(int availabilityId)
    {
        try
        {
            return await _context.Availabilities
                .Include(a => a.Caregiver)
                .AsNoTracking()
                .FirstOrDefaultAsync(a => a.AvailabilityId == availabilityId);
        }
        catch (Exception e)
        {
            _logger.LogError("[AvailabilityRepository] availability query failed when GetAvailabilityById() for AvailabilityId {AvailabilityId:0000}, error message: {e}", availabilityId, e.Message);
            return null;
        }
    }

    public async Task<bool> AvailabilityExists(int caregiverId, DateTime date, string startTime, string endTime)
    {
        try
        {
            return await _context.Availabilities
                .AnyAsync(a => a.CaregiverId == caregiverId
                    && a.Date.Date == date.Date
                    && a.StartTime == startTime
                    && a.EndTime == endTime);
        }
        catch (Exception e)
        {
            _logger.LogError("[AvailabilityRepository] availability exists check failed for CaregiverId {CaregiverId:0000}, error message: {e}", caregiverId, e.Message);
            return false;
        }
    }

    public async Task<bool> AvailabilityConflictExists(int availabilityId, int caregiverId, DateTime date, string startTime, string endTime)
    {
        try
        {
            return await _context.Availabilities
                .AnyAsync(a => a.AvailabilityId != availabilityId
                    && a.CaregiverId == caregiverId
                    && a.Date.Date == date.Date
                    && a.StartTime == startTime
                    && a.EndTime == endTime);
        }
        catch (Exception e)
        {
            _logger.LogError("[AvailabilityRepository] availability conflict check failed for AvailabilityId {AvailabilityId:0000}, error message: {e}", availabilityId, e.Message);
            return false;
        }
    }

    public async Task<bool> CreateAvailability(Availability availability)
    {
        try
        {
            _context.Availabilities.Add(availability);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError("[AvailabilityRepository] availability creation failed for availability {@availability}, error message: {e}", availability, e.Message);
            return false;
        }
    }

    public async Task<bool> UpdateAvailability(Availability availability)
    {
        try
        {
            _context.Availabilities.Update(availability);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError("[AvailabilityRepository] availability update failed for AvailabilityId {AvailabilityId:0000}, error message: {e}", availability.AvailabilityId, e.Message);
            return false;
        }
    }

    public async Task<bool> DeleteAvailability(int availabilityId)
    {
        try
        {
            var availability = await _context.Availabilities.FindAsync(availabilityId);
            if (availability == null)
            {
                _logger.LogError("[AvailabilityRepository] availability not found for AvailabilityId {AvailabilityId:0000}", availabilityId);
                return false;
            }

            _context.Availabilities.Remove(availability);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError("[AvailabilityRepository] availability deletion failed for AvailabilityId {AvailabilityId:0000}, error message: {e}", availabilityId, e.Message);
            return false;
        }
    }
}