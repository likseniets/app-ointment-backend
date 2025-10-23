using System.ComponentModel.DataAnnotations;
namespace app_ointment_backend.Models;

public enum UserRole {Caregiver = 1, Client = 2, Admin = 3}
public class User
{
    public int UserId { get; set; }

    // This regex is from the course demo for input validation
    [RegularExpression(@"[0-9a-zA-ZæøåÆØÅ. \-]{2,20}", ErrorMessage = "The Name must be numbers or letters and between 2 to 20 characters.")]
    [Display(Name = "Item name")]
    public required string Name { get; set; }

    public UserRole Role { get; set; }
    public string Adress { get; set; } = string.Empty;

    //regex from Regexr.com, changed minimum characters allowed to avoid invalid phone numbers
    [RegularExpression(@"^[+]*[(]{0,2}[0-9]{7,20}[)]{0,1}[-\s\./0-9]*$", ErrorMessage = "Please enter a valid phone number, minimum 7 characters.")]
    public string Phone { get; set; } = string.Empty;

    //regex from stackoverflow
    [RegularExpression(@"^([\w\.\-]+)@([\w\-]+)((\.(\w){2,3})+)$", ErrorMessage = "Please enter a valid email adress in the form of example@mail.com.")]
    public string Email { get; set; } = string.Empty;

    public string? ImageUrl { get; set; }

}