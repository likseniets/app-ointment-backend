using System;
using System.ComponentModel.DataAnnotations;
namespace app_ointment_backend.Models;

public class Appointment
{
    public int AppointmentId { get; set; }
    public required DateTime Date { get; set; }
    public required int CaregiverId { get; set; }
    public virtual Caregiver Caregiver { get; set; } = default!;
    public required int ClientId { get; set; }
    public virtual Client Client { get; set; } = default!;
    public required string Location { get; set; }
}
