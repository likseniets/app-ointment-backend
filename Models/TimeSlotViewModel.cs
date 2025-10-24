using System;

namespace app_ointment_backend.Models
{
    /// <summary>
    /// NY FIL: TimeSlotViewModel - ViewModel for individuelle tidsrom
    /// Brukes for å dele opp tilgjengelighet i mindre, bookbare tidsrom
    /// </summary>
    public class TimeSlotViewModel
    {
        public DateTime Date { get; set; }
        public string StartTime { get; set; } = string.Empty;
        public string EndTime { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int AvailabilityId { get; set; }
    }
}

