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
    public required AppointmentTask Task { get; set; }


    // validate the date input for making appointments
    public static ValidationResult? ValidateFutureDate(DateTime value, ValidationContext _)
    {
        return value > DateTime.Now ? ValidationResult.Success : new ValidationResult("The appointment date must be in the future, time travel is not enabled yet.");
    }
}

public enum ChangeRequestStatus
{
    Pending,
    Approved,
    Rejected
}

public class AppointmentChangeRequest
{
    [Key]
    public int ChangeRequestId { get; set; }

    [Required]
    public int AppointmentId { get; set; }
    public virtual Appointment? Appointment { get; set; }

    [Required]
    public int RequestedByUserId { get; set; }
    public virtual User? RequestedByUser { get; set; }

    public AppointmentTask? OldTask { get; set; }
    public DateTime? OldDateTime { get; set; }

    public AppointmentTask? NewTask { get; set; }
    public DateTime? NewDateTime { get; set; }

    public ChangeRequestStatus Status { get; set; } = ChangeRequestStatus.Pending;

    public DateTime RequestedAt { get; set; } = DateTime.UtcNow;
    public DateTime? RespondedAt { get; set; }

    public int? RespondedByUserId { get; set; }
    public virtual User? RespondedByUser { get; set; }
}

