using app_ointment_backend.Models;

namespace app_ointment_backend.ViewModels
{
    public class AppointmentsViewModel
    {
        public IEnumerable<Appointment> Appointments;
        public string? CurrentViewName;

        public AppointmentsViewModel(IEnumerable<Appointment> appointments, string? currentViewName)
        {
            Appointments = appointments;
            CurrentViewName = currentViewName;
        }
    }
}