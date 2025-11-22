namespace app_ointment_backend.Models;

public class UserDto
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public UserRole Role { get; set; }
    public string Adress { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }

    public static UserDto FromUser(User user)
    {
        return new UserDto
        {
            UserId = user.UserId,
            Name = user.Name,
            Role = user.Role,
            Adress = user.Adress,
            Phone = user.Phone,
            Email = user.Email,
            ImageUrl = user.ImageUrl
        };
    }
}
