using app_ointment_backend.Models;

namespace app_ointment_backend.Services;

//interface to define what is in the user service

public interface IUserService
{
    Task<User?> GetUserById(int userId);
    Task<User?> GetUserByEmail(string email);
    Task<IEnumerable<User>?> GetAllUsers();
    Task<IEnumerable<User>?> GetCaregivers();
    Task<IEnumerable<User>?> GetClients();
    Task<(bool Success, string Message, User? User)> CreateClient(CreateUserDto dto);
    Task<(bool Success, string Message, User? User)> CreateCaregiver(CreateUserDto dto);
    Task<(bool Success, string Message, User? User)> CreateAdmin(CreateUserDto dto);
    Task<(bool Success, string Message)> UpdateUser(int userId, UpdateUserDto dto);
    Task<(bool Success, string Message)> ChangePassword(int userId, string oldPassword, string newPassword);
    Task<(bool Success, string Message)> DeleteUser(int userId);
    Task<bool> ValidatePassword(int userId, string password);
}
