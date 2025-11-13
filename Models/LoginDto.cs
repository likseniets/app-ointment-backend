using System.ComponentModel.DataAnnotations;

namespace app_ointment_backend.Models;

public class LoginDto
{
    [Required]
    [RegularExpression(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$", ErrorMessage = "Please enter a valid email address.")]
    public required string Email { get; set; }

    [Required]
    public required string Password { get; set; }
}
