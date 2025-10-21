using System.ComponentModel.DataAnnotations;

namespace app_ointment_backend.Models;

public class Caregiver : User
{
    public int CaregiverId { get; set; }
    public virtual List<Appointment>? Appointments { get; set; }
    public virtual List<Availability>? Availability { get; set; }
}