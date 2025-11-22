using System.ComponentModel.DataAnnotations;

namespace app_ointment_backend.Models;

public class CreateAppointmentDto
{
    [Required]
    public int AvailabilityId { get; set; }

    [Required]
    public int ClientId { get; set; }

    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
}
