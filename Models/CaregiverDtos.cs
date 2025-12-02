namespace app_ointment_backend.Models;

public class GetCaregiverWithAvailability
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string Adress { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public virtual List<Availability>? Availability { get; set; }

    //convert Caregiver model to GetCaregiverWithAvailability DTO
    public static GetCaregiverWithAvailability FromCaregiver(Caregiver caregiver)
    {
        return new GetCaregiverWithAvailability
        {
            UserId = caregiver.UserId,
            Name = caregiver.Name,
            Role = caregiver.Role,
            Adress = caregiver.Adress,
            Phone = caregiver.Phone,
            Email = caregiver.Email,
            ImageUrl = caregiver.ImageUrl,
            Availability = caregiver.Availability
        };
    }
}
