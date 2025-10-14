using System;
using System.ComponentModel.DataAnnotations;
namespace app_ointment_backend.Models;

public class Appointment
{
    public int AppointmentId { get; set; }
    public required DateTime Date { get; set; }
    public required int CaregiverId { get; set; }
    public required int ClientId { get; set; }
    public required string Location { get; set; }

}
