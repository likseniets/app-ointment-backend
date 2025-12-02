using System.ComponentModel.DataAnnotations;

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

    //convert User model to UserDto
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


public class CreateUserDto
{
    [RegularExpression(@"[0-9a-zA-ZæøåÆØÅ. \-]{2,20}", ErrorMessage = "The Name must be numbers or letters and between 2 to 20 characters.")]
    [Display(Name = "Item name")]
    public required string Name { get; set; }

    public UserRole Role { get; set; }
    public string Adress { get; set; } = string.Empty;

    [RegularExpression(@"^[+]*[(]{0,2}[0-9]{7,20}[)]{0,1}[-\s\./0-9]*$", ErrorMessage = "Please enter a valid phone number, minimum 7 characters.")]
    public string Phone { get; set; } = string.Empty;

    [RegularExpression(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$", ErrorMessage = "Please enter a valid email adress in the form of example@mail.com.")]
    public string Email { get; set; } = string.Empty;

    [Required]
    [MinLength(6, ErrorMessage = "Password must be at least 6 characters long.")]
    public required string Password { get; set; }

    public string? ImageUrl { get; set; }
}

public class UpdateUserDto
{
    [RegularExpression(@"[0-9a-zA-ZæøåÆØÅ. \-]{2,20}", ErrorMessage = "The Name must be numbers or letters and between 2 to 20 characters.")]
    [Display(Name = "Item name")]
    public required string Name { get; set; }
    public string Adress { get; set; } = string.Empty;

    [RegularExpression(@"^[+]*[(]{0,2}[0-9]{7,20}[)]{0,1}[-\s\./0-9]*$", ErrorMessage = "Please enter a valid phone number, minimum 7 characters.")]
    public string Phone { get; set; } = string.Empty;

    [RegularExpression(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$", ErrorMessage = "Please enter a valid email adress in the form of example@mail.com.")]
    public string Email { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }
}