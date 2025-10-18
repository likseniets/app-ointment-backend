using app_ointment_backend.Models;

namespace app_ointment_backend.DAL;

public interface IAppointmentRepository
{
    Task<IEnumerable<Appointment>?> GetAll();
    Task<Appointment?> GetAppointmentById(int appointmentId);
    Task<bool> CreateAppointment(Appointment appointment);
    Task<bool> UpdateAppointment(Appointment appointment);
    Task<bool> DeleteAppointment(int appointmentId);
}