namespace app_ointment_backend.Models;

public class Client : User
{
    public int ClientId { get; set; }
    
/*  public string Name { get; set; } = string.Empty;
    public string Adress { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? Email { get; set; }

*/
    public virtual List<Appointment>? Appointments { get; set; } 
}