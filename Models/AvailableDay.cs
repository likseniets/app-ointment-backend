namespace app_ointment_backend.Models;

public class AvailableDay
{
    public int Id { get; set; }
    public int HealthcarePersonnelId { get; set; }
    public DateTime Date { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public bool IsBooked { get; set; } = false;
    public string? Notes { get; set; }
    
    // Navigation property
    public User HealthcarePersonnel { get; set; } = null!;
}

