using app_ointment_backend.Models;

namespace app_ointment_backend.DAL;

public interface IAvailabilityRepository
{
    Task<IEnumerable<Availability>?> GetAll();
    Task<IEnumerable<Availability>?> GetAvailabilityByCaregiver(int caregiverId);
    Task<Availability?> GetAvailabilityById(int availabilityId);
    Task<bool> AvailabilityExists(int caregiverId, DateTime date, string startTime, string endTime);
    Task<bool> AvailabilityConflictExists(int availabilityId, int caregiverId, DateTime date, string startTime, string endTime);
    Task<bool> CreateAvailability(Availability availability);
    Task<bool> UpdateAvailability(Availability availability);
    Task<bool> DeleteAvailability(int availabilityId);
}