using app_ointment_backend.Models;

namespace app_ointment_backend.DAL;

//interface to define what is in the repository

public interface IAppointmentRepository
{
    Task<IEnumerable<Appointment>?> GetAll();
    Task<Appointment?> GetAppointmentById(int appointmentId);
    Task<bool> CreateAppointment(Appointment appointment);
    Task<bool> UpdateAppointment(Appointment appointment);
    Task<bool> DeleteAppointment(int appointmentId);
    Task<IEnumerable<Appointment>?> GetClientAppointment(int id);
    Task<IEnumerable<Appointment>?> GetCaregiverAppointments(int caregiverId);
}