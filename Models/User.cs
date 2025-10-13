namespace app_ointment_backend.Models;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    // Navigation properties
    public ICollection<Appointment> Appointments { get; set; } = new List<Appointment>();
    public ICollection<AvailableDay> AvailableDays { get; set; } = new List<AvailableDay>();
}

public enum UserRole
{
    HealthcarePersonnel,
    Elderly
}
