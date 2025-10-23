using Microsoft.AspNetCore.Identity;

namespace app_ointment_backend.Models;

public class ApplicationUser : IdentityUser
{
    public string Name { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
}
