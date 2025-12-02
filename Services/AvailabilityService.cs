using app_ointment_backend.DAL;
using app_ointment_backend.Models;

namespace app_ointment_backend.Services;

public class AvailabilityService : IAvailabilityService
{
    private readonly IAvailabilityRepository _availabilityRepository;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<AvailabilityService> _logger;

    public AvailabilityService(
        IAvailabilityRepository availabilityRepository,
        IUserRepository userRepository,
        ILogger<AvailabilityService> logger)
    {
        _availabilityRepository = availabilityRepository;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<Availability>?> GetAllAvailabilities()
    {
        return await _availabilityRepository.GetAll();
    }

    public async Task<IEnumerable<Availability>?> GetAvailabilitiesByCaregiver(int caregiverId)
    {
        return await _availabilityRepository.GetAvailabilityByCaregiver(caregiverId);
    }

    public async Task<Availability?> GetAvailabilityById(int availabilityId)
    {
        return await _availabilityRepository.GetAvailabilityById(availabilityId);
    }

    // Create a new availability slot
    public async Task<(bool Success, string Message)> CreateAvailability(CreateAvailabilityDto dto)
    {
        try
        {
            // Verify caregiver exists
            var caregiver = await _userRepository.GetUserById(dto.CaregiverId);
            if (caregiver == null || caregiver.Role != UserRole.Caregiver)
            {
                return (false, "Caregiver not found");
            }

            // Validate time format
            if (!TimeSpan.TryParse(dto.StartTime, out var startTs) ||
                !TimeSpan.TryParse(dto.EndTime, out var endTs))
            {
                return (false, "Invalid time format. Please use HH:mm");
            }

            if (startTs >= endTs)
            {
                return (false, "End time must be after start time");
            }

            // Check if availability already exists
            bool exists = await _availabilityRepository.AvailabilityExists(
                dto.CaregiverId,
                dto.Date,
                dto.StartTime,
                dto.EndTime);

            if (exists)
            {
                return (false, "This availability slot already exists");
            }

            var availability = new Availability
            {
                Date = dto.Date,
                StartTime = dto.StartTime,
                EndTime = dto.EndTime,
                CaregiverId = dto.CaregiverId
            };

            bool created = await _availabilityRepository.CreateAvailability(availability);
            if (created)
            {
                _logger.LogInformation("[AvailabilityService] Availability created for caregiver {CaregiverId}", dto.CaregiverId);
                return (true, "Availability created successfully");
            }

            return (false, "Failed to create availability");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AvailabilityService] Error creating availability");
            return (false, "An error occurred while creating the availability");
        }
    }

    // Create multiple availability slots based on a time range and slot length
    public async Task<(bool Success, string Message, int SlotsCreated)> CreateAvailabilitySlots(CreateAvailabilityDto dto)
    {
        try
        {
            // Verify caregiver exists
            var caregiver = await _userRepository.GetUserById(dto.CaregiverId);
            if (caregiver == null || caregiver.Role != UserRole.Caregiver)
            {
                return (false, "Caregiver not found", 0);
            }

            // Validate time format
            if (!TimeSpan.TryParse(dto.StartTime, out var startTs) ||
                !TimeSpan.TryParse(dto.EndTime, out var endTs))
            {
                return (false, "Invalid time format. Please use HH:mm", 0);
            }

            if (startTs >= endTs)
            {
                return (false, "End time must be after start time", 0);
            }

            // Create slots, dividing the time range by slot length and checking for existing slots
            int created = 0;
            var slotLength = TimeSpan.FromMinutes(dto.SlotLengthMinutes);
            
            for (var t = startTs; t + slotLength <= endTs; t += slotLength)
            {
                var slotStart = t.ToString(@"hh\:mm");
                var slotEnd = (t + slotLength).ToString(@"hh\:mm");
                
                bool exists = await _availabilityRepository.AvailabilityExists(
                    dto.CaregiverId,
                    dto.Date.Date,
                    slotStart,
                    slotEnd);
                
                if (exists) continue;
                
                var slot = new Availability
                {
                    CaregiverId = dto.CaregiverId,
                    Date = dto.Date.Date,
                    StartTime = slotStart,
                    EndTime = slotEnd
                };
                
                bool slotCreated = await _availabilityRepository.CreateAvailability(slot);
                if (slotCreated)
                {
                    created++;
                }
            }

            if (created > 0)
            {
                _logger.LogInformation("[AvailabilityService] Created {Count} availability slots for caregiver {CaregiverId}", created, dto.CaregiverId);
                return (true, $"Created {created} slot(s)", created);
            }

            return (false, "No new slots created (may already exist)", 0);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AvailabilityService] Error creating availability slots");
            return (false, "An error occurred while creating the availability slots", 0);
        }
    }

    // Update an existing availability slot
    public async Task<(bool Success, string Message)> UpdateAvailability(int availabilityId, UpdateAvailabilityDto dto)
    {
        try
        {
            var availability = await _availabilityRepository.GetAvailabilityById(availabilityId);
            if (availability == null)
            {
                return (false, "Availability not found");
            }

            // Validate time format
            if (!TimeSpan.TryParse(dto.StartTime, out var startTs) ||
                !TimeSpan.TryParse(dto.EndTime, out var endTs))
            {
                return (false, "Invalid time format. Please use HH:mm");
            }

            if (startTs >= endTs)
            {
                return (false, "End time must be after start time");
            }

            // Check for conflicts with other availabilities
            bool conflict = await _availabilityRepository.AvailabilityConflictExists(
                availabilityId,
                availability.CaregiverId,
                dto.Date,
                dto.StartTime,
                dto.EndTime);

            if (conflict)
            {
                return (false, "This availability slot conflicts with an existing one");
            }

            availability.Date = dto.Date;
            availability.StartTime = dto.StartTime;
            availability.EndTime = dto.EndTime;

            bool updated = await _availabilityRepository.UpdateAvailability(availability);
            if (updated)
            {
                _logger.LogInformation("[AvailabilityService] Availability {AvailabilityId} updated successfully", availabilityId);
                return (true, "Availability updated successfully");
            }

            return (false, "Failed to update availability");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AvailabilityService] Error updating availability {AvailabilityId}", availabilityId);
            return (false, "An error occurred while updating the availability");
        }
    }

    // Delete an availability slot
    public async Task<(bool Success, string Message)> DeleteAvailability(int availabilityId)
    {
        try
        {
            var availability = await _availabilityRepository.GetAvailabilityById(availabilityId);
            if (availability == null)
            {
                return (false, "Availability not found");
            }

            bool deleted = await _availabilityRepository.DeleteAvailability(availabilityId);
            if (deleted)
            {
                _logger.LogInformation("[AvailabilityService] Availability {AvailabilityId} deleted successfully", availabilityId);
                return (true, "Availability deleted successfully");
            }

            return (false, "Failed to delete availability");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AvailabilityService] Error deleting availability {AvailabilityId}", availabilityId);
            return (false, "An error occurred while deleting the availability");
        }
    }
}
