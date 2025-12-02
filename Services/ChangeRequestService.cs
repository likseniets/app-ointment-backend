using app_ointment_backend.DAL;
using app_ointment_backend.Models;

namespace app_ointment_backend.Services;

public class ChangeRequestService : IChangeRequestService
{
    private readonly IChangeRequestRepository _changeRequestRepository;
    private readonly IAppointmentRepository _appointmentRepository;
    private readonly IAvailabilityRepository _availabilityRepository;
    private readonly ILogger<ChangeRequestService> _logger;

    public ChangeRequestService(
        IChangeRequestRepository changeRequestRepository,
        IAppointmentRepository appointmentRepository,
        IAvailabilityRepository availabilityRepository,
        ILogger<ChangeRequestService> logger)
    {
        _changeRequestRepository = changeRequestRepository;
        _appointmentRepository = appointmentRepository;
        _availabilityRepository = availabilityRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<AppointmentChangeRequest>?> GetPendingChangeRequestsForUser(int userId)
    {
        return await _changeRequestRepository.GetPendingChangeRequestsForUser(userId);
    }

    public async Task<IEnumerable<AppointmentChangeRequest>?> GetChangeRequestsByUser(int userId)
    {
        return await _changeRequestRepository.GetChangeRequestsByUser(userId);
    }

    // Get change requests for a specific appointment, with access control
    public async Task<(IEnumerable<AppointmentChangeRequest>? Requests, bool Success, string Message)> GetChangeRequestsByAppointment(int appointmentId, int userId, UserRole userRole)
    {
        try
        {
            var appointment = await _appointmentRepository.GetAppointmentById(appointmentId);
            if (appointment == null)
            {
                return (null, false, "Appointment not found");
            }

            // Only allow the client, caregiver, or admin to view change requests
            if (userRole != UserRole.Admin &&
                appointment.ClientId != userId &&
                appointment.CaregiverId != userId)
            {
                return (null, false, "Access denied");
            }

            var requests = await _changeRequestRepository.GetChangeRequestsByAppointment(appointmentId);
            return (requests ?? Enumerable.Empty<AppointmentChangeRequest>(), true, "Success");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ChangeRequestService] Error getting change requests for appointment {AppointmentId}", appointmentId);
            return (null, false, "An error occurred while retrieving change requests");
        }
    }

    public async Task<(bool Success, string Message, AppointmentChangeRequest? Request)> CreateChangeRequest(CreateChangeRequestDto dto, int requestedByUserId)
    {
        try
        {
            var appointment = await _appointmentRepository.GetAppointmentById(dto.AppointmentId);
            if (appointment == null)
            {
                return (false, "Appointment not found", null);
            }

            // Only the client or caregiver involved can request changes
            if (appointment.ClientId != requestedByUserId && appointment.CaregiverId != requestedByUserId)
            {
                _logger.LogWarning("[ChangeRequestService] User {UserId} attempted to request change for appointment {AppointmentId}", requestedByUserId, dto.AppointmentId);
                return (false, "Access denied", null);
            }

            // Check if there's already a pending change request for this appointment
            var existingRequests = await _changeRequestRepository.GetChangeRequestsByAppointment(dto.AppointmentId);
            if (existingRequests != null && existingRequests.Any(r => r.Status == ChangeRequestStatus.Pending))
            {
                return (false, "There is already a pending change request for this appointment", null);
            }

            DateTime? newDateTime = null;
            // If requesting a datetime change, get the datetime from the availability slot
            if (dto.NewAvailabilityId.HasValue)
            {
                _logger.LogInformation("[ChangeRequestService] User {UserId} is requesting a datetime change using AvailabilityId {AvailabilityId}", requestedByUserId, dto.NewAvailabilityId);
                
                var availableSlots = await _availabilityRepository.GetAvailabilityByCaregiver(appointment.CaregiverId);
                var matchingSlot = availableSlots?.FirstOrDefault(a => a.AvailabilityId == dto.NewAvailabilityId.Value);

                if (matchingSlot == null)
                {
                    return (false, "The requested availability slot does not exist or does not belong to this caregiver", null);
                }

                // Parse the time from the availability slot
                var timeParts = matchingSlot.StartTime.Split(':');
                if (timeParts.Length >= 2 && int.TryParse(timeParts[0], out int hour))
                {
                    newDateTime = matchingSlot.Date.Date.AddHours(hour);
                }
                else
                {
                    return (false, "Invalid time format in availability slot", null);
                }
            }

            // Create the change request
            var changeRequest = new AppointmentChangeRequest
            {
                AppointmentId = dto.AppointmentId,
                RequestedByUserId = requestedByUserId,
                OldTask = appointment.Task,
                OldDateTime = appointment.Date,
                NewTask = dto.NewTask,
                NewDateTime = newDateTime,
                Status = ChangeRequestStatus.Pending,
                RequestedAt = DateTime.UtcNow
            };

            bool success = await _changeRequestRepository.CreateChangeRequest(changeRequest);
            if (!success)
            {
                return (false, "Failed to create change request", null);
            }

            _logger.LogInformation("[ChangeRequestService] User {UserId} created change request for appointment {AppointmentId}", requestedByUserId, dto.AppointmentId);

            // Reload to get navigation properties
            var createdRequest = await _changeRequestRepository.GetChangeRequestById(changeRequest.ChangeRequestId);
            return (true, "Change request created successfully", createdRequest);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ChangeRequestService] Error creating change request");
            return (false, "An error occurred while creating the change request", null);
        }
    }

    public async Task<(bool Success, string Message)> ApproveChangeRequest(int changeRequestId, int approvingUserId, UserRole userRole)
    {
        try
        {
            var changeRequest = await _changeRequestRepository.GetChangeRequestById(changeRequestId);
            if (changeRequest == null)
            {
                return (false, "Change request not found");
            }

            if (changeRequest.Status != ChangeRequestStatus.Pending)
            {
                return (false, "This change request has already been processed");
            }

            var appointment = changeRequest.Appointment;
            if (appointment == null)
            {
                return (false, "Appointment not found");
            }

            // Only the other party (not the requester) can approve
            if (changeRequest.RequestedByUserId == approvingUserId)
            {
                return (false, "You cannot approve your own change request");
            }

            // Verify the user is the client or caregiver involved
            if (userRole != UserRole.Admin &&
                appointment.ClientId != approvingUserId &&
                appointment.CaregiverId != approvingUserId)
            {
                _logger.LogWarning("[ChangeRequestService] User {UserId} attempted to approve change request {ChangeRequestId}", approvingUserId, changeRequestId);
                return (false, "Access denied");
            }

            // Store the old date/time from the change request
            var oldDateTime = changeRequest.OldDateTime ?? appointment.Date;
            var hasDateTimeChange = changeRequest.NewDateTime.HasValue && changeRequest.NewDateTime.Value != oldDateTime;

            // If datetime is changing, we need to handle availability slots
            if (hasDateTimeChange)
            {
                // Check if the new timeslot is available
                var newDate = changeRequest.NewDateTime!.Value;
                var newStartTime = new TimeSpan(newDate.Hour, 0, 0).ToString(@"hh\:mm");
                var newEndTime = new TimeSpan(newDate.Hour + 1, 0, 0).ToString(@"hh\:mm");

                // Check if there's an available slot for the new time
                var availableSlot = await _availabilityRepository.GetAvailabilityByCaregiver(appointment.CaregiverId);
                var matchingSlot = availableSlot?.FirstOrDefault(a =>
                    a.Date.Date == newDate.Date &&
                    a.StartTime == newStartTime &&
                    a.EndTime == newEndTime);

                if (matchingSlot == null)
                {
                    return (false, "The requested time slot is not available");
                }

                // Apply the changes to the appointment
                if (changeRequest.NewTask.HasValue)
                {
                    appointment.Task = changeRequest.NewTask.Value;
                }
                appointment.Date = changeRequest.NewDateTime.Value;

                bool appointmentUpdated = await _appointmentRepository.UpdateAppointment(appointment);
                if (!appointmentUpdated)
                {
                    return (false, "Failed to update appointment");
                }

                // Delete the new availability slot (it's now booked)
                await _availabilityRepository.DeleteAvailability(matchingSlot.AvailabilityId);

                // Restore the old availability slot
                var restoredSlot = new Availability
                {
                    CaregiverId = appointment.CaregiverId,
                    Date = oldDateTime.Date,
                    StartTime = new TimeSpan(oldDateTime.Hour, 0, 0).ToString(@"hh\:mm"),
                    EndTime = new TimeSpan(oldDateTime.Hour + 1, 0, 0).ToString(@"hh\:mm")
                };
                await _availabilityRepository.CreateAvailability(restoredSlot);
            }
            else
            {
                // Only task is changing, no availability slot changes needed
                if (changeRequest.NewTask.HasValue)
                {
                    appointment.Task = changeRequest.NewTask.Value;
                }

                bool appointmentUpdated = await _appointmentRepository.UpdateAppointment(appointment);
                if (!appointmentUpdated)
                {
                    return (false, "Failed to update appointment");
                }
            }

            // Update the change request status
            changeRequest.Status = ChangeRequestStatus.Approved;
            changeRequest.RespondedAt = DateTime.UtcNow;
            changeRequest.RespondedByUserId = approvingUserId;

            bool requestUpdated = await _changeRequestRepository.UpdateChangeRequest(changeRequest);
            if (!requestUpdated)
            {
                _logger.LogWarning("[ChangeRequestService] Failed to update change request {ChangeRequestId} status", changeRequestId);
            }

            _logger.LogInformation("[ChangeRequestService] User {UserId} approved change request {ChangeRequestId}", approvingUserId, changeRequestId);

            return (true, "Change request approved and appointment updated");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ChangeRequestService] Error approving change request {ChangeRequestId}", changeRequestId);
            return (false, "An error occurred while approving the change request");
        }
    }

    public async Task<(bool Success, string Message)> RejectChangeRequest(int changeRequestId, int rejectingUserId, UserRole userRole, string? rejectionReason)
    {
        try
        {
            var changeRequest = await _changeRequestRepository.GetChangeRequestById(changeRequestId);
            if (changeRequest == null)
            {
                return (false, "Change request not found");
            }

            if (changeRequest.Status != ChangeRequestStatus.Pending)
            {
                return (false, "This change request has already been processed");
            }

            var appointment = await _appointmentRepository.GetAppointmentById(changeRequest.AppointmentId);
            if (appointment == null)
            {
                return (false, "Appointment not found");
            }

            // Only the other party (not the requester) can reject
            if (changeRequest.RequestedByUserId == rejectingUserId)
            {
                return (false, "You cannot reject your own change request. Use the cancel endpoint instead.");
            }

            // Verify the user is the client or caregiver involved
            if (userRole != UserRole.Admin &&
                appointment.ClientId != rejectingUserId &&
                appointment.CaregiverId != rejectingUserId)
            {
                _logger.LogWarning("[ChangeRequestService] User {UserId} attempted to reject change request {ChangeRequestId}", rejectingUserId, changeRequestId);
                return (false, "Access denied");
            }

            // Update the change request status
            changeRequest.Status = ChangeRequestStatus.Rejected;
            changeRequest.RespondedAt = DateTime.UtcNow;
            changeRequest.RespondedByUserId = rejectingUserId;

            bool success = await _changeRequestRepository.UpdateChangeRequest(changeRequest);
            if (!success)
            {
                return (false, "Failed to reject change request");
            }

            _logger.LogInformation("[ChangeRequestService] User {UserId} rejected change request {ChangeRequestId}", rejectingUserId, changeRequestId);

            return (true, "Change request rejected");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ChangeRequestService] Error rejecting change request {ChangeRequestId}", changeRequestId);
            return (false, "An error occurred while rejecting the change request");
        }
    }

    public async Task<(bool Success, string Message)> CancelChangeRequest(int changeRequestId, int userId)
    {
        try
        {
            var changeRequest = await _changeRequestRepository.GetChangeRequestById(changeRequestId);
            if (changeRequest == null)
            {
                return (false, "Change request not found");
            }

            // Only the requester can cancel their own request
            if (changeRequest.RequestedByUserId != userId)
            {
                _logger.LogWarning("[ChangeRequestService] User {UserId} attempted to cancel change request {ChangeRequestId} by another user", userId, changeRequestId);
                return (false, "Access denied");
            }

            if (changeRequest.Status != ChangeRequestStatus.Pending)
            {
                return (false, "Only pending change requests can be cancelled");
            }

            bool success = await _changeRequestRepository.DeleteChangeRequest(changeRequestId);
            if (!success)
            {
                return (false, "Failed to cancel change request");
            }

            _logger.LogInformation("[ChangeRequestService] User {UserId} cancelled change request {ChangeRequestId}", userId, changeRequestId);

            return (true, "Change request cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[ChangeRequestService] Error cancelling change request {ChangeRequestId}", changeRequestId);
            return (false, "An error occurred while cancelling the change request");
        }
    }
}
