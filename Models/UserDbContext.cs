using Microsoft.EntityFrameworkCore;

namespace app_ointment_backend.Models;

public class UserDbContext : DbContext
{
	public UserDbContext(DbContextOptions<UserDbContext> options) : base(options)
	{
		//Database.EnsureCreated(); //Removed because migrations
	}

	public DbSet<User> Users { get; set; }
	public DbSet<Client> Clients { get; set; }
	public DbSet<Caregiver> Caregivers { get; set; }
	public DbSet<Appointment> Appointments { get; set; }

	protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
	{
		optionsBuilder.UseLazyLoadingProxies();
	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		base.OnModelCreating(modelBuilder);

		// Explicitly map Appointment -> User relationships to avoid ambiguity
		modelBuilder.Entity<Appointment>()
			.HasOne(a => a.Caregiver)
			.WithMany()
			.HasForeignKey(a => a.CaregiverId)
			.OnDelete(DeleteBehavior.Cascade);

		modelBuilder.Entity<Appointment>()
			.HasOne(a => a.Client)
			.WithMany()
			.HasForeignKey(a => a.ClientId)
			.OnDelete(DeleteBehavior.Cascade);

		// Ignore derived-type collection navigations that conflict with the above
		modelBuilder.Entity<Caregiver>().Ignore(c => c.Appointments);
		modelBuilder.Entity<Client>().Ignore(c => c.Appointments);
	}
}

// Should this be changed to eager loading (.Include/ThenInclude) instead? Can give predictable, minimal queries and avoid N+1 query spikes.
// Will test and see if this is needed.
