using app_ointment_backend.DAL;
using app_ointment_backend.Models;
using Microsoft.EntityFrameworkCore;

namespace app_ointment_backend.Services;

public class AppointmentService : IAppointmentService
{
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IAvailabilityRepository _availabilityRepository;
    private readonly IUserRepository _userRepository;
    private readonly UserDbContext _context;
    private readonly ILogger<AppointmentService> _logger;

    public AppointmentService(
        IAppointmentRepository appointmentRepository,
        IAvailabilityRepository availabilityRepository,
        IUserRepository userRepository,
        UserDbContext context,
        ILogger<AppointmentService> logger)
    {
        _appointmentRepository = appointmentRepository;
        _availabilityRepository = availabilityRepository;
        _userRepository = userRepository;
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<Appointment>?> GetAllAppointments()
    {
        return await _appointmentRepository.GetAll();
    }

    public async Task<IEnumerable<Appointment>?> GetClientAppointments(int clientId)
    {
        return await _appointmentRepository.GetClientAppointment(clientId);
    }

    public async Task<IEnumerable<Appointment>?> GetCaregiverAppointments(int caregiverId)
    {
        return await _appointmentRepository.GetCaregiverAppointments(caregiverId);
    }

    public async Task<Appointment?> GetAppointmentById(int appointmentId)
    {
        return await _appointmentRepository.GetAppointmentById(appointmentId);
    }

    // Create a new appointment based on an availability slot
    public async Task<(bool Success, string Message, Appointment? Appointment)> CreateAppointment(CreateAppointmentDto dto)
    {
        try
        {
            // Get the availability slot
            var availability = await _availabilityRepository.GetAvailabilityById(dto.AvailabilityId);
            if (availability == null)
            {
                return (false, "Selected availability slot not found", null);
            }

            // Verify client exists
            var client = await _context.Users.FirstOrDefaultAsync(u => u.UserId == dto.ClientId && u.Role == UserRole.Client);
            if (client == null)
            {
                return (false, "Client not found", null);
            }

            // Verify caregiver exists
            var caregiver = await _context.Users.FirstOrDefaultAsync(u => u.UserId == availability.CaregiverId && u.Role == UserRole.Caregiver);
            if (caregiver == null)
            {
                return (false, "Caregiver not found", null);
            }

            // Parse the availability time to create the appointment datetime
            if (!TimeSpan.TryParse(availability.StartTime, out var startTime))
            {
                return (false, "Invalid availability time format", null);
            }

            var appointmentDate = availability.Date.Date + startTime;

            // Check if appointment date is in the future
            if (appointmentDate <= DateTime.Now)
            {
                return (false, "Appointment date must be in the future", null);
            }

            // Check if slot is already booked
            bool alreadyBooked = await _context.Appointments.AnyAsync(a =>
                a.CaregiverId == availability.CaregiverId &&
                a.Date == appointmentDate);
            if (alreadyBooked)
            {
                return (false, "Selected time slot is already booked", null);
            }

            // Create the appointment
            var appointment = new Appointment
            {
                Date = appointmentDate,
                CaregiverId = availability.CaregiverId,
                ClientId = dto.ClientId,
                Task = dto.Task
            };

            bool created = await _appointmentRepository.CreateAppointment(appointment);
            if (created)
            {
                // Remove the availability slot
                await _availabilityRepository.DeleteAvailability(dto.AvailabilityId);
                _logger.LogInformation("[AppointmentService] Appointment created successfully for client {ClientId} with caregiver {CaregiverId}", dto.ClientId, availability.CaregiverId);
                return (true, "Appointment created successfully", appointment);
            }

            return (false, "Failed to create appointment", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AppointmentService] Error creating appointment");
            return (false, "An error occurred while creating the appointment", null);
        }
    }

    // Update an existing appointment
    public async Task<(bool Success, string Message)> UpdateAppointment(int appointmentId, UpdateAppointmentDto dto)
    {
        try
        {
            var appointment = await _appointmentRepository.GetAppointmentById(appointmentId);
            if (appointment == null)
            {
                return (false, "Appointment not found");
            }

            // Validate new date is in the future
            if (dto.Date <= DateTime.Now)
            {
                return (false, "Appointment date must be in the future");
            }

            // Check if new time slot is available (excluding current appointment)
            bool slotTaken = await _context.Appointments.AnyAsync(a =>
                a.AppointmentId != appointmentId &&
                a.CaregiverId == appointment.CaregiverId &&
                a.Date == dto.Date);

            if (slotTaken)
            {
                return (false, "Selected time slot is already booked");
            }

            appointment.Date = dto.Date;
            appointment.Task = dto.Task;

            bool updated = await _appointmentRepository.UpdateAppointment(appointment);
            if (updated)
            {
                _logger.LogInformation("[AppointmentService] Appointment {AppointmentId} updated successfully", appointmentId);
                return (true, "Appointment updated successfully");
            }

            return (false, "Failed to update appointment");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AppointmentService] Error updating appointment {AppointmentId}", appointmentId);
            return (false, "An error occurred while updating the appointment");
        }
    }

    // Delete an existing appointment
    public async Task<(bool Success, string Message)> DeleteAppointment(int appointmentId)
    {
        try
        {
            var appointment = await _appointmentRepository.GetAppointmentById(appointmentId);
            if (appointment == null)
            {
                return (false, "Appointment not found");
            }

            bool deleted = await _appointmentRepository.DeleteAppointment(appointmentId);
            if (deleted)
            {
                _logger.LogInformation("[AppointmentService] Appointment {AppointmentId} deleted successfully", appointmentId);
                return (true, "Appointment deleted successfully");
            }

            return (false, "Failed to delete appointment");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AppointmentService] Error deleting appointment {AppointmentId}", appointmentId);
            return (false, "An error occurred while deleting the appointment");
        }
    }

    // Check if an appointment slot is available for a caregiver at a given date and time
    public async Task<bool> IsAppointmentSlotAvailable(int caregiverId, DateTime dateTime)
    {
        try
        {
            return !await _context.Appointments.AnyAsync(a =>
                a.CaregiverId == caregiverId &&
                a.Date == dateTime);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[AppointmentService] Error checking slot availability");
            return false;
        }
    }
}
