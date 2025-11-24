using System.ComponentModel.DataAnnotations;

namespace app_ointment_backend.Models;

public class AvailabilityDto
{
    public int AvailabilityId { get; set; }
    public DateTime Date { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public int CaregiverId { get; set; }
    public string CaregiverName { get; set; } = string.Empty;

    public static AvailabilityDto FromAvailability(Availability availability)
    {
        return new AvailabilityDto
        {
            AvailabilityId = availability.AvailabilityId,
            Date = availability.Date,
            StartTime = availability.StartTime,
            EndTime = availability.EndTime,
            CaregiverId = availability.CaregiverId,
            CaregiverName = availability.Caregiver.Name
        };
    }
}

public class CreateAvailabilityDto
{
    [Required]
    public int CaregiverId { get; set; }

    [Required]
    [DataType(DataType.Date)]
    public DateTime Date { get; set; }

    [Required]
    [RegularExpression(@"^([01]?[0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "Start time must be in format HH:mm")]
    public string StartTime { get; set; } = string.Empty;

    [Required]
    [RegularExpression(@"^([01]?[0-9]|2[0-3]):[0-5][0-9]$", ErrorMessage = "End time must be in format HH:mm")]
    public string EndTime { get; set; } = string.Empty;

    [Required]
    [Range(15, 480, ErrorMessage = "Slot length must be between 15 minutes and 8 hours (480 minutes)")]
    public int SlotLengthMinutes { get; set; } = 60; // Default to 60 minutes
}
