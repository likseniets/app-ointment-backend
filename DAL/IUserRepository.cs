using app_ointment_backend.Models;

namespace app_ointment_backend.DAL;

public interface IUserRepository
{
    Task<List<User>> GetAll();
    Task<User?> GetUserById(int userId);
    Task CreateUser(User user);
    Task UpdateUser(User user);
    Task DeleteUser(int userId);
}