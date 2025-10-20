using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
namespace app_ointment_backend.Models;

public class Appointment
{
    public int AppointmentId { get; set; }
    public required DateTime Date { get; set; }
    public required int CaregiverId { get; set; }
    [ValidateNever]
    public virtual User Caregiver { get; set; } = default!;
    public required int ClientId { get; set; }
    [ValidateNever]
    public virtual User Client { get; set; } = default!;
    public required string Location { get; set; }
}
