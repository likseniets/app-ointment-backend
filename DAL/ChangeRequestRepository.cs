using Microsoft.EntityFrameworkCore;
using app_ointment_backend.Models;

namespace app_ointment_backend.DAL;

public class ChangeRequestRepository : IChangeRequestRepository
{
    private readonly UserDbContext _db;
    private readonly ILogger<ChangeRequestRepository> _logger;

    public ChangeRequestRepository(UserDbContext db, ILogger<ChangeRequestRepository> logger)
    {
        _db = db;
        _logger = logger;
    }

    public async Task<IEnumerable<AppointmentChangeRequest>?> GetPendingChangeRequestsForUser(int userId)
    {
        try
        {
            return await _db.AppointmentChangeRequests
                .Include(cr => cr.Appointment)
                .Include(cr => cr.RequestedByUser)
                .Where(cr => cr.Status == ChangeRequestStatus.Pending &&
                           (cr.Appointment!.ClientId == userId || cr.Appointment.CaregiverId == userId) &&
                           cr.RequestedByUserId != userId)
                .OrderByDescending(cr => cr.RequestedAt)
                .ToListAsync();
        }
        catch (Exception e)
        {
            _logger.LogError("[ChangeRequestRepository] Failed to get pending change requests for user {UserId}, error: {Error}", userId, e.Message);
            return null;
        }
    }

    public async Task<IEnumerable<AppointmentChangeRequest>?> GetChangeRequestsByUser(int userId)
    {
        try
        {
            return await _db.AppointmentChangeRequests
                .Include(cr => cr.Appointment)
                .Include(cr => cr.RequestedByUser)
                .Where(cr => cr.Status == ChangeRequestStatus.Pending &&
                           (cr.Appointment!.ClientId == userId || cr.Appointment.CaregiverId == userId) &&
                           cr.RequestedByUserId == userId)
                .OrderByDescending(cr => cr.RequestedAt)
                .ToListAsync();
        }
        catch (Exception e)
        {
            _logger.LogError("[ChangeRequestRepository] Failed to get pending change requests for user {UserId}, error: {Error}", userId, e.Message);
            return null;
        }
    }

    public async Task<IEnumerable<AppointmentChangeRequest>?> GetChangeRequestsByAppointment(int appointmentId)
    {
        try
        {
            return await _db.AppointmentChangeRequests
                .Include(cr => cr.RequestedByUser)
                .Include(cr => cr.RespondedByUser)
                .Where(cr => cr.AppointmentId == appointmentId)
                .OrderByDescending(cr => cr.RequestedAt)
                .ToListAsync();
        }
        catch (Exception e)
        {
            _logger.LogError("[ChangeRequestRepository] Failed to get change requests for appointment {AppointmentId}, error: {Error}", appointmentId, e.Message);
            return null;
        }
    }

    public async Task<AppointmentChangeRequest?> GetChangeRequestById(int changeRequestId)
    {
        try
        {
            return await _db.AppointmentChangeRequests
                .Include(cr => cr.Appointment)
                .Include(cr => cr.RequestedByUser)
                .Include(cr => cr.RespondedByUser)
                .FirstOrDefaultAsync(cr => cr.ChangeRequestId == changeRequestId);
        }
        catch (Exception e)
        {
            _logger.LogError("[ChangeRequestRepository] Failed to get change request {ChangeRequestId}, error: {Error}", changeRequestId, e.Message);
            return null;
        }
    }

    public async Task<bool> CreateChangeRequest(AppointmentChangeRequest changeRequest)
    {
        try
        {
            await _db.AppointmentChangeRequests.AddAsync(changeRequest);
            await _db.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError("[ChangeRequestRepository] Failed to create change request, error: {Error}", e.Message);
            return false;
        }
    }

    public async Task<bool> UpdateChangeRequest(AppointmentChangeRequest changeRequest)
    {
        try
        {
            _db.AppointmentChangeRequests.Update(changeRequest);
            await _db.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError("[ChangeRequestRepository] Failed to update change request {ChangeRequestId}, error: {Error}", changeRequest.ChangeRequestId, e.Message);
            return false;
        }
    }

    public async Task<bool> DeleteChangeRequest(int changeRequestId)
    {
        try
        {
            var changeRequest = await _db.AppointmentChangeRequests.FindAsync(changeRequestId);
            if (changeRequest == null)
            {
                return false;
            }
            _db.AppointmentChangeRequests.Remove(changeRequest);
            await _db.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError("[ChangeRequestRepository] Failed to delete change request {ChangeRequestId}, error: {Error}", changeRequestId, e.Message);
            return false;
        }
    }
}
