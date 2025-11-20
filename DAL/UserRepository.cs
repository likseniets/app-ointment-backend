using Microsoft.EntityFrameworkCore;
using app_ointment_backend.Models;

namespace app_ointment_backend.DAL;

public class UserRepository : IUserRepository
{
    private readonly UserDbContext _context;

    private readonly ILogger<UserRepository> _logger;

    public UserRepository(UserDbContext context, ILogger<UserRepository> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<IEnumerable<User>?> GetAll()
    {
        try
        {
            return await _context.Users.ToListAsync();
        }
        catch (Exception e)
        {
            _logger.LogError("[UserRepository] users ToListAsync() failed when GetAll(), error message: {e}", e.Message);
            return null;
        }
        
    }

    public async Task<IEnumerable<Caregiver>?> GetCaregivers()
    {
        try
        {
            return await _context.Caregivers
                .Where(c => c.Role == UserRole.Caregiver)
                .Include(c => c.Availability)
                .ToListAsync();
        }
        catch (Exception e)
        {
            _logger.LogError("[UserRepository] caregivers query failed when GetCaregivers(), error message: {e}", e.Message);
            return null;
        }
    }

    public async Task<User?> GetUserById(int id)
    {
        try
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == id);
        }
        catch (Exception e)
        {
            _logger.LogError("[UserRepository] users FindAsync(id) failed when GetUserById() for UserId {UserId:0000}, error message: {e}", id, e.Message);
            return null;
        }   
    }

    public async Task<User?> GetUserByEmail(string email)
    {
        try
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
        }
        catch (Exception e)
        {
            _logger.LogError("[UserRepository] users FirstOrDefaultAsync() failed when GetUserByEmail() for Email {Email}, error message: {e}", email, e.Message);
            return null;
        }
    }

    public async Task<Caregiver?> GetCaregiverWithAvailability(int caregiverId)
    {
        try
        {
            return await _context.Caregivers
                .Include(c => c.Availability)
                .FirstOrDefaultAsync(c => c.UserId == caregiverId && c.Role == UserRole.Caregiver);
        }
        catch (Exception e)
        {
            _logger.LogError("[UserRepository] caregiver query failed when GetCaregiverWithAvailability() for CaregiverId {CaregiverId:0000}, error message: {e}", caregiverId, e.Message);
            return null;
        }
    }

    public async Task<Caregiver?> GetFirstCaregiver()
    {
        try
        {
            return await _context.Caregivers
                .Include(c => c.Availability)
                .FirstOrDefaultAsync(c => c.Role == UserRole.Caregiver);
        }
        catch (Exception e)
        {
            _logger.LogError("[UserRepository] caregiver query failed when GetFirstCaregiver(), error message: {e}", e.Message);
            return null;
        }
    }

    public async Task<bool> CreateUser(User user)
    {
        try
        {
            var entity = user.Role switch
            {
                UserRole.Caregiver => new Caregiver { Name = user.Name, Adress = user.Adress, Phone = user.Phone, Email = user.Email, Role = user.Role, PasswordHash = user.PasswordHash, ImageUrl = user.ImageUrl },
                UserRole.Client => new Client { Name = user.Name, Adress = user.Adress, Phone = user.Phone, Email = user.Email, Role = user.Role, PasswordHash = user.PasswordHash, ImageUrl = user.ImageUrl },
                _ => user
            };
            _context.Add(entity);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError("[UserRepository] user creation failed for user {@user}, error message: {e}", user, e.Message);
            return false;
        }
        
    }

    public async Task<bool> UpdateUser(User user)
    {
        try
        {
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError("[UserRepository] item FindAsync(id) failed when updating the UserId {UserId:0000}, error message: {e}", user, e.Message);
            return false;
        }

    }

    public async Task<bool> DeleteUser(int id)
    {
        try
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null)
            {
                _logger.LogError("[UserRepository] user not found for UserId {UserId:0000}", id);
                return false;
            }

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return true;
        }
        catch (Exception e)
        {
            _logger.LogError("[UserRepository] user deletion failed for UserId {UserId:0000}, error message: {e}", id, e.Message);
                return false;
        }
        
    }
}
