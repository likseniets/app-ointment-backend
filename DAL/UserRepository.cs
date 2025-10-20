using Microsoft.EntityFrameworkCore;
using app_ointment_backend.Models;

namespace app_ointment_backend.DAL;

public class UserRepository : IUserRepository
{
    private readonly UserDbContext _context;

    public UserRepository(UserDbContext context)
    {
        _context = context;
    }

    // FIKSET: Alle referanser endret fra Users til AppUsers for å unngå konflikt med Identity
    public async Task<List<User>> GetAll()
    {
        return await _context.AppUsers.ToListAsync();
    }

    public async Task<User?> GetUserById(int id)
    {
        return await _context.AppUsers.FindAsync(id);
    }

    public async Task CreateUser(User user)
    {
        _context.AppUsers.Add(user);
        await _context.SaveChangesAsync();
    }

    public async Task UpdateUser(User user)
    {
        _context.AppUsers.Update(user);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteUser(int id)
    {
        var user = await _context.AppUsers.FindAsync(id);
        if (user != null)
        {
            _context.AppUsers.Remove(user);
            await _context.SaveChangesAsync();
        }
    }
}
