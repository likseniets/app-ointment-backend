using app_ointment_backend.Models;

namespace app_ointment_backend.DAL;

public interface IUserRepository
{
    Task<IEnumerable<User>?> GetAll();
    Task<IEnumerable<Caregiver>?> GetCaregivers();
    Task<IEnumerable<Client>?> GetClients();
    Task<User?> GetUserById(int id);
    Task<User?> GetUserByEmail(string email);
    Task<Caregiver?> GetCaregiverWithAvailability(int caregiverId);
    Task<Caregiver?> GetFirstCaregiver();
    Task<bool> CreateUser(User user);
    Task<bool> UpdateUser(User user);
    Task<bool> DeleteUser(int id);
}