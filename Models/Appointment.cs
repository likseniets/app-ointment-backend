namespace app_ointment_backend.Models;

public class Appointment
{
    public int AppointmentId { get; set; }
    public string AppointmentDate { get; set; } = string.Empty;
    public int ClientId { get; set; }
    public int CaregiverId { get; set; }
    public virtual Caregiver Caregiver { get; set; } = default!;
    public virtual Client Client { set; get; } = default!;
    
    //public virtual List<Appointment>? Appointments { get; set; }
}