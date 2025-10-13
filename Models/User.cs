using System;
namespace app_ointment_backend.Models;

public enum UserRole {Caregiver = 1, Caretaker = 2, Admin = 3}
public class User
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public UserRole Role { get; set; }

}
