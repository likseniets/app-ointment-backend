using app_ointment_backend.Models;

namespace app_ointment_backend.DAL;

public interface IAvailabilityRepository
{
    Task<IEnumerable<Availability>?> GetAll();
    Task<IEnumerable<Availability>?> GetAvailabilityByCaregiver(int caregiverId);
    Task<Availability?> GetAvailabilityById(int availabilityId);
    Task<bool> CreateAvailability(Availability availability);
    Task<bool> UpdateAvailability(Availability availability);
    Task<bool> DeleteAvailability(int availabilityId);
}