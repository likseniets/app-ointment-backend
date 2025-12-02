using app_ointment_backend.Models;

namespace app_ointment_backend.Services;

//interface to define what is in the appointment service

public interface IAppointmentService
{
    Task<IEnumerable<Appointment>?> GetAllAppointments();
    Task<IEnumerable<Appointment>?> GetClientAppointments(int clientId);
    Task<IEnumerable<Appointment>?> GetCaregiverAppointments(int caregiverId);
    Task<Appointment?> GetAppointmentById(int appointmentId);
    Task<(bool Success, string Message, Appointment? Appointment)> CreateAppointment(CreateAppointmentDto dto);
    Task<(bool Success, string Message)> UpdateAppointment(int appointmentId, UpdateAppointmentDto dto);
    Task<(bool Success, string Message)> DeleteAppointment(int appointmentId);
    Task<bool> IsAppointmentSlotAvailable(int caregiverId, DateTime dateTime);
}
