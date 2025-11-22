namespace app_ointment_backend.Models;

public class AvailabilityDto
{
    public int AvailabilityId { get; set; }
    public DateTime Date { get; set; }
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public int CaregiverId { get; set; }

    public static AvailabilityDto FromAvailability(Availability availability)
    {
        return new AvailabilityDto
        {
            AvailabilityId = availability.AvailabilityId,
            Date = availability.Date,
            StartTime = availability.StartTime,
            EndTime = availability.EndTime,
            CaregiverId = availability.CaregiverId
        };
    }
}
