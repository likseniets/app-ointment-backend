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

    public async Task<User?> GetUserById(int id)
    {
        try
        {
            return await _context.Users.FindAsync(id);
        }
        catch (Exception e)
        {
            _logger.LogError("[UserRepository] users FindAsync(id) failed when GetUserById() for UserId {UserId:0000}, error message: {e}", id, e.Message);
            return null;
        }
        
    }

    /// <summary>
    /// ENDRET: CreateUser - Opprettet riktig brukertype basert på rolle
    /// Før: Opprettet bare vanlige User-objekter
    /// Nå: Oppretter Caregiver, Client eller User basert på valgt rolle
    /// </summary>
    public async Task<bool> CreateUser(User user)
    {
        try
        {
            // LAGT TIL: Opprett riktig type basert på rolle
            switch (user.Role)
            {
                case UserRole.Caregiver:
                    var caregiver = new Caregiver
                    {
                        Name = user.Name,
                        Role = user.Role,
                        Adress = user.Adress,
                        Phone = user.Phone,
                        Email = user.Email,
                        ImageUrl = user.ImageUrl
                    };
                    _context.Caregivers.Add(caregiver);
                    break;
                    
                case UserRole.Client:
                    var client = new Client
                    {
                        Name = user.Name,
                        Role = user.Role,
                        Adress = user.Adress,
                        Phone = user.Phone,
                        Email = user.Email,
                        ImageUrl = user.ImageUrl
                    };
                    _context.Clients.Add(client);
                    break;
                    
                default:
                    // Admin eller andre roller - bruk vanlig User
                    _context.Users.Add(user);
                    break;
            }
            
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
