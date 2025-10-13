namespace app_ointment_backend.Models;

public class AppointmentTask
{
    public int Id { get; set; }
    public int AppointmentId { get; set; }
    public string TaskName { get; set; } = string.Empty;
    public TaskType TaskType { get; set; }
    public string? Description { get; set; }
    public bool IsCompleted { get; set; } = false;
    
    // Navigation property
    public Appointment Appointment { get; set; } = null!;
}

public enum TaskType
{
    AssistanceWithDailyLiving,
    MedicationReminder,
    Shopping,
    HouseholdChores,
    HealthCheck,
    Companionship,
    Other
}

