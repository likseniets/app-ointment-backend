using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;

namespace app_ointment_backend.Models;

public class AppointmentDbContext : DbContext
{
    public AppointmentDbContext(DbContextOptions<AppointmentDbContext> options) : base(options)
    {
        try
        {
            Database.EnsureCreated();
            Console.WriteLine("Database created successfully");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Database creation failed: {ex.Message}");
        }
    }

    public DbSet<Appointment> Appointment { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		optionsBuilder.UseLazyLoadingProxies();
	}
}