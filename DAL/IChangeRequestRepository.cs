using app_ointment_backend.Models;

namespace app_ointment_backend.DAL;

public interface IChangeRequestRepository
{
    Task<IEnumerable<AppointmentChangeRequest>?> GetPendingChangeRequestsForUser(int userId);
    Task<IEnumerable<AppointmentChangeRequest>?> GetChangeRequestsByUser(int userId);
    Task<IEnumerable<AppointmentChangeRequest>?> GetChangeRequestsByAppointment(int appointmentId);
    Task<AppointmentChangeRequest?> GetChangeRequestById(int changeRequestId);
    Task<bool> CreateChangeRequest(AppointmentChangeRequest changeRequest);
    Task<bool> UpdateChangeRequest(AppointmentChangeRequest changeRequest);
    Task<bool> DeleteChangeRequest(int changeRequestId);
}
