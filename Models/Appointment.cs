using System;
using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
namespace app_ointment_backend.Models;

public class Appointment
{
    public int AppointmentId { get; set; }

    [CustomValidation(typeof(Appointment), nameof(ValidateFutureDate))]
    public required DateTime Date { get; set; } //Date and time must somehow be validated
    public required int CaregiverId { get; set; }
    [ValidateNever]
    public virtual User Caregiver { get; set; } = default!;
    public required int ClientId { get; set; }
    [ValidateNever]
    public virtual User Client { get; set; } = default!;
    public required string Location { get; set; }

    public static ValidationResult? ValidateFutureDate(DateTime value, ValidationContext _) 
    {
        return value > DateTime.Now ? ValidationResult.Success : new ValidationResult("The appointment date must be in the future, time travel is not enabled yet."); 
    }
}

