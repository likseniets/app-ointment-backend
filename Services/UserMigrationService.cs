using Microsoft.AspNetCore.Identity;
using app_ointment_backend.Models;
using app_ointment_backend.DAL;

namespace app_ointment_backend.Services;

public class UserMigrationService
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly UserDbContext _context;

    public UserMigrationService(UserManager<ApplicationUser> userManager, UserDbContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    public async Task MigrateExistingUsersAsync()
    {
        // Hent alle eksisterende brukere fra AppUsers
        var existingUsers = _context.AppUsers.ToList();

        foreach (var user in existingUsers)
        {
            // Sjekk om brukeren allerede eksisterer i Identity-systemet
            var existingIdentityUser = await _userManager.FindByEmailAsync(user.Email);
            
            if (existingIdentityUser == null)
            {
                // Opprett ny Identity-bruker basert på eksisterende bruker
                var identityUser = new ApplicationUser
                {
                    UserName = user.Email, // Bruk e-post som brukernavn
                    Email = user.Email,
                    Name = user.Name,
                    Role = user.Role,
                    Address = user.Adress,
                    Phone = user.Phone,
                    ImageUrl = user.ImageUrl,
                    EmailConfirmed = true // Sett som bekreftet siden de allerede eksisterer
                };

                // Generer et tilfeldig passord
                var password = GenerateRandomPassword();
                
                // Opprett brukeren med passordet
                var result = await _userManager.CreateAsync(identityUser, password);
                
                if (result.Succeeded)
                {
                    Console.WriteLine($"Opprettet Identity-bruker for {user.Name} med e-post: {user.Email}");
                    Console.WriteLine($"Midlertidig passord: {password}");
                    Console.WriteLine("---");
                }
                else
                {
                    Console.WriteLine($"Feil ved opprettelse av Identity-bruker for {user.Name}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
        }
    }

    private string GenerateRandomPassword()
    {
        // Generer et sikker tilfeldig passord
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789!@#$%^&*";
        var random = new Random();
        return new string(Enumerable.Repeat(chars, 12)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
