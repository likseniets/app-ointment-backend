using app_ointment_backend.Models;

namespace app_ointment_backend.Services;

//interface to define what is in the change request service

public interface IChangeRequestService
{
    Task<IEnumerable<AppointmentChangeRequest>?> GetPendingChangeRequestsForUser(int userId);
    Task<IEnumerable<AppointmentChangeRequest>?> GetChangeRequestsByUser(int userId);
    Task<(IEnumerable<AppointmentChangeRequest>? Requests, bool Success, string Message)> GetChangeRequestsByAppointment(int appointmentId, int userId, UserRole userRole);
    Task<(bool Success, string Message, AppointmentChangeRequest? Request)> CreateChangeRequest(CreateChangeRequestDto dto, int requestedByUserId);
    Task<(bool Success, string Message)> ApproveChangeRequest(int changeRequestId, int approvingUserId, UserRole userRole);
    Task<(bool Success, string Message)> RejectChangeRequest(int changeRequestId, int rejectingUserId, UserRole userRole, string? rejectionReason);
    Task<(bool Success, string Message)> CancelChangeRequest(int changeRequestId, int userId);
}
