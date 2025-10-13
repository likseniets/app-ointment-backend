using Microsoft.EntityFrameworkCore;
using app_ointment_backend.Models;

namespace app_ointment_backend.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users { get; set; }
    public DbSet<Appointment> Appointments { get; set; }
    public DbSet<AvailableDay> AvailableDays { get; set; }
    public DbSet<AppointmentTask> AppointmentTasks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure User entity
        modelBuilder.Entity<User>()
            .HasIndex(u => u.Email)
            .IsUnique();

        // Configure Appointment relationships
        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.ElderlyUser)
            .WithMany(u => u.Appointments)
            .HasForeignKey(a => a.ElderlyUserId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Appointment>()
            .HasOne(a => a.HealthcarePersonnel)
            .WithMany()
            .HasForeignKey(a => a.HealthcarePersonnelId)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure AvailableDay relationship
        modelBuilder.Entity<AvailableDay>()
            .HasOne(ad => ad.HealthcarePersonnel)
            .WithMany(u => u.AvailableDays)
            .HasForeignKey(ad => ad.HealthcarePersonnelId)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure AppointmentTask relationship
        modelBuilder.Entity<AppointmentTask>()
            .HasOne(at => at.Appointment)
            .WithMany(a => a.Tasks)
            .HasForeignKey(at => at.AppointmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

