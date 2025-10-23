using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace app_ointment_backend.ViewModels;

public class CreateAppointmentViewModel
{
    public int? SelectedCaregiverId { get; set; }
    public IEnumerable<SelectListItem> Caregivers { get; set; } = new List<SelectListItem>();

    public int? SelectedClientId { get; set; }
    public IEnumerable<SelectListItem> Clients { get; set; } = new List<SelectListItem>();

    // Date part (local) used to compute available time slots
    public DateTime? SelectedDate { get; set; }

    // Time in HH:mm (24h) format
    public string? SelectedTime { get; set; }
    public IEnumerable<SelectListItem> TimeSlots { get; set; } = new List<SelectListItem>();

    public string Location { get; set; } = string.Empty;

    public bool DateHasAvailability { get; set; }
}

