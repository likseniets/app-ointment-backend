namespace app_ointment_backend.Models;

public class Appointment
{
    public int Id { get; set; }
    public int ElderlyUserId { get; set; }
    public int HealthcarePersonnelId { get; set; }
    public DateTime AppointmentDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public AppointmentStatus Status { get; set; } = AppointmentStatus.Scheduled;
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.Now;
    
    // Navigation properties
    public User ElderlyUser { get; set; } = null!;
    public User HealthcarePersonnel { get; set; } = null!;
    public ICollection<AppointmentTask> Tasks { get; set; } = new List<AppointmentTask>();
}

public enum AppointmentStatus
{
    Scheduled,
    InProgress,
    Completed,
    Cancelled
}
