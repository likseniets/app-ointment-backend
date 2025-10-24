using app_ointment_backend.Models;

namespace app_ointment_backend.Models
{
    /// <summary>
    /// NY FIL: ClientDashboardViewModel - ViewModel for klient-dashboard
    /// Kombinerer tilgjengelige omsorgspersoner med klientens egne avtaler
    /// </summary>
    public class ClientDashboardViewModel
    {
        public List<Caregiver> Caregivers { get; set; } = new List<Caregiver>();
        public List<Appointment> Appointments { get; set; } = new List<Appointment>();
    }
}

