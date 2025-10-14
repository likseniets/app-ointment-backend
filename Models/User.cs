using System;
namespace app_ointment_backend.Models;

public enum UserRole {Caregiver = 1, Client = 2, Admin = 3}
public class User
{
    public int UserId { get; set; }
    public required string Name { get; set; }
    public UserRole Role { get; set; }
    public string Adress { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }

}