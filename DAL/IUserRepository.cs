using app_ointment_backend.Models;

namespace app_ointment_backend.DAL;

public interface IUserRepository
{
    Task<IEnumerable<User>?> GetAll();
    Task<User?> GetUserById(int id);
    Task<bool> CreateUser(User user);
    Task<bool> UpdateUser(User user);
    Task<bool> DeleteUser(int id);
}