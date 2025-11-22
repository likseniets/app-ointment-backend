namespace app_ointment_backend.Models;

public class AppointmentDto
{
    public int AppointmentId { get; set; }
    public DateTime Date { get; set; }
    public int CaregiverId { get; set; }
    public UserDto? Caregiver { get; set; }
    public int ClientId { get; set; }
    public UserDto? Client { get; set; }
    public string Location { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    public static AppointmentDto FromAppointment(Appointment appointment)
    {
        return new AppointmentDto
        {
            AppointmentId = appointment.AppointmentId,
            Date = appointment.Date,
            CaregiverId = appointment.CaregiverId,
            Caregiver = appointment.Caregiver != null ? UserDto.FromUser(appointment.Caregiver) : null,
            ClientId = appointment.ClientId,
            Client = appointment.Client != null ? UserDto.FromUser(appointment.Client) : null,
            Location = appointment.Location,
            Description = appointment.Description
        };
    }
}
