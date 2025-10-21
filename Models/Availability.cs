using System.ComponentModel.DataAnnotations;

namespace app_ointment_backend.Models;

public class Availability
{
    public int AvailabilityId { get; set; }

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
    public int CaregiverId { get; set; }

    public string Description { get; set; } = string.Empty;

    public virtual Caregiver Caregiver { get; set; } = default!;
}