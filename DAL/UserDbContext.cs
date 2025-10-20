using Microsoft.EntityFrameworkCore;
using app_ointment_backend.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

namespace app_ointment_backend.DAL;

public class UserDbContext : IdentityDbContext
{
	public UserDbContext(DbContextOptions<UserDbContext> options) : base(options)
	{
		//Database.EnsureCreated(); //Removed because migrations
	}

	// FIKSET: Endret fra 'Users' til 'AppUsers' for å unngå konflikt med Identity's innebygde Users property
	public DbSet<User> AppUsers { get; set; }
	public DbSet<Client> Clients { get; set; }
	public DbSet<Caregiver> Caregivers { get; set; }
	public DbSet<Appointment> Appointments { get; set; }

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		optionsBuilder.UseLazyLoadingProxies();
	}
}

// Should this be changed to eager loading (.Include/ThenInclude) instead? Can give predictable, minimal queries and avoid N+1 query spikes.
// Will test and see if this is needed.