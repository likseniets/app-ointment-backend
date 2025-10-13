using System;
namespace app_ointment_backend.Models;

public class Appointment
{
    public int AppointmentId { get; set; }
    public required DateTime Date { get; set; }
    public required User Caretaker { get; set; }
    public required User Client { get; set; }
    public required string Location { get; set; }

}
