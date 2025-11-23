using System.ComponentModel.DataAnnotations;

namespace app_ointment_backend.Models;



public class AppointmentDto
{
    public int AppointmentId { get; set; }
    public DateTime Date { get; set; }
    public int CaregiverId { get; set; }
    public UserDto? Caregiver { get; set; }
    public int ClientId { get; set; }
    public UserDto? Client { get; set; }
    public string ClientAddress { get; set; } = string.Empty;
    public string Task { get; set; } = string.Empty;

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
            ClientAddress = appointment.Client?.Adress ?? "Unknown",
            Task = appointment.Task.ToDisplayString()
        };
    }
}

public class CreateAppointmentDto
{
    [Required]
    public int AvailabilityId { get; set; }

    [Required]
    public int ClientId { get; set; }

    [Required]
    public AppointmentTask Task { get; set; }
}

public enum AppointmentTask
{
    AssistanceWithDailyLiving,
    MedicationReminders,
    Shopping,
    HouseholdChores,
    PersonalHygiene,
    MealPreparation,
    Transportation,
    Companionship,
    PhysicalTherapyAssistance,
    MedicalAppointmentSupport
}

public static class AppointmentTaskExtensions
{
    public static string ToDisplayString(this AppointmentTask task)
    {
        return task switch
        {
            AppointmentTask.AssistanceWithDailyLiving => "Assistance with Daily Living",
            AppointmentTask.MedicationReminders => "Medication Reminders",
            AppointmentTask.Shopping => "Shopping",
            AppointmentTask.HouseholdChores => "Household Chores",
            AppointmentTask.PersonalHygiene => "Personal Hygiene",
            AppointmentTask.MealPreparation => "Meal Preparation",
            AppointmentTask.Transportation => "Transportation",
            AppointmentTask.Companionship => "Companionship",
            AppointmentTask.PhysicalTherapyAssistance => "Physical Therapy Assistance",
            AppointmentTask.MedicalAppointmentSupport => "Medical Appointment Support",
            _ => task.ToString()
        };
    }
}

public class AppointmentChangeRequestDto
{
    public int ChangeRequestId { get; set; }
    public int AppointmentId { get; set; }
    public int RequestedByUserId { get; set; }
    public string RequestedByName { get; set; } = string.Empty;
    public string? OldTask { get; set; }
    public DateTime? OldDateTime { get; set; }
    public string? NewTask { get; set; }
    public DateTime? NewDateTime { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime RequestedAt { get; set; }
    public DateTime? RespondedAt { get; set; }
    public int? RespondedByUserId { get; set; }
    public string? RespondedByName { get; set; }

    public static AppointmentChangeRequestDto FromChangeRequest(AppointmentChangeRequest request)
    {
        return new AppointmentChangeRequestDto
        {
            ChangeRequestId = request.ChangeRequestId,
            AppointmentId = request.AppointmentId,
            RequestedByUserId = request.RequestedByUserId,
            RequestedByName = request.RequestedByUser?.Name ?? "Unknown",
            OldTask = request.OldTask?.ToDisplayString(),
            OldDateTime = request.OldDateTime,
            NewTask = request.NewTask?.ToDisplayString(),
            NewDateTime = request.NewDateTime,
            Status = request.Status.ToString(),
            RequestedAt = request.RequestedAt,
            RespondedAt = request.RespondedAt,
            RespondedByUserId = request.RespondedByUserId,
            RespondedByName = request.RespondedByUser?.Name
        };
    }
}

public class CreateChangeRequestDto
{
    public int AppointmentId { get; set; }
    public AppointmentTask? NewTask { get; set; }
    public int? NewAvailabilityId { get; set; }
}

