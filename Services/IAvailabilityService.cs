using app_ointment_backend.Models;

namespace app_ointment_backend.Services;

//interface to define what is in the availability service

public interface IAvailabilityService
{
    Task<IEnumerable<Availability>?> GetAllAvailabilities();
    Task<IEnumerable<Availability>?> GetAvailabilitiesByCaregiver(int caregiverId);
    Task<Availability?> GetAvailabilityById(int availabilityId);
    Task<(bool Success, string Message)> CreateAvailability(CreateAvailabilityDto dto);
    Task<(bool Success, string Message, int SlotsCreated)> CreateAvailabilitySlots(CreateAvailabilityDto dto);
    Task<(bool Success, string Message)> UpdateAvailability(int availabilityId, UpdateAvailabilityDto dto);
    Task<(bool Success, string Message)> DeleteAvailability(int availabilityId);
}
