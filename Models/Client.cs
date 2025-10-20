namespace app_ointment_backend.Models;

public class Client : User
{
    public int ClientId { get; set; }
    public virtual List<Appointment>? Appointments { get; set; } 
}