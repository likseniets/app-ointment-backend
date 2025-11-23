using Microsoft.EntityFrameworkCore;
using app_ointment_backend.Models;
using BCrypt.Net;

namespace app_ointment_backend.DAL;

public static class DBInit
{
    public static void Seed(IApplicationBuilder app)
    {
        using var serviceScope = app.ApplicationServices.CreateScope();
        var userRepository = serviceScope.ServiceProvider.GetRequiredService<IUserRepository>();
        var appointmentRepository = serviceScope.ServiceProvider.GetRequiredService<IAppointmentRepository>();
        var availabilityRepository = serviceScope.ServiceProvider.GetRequiredService<IAvailabilityRepository>();
        var context = serviceScope.ServiceProvider.GetRequiredService<UserDbContext>();

        // Only used for development
        context.Database.EnsureDeleted(); // This will delete the existing database
        context.Database.EnsureCreated(); // This will create a new database with all required tables

        if (!context.Users.Any())
        {
            var users = new List<User>
            {
                new User
                {
                    Name = "Admin1",
                    Role = UserRole.Admin,
                    Adress = "Eksempelveien 1",
                    Email = "admin@example.com",
                    Phone = "46213657",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123")
                },
                new Caregiver
                {
                    Name = "Caregiver1",
                    Role = UserRole.Caregiver,
                    Adress = "Bislett gate 1",
                    Email = "caregiver1@example.com",
                    Phone = "82888222",
                    ImageUrl = "https://media.istockphoto.com/id/1919265357/photo/close-up-portrait-of-confident-businessman-standing-in-office.jpg?s=2048x2048&w=is&k=20&c=b31q8IXUnas7j0DCl8eMrAWMVj8YG14cP7lw5eF8TrQ=",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123")
                },
                new Caregiver
                {
                    Name = "Caregiver2",
                    Role = UserRole.Caregiver,
                    Adress = "Gingertown",
                    Email = "caregiver2@example.com",
                    Phone = "82889322",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123")
                },
                new Client
                {
                    Name = "Client1",
                    Role = UserRole.Client,
                    Adress = "Gokk",
                    Email = "client1@example.com",
                    Phone = "99884432",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123")
                },
                new Client
                {
                    Name = "Client2",
                    Role = UserRole.Client,
                    Adress = "Gokk",
                    Email = "client2@example.com",
                    Phone = "98912313",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123")
                }
            };

            // Insert users directly so they're immediately available in the created database
            context.AddRange(users);
            context.SaveChanges();
        }

        if (!context.Appointments.Any())
        {
            var caregiver = context.Users.FirstOrDefault(u => u.Role == UserRole.Caregiver);
            var client = context.Users.FirstOrDefault(u => u.Role == UserRole.Client);

            if (caregiver != null && client != null)
            {
                var appointment1 = new Appointment
                {
                    Date = DateTime.Now.AddDays(1),
                    ClientId = client.UserId,
                    CaregiverId = caregiver.UserId,
                    Task = AppointmentTask.AssistanceWithDailyLiving
                };

                var appointment2 = new Appointment
                {
                    Date = DateTime.Now.AddDays(2),
                    ClientId = client.UserId,
                    CaregiverId = caregiver.UserId,
                    Task = AppointmentTask.PersonalHygiene
                };

                // Insert appointment directly so it's immediately available
                context.Appointments.Add(appointment1);
                context.Appointments.Add(appointment2);
                context.SaveChanges();
            }
        }

        // Seed initial availability for all caregivers with different schedules
        if (!context.Availabilities.Any())
        {
            var caregivers = context.Users.OfType<Caregiver>().ToList();
            if (caregivers.Any())
            {
                var startDate = DateTime.Today;
                var availabilities = new List<Availability>();
                var random = new Random();

                // Caregiver2 - Available Monday, Wednesday, Friday with varying hours
                var caregiver1 = caregivers.FirstOrDefault(c => c.Name == "Caregiver1");
                if (caregiver1 != null)
                {
                    for (int i = 0; i < 14; i++) // 2 weeks
                    {
                        var date = startDate.AddDays(i);
                        if (date.DayOfWeek == DayOfWeek.Monday ||
                            date.DayOfWeek == DayOfWeek.Wednesday ||
                            date.DayOfWeek == DayOfWeek.Friday)
                        {
                            // Random start hour between 8-10, random end hour between 16-18
                            var startHour = random.Next(8, 11);
                            var endHour = random.Next(16, 19);

                            for (var hour = startHour; hour < endHour; hour++)
                            {
                                // Randomly skip some slots (70% chance to include)
                                if (random.NextDouble() > 0.3)
                                {
                                    availabilities.Add(new Availability
                                    {
                                        CaregiverId = caregiver1.UserId,
                                        Date = date,
                                        StartTime = new TimeSpan(hour, 0, 0).ToString(@"hh\:mm"),
                                        EndTime = new TimeSpan(hour + 1, 0, 0).ToString(@"hh\:mm")
                                    });
                                }
                            }
                        }
                    }
                }

                // caregiver2/ - Available Tuesday, Thursday with varying hours
                var caregiver2 = caregivers.FirstOrDefault(c => c.Name == "Caregiver2");
                if (caregiver2 != null)
                {
                    for (int i = 0; i < 14; i++) // 2 weeks
                    {
                        var date = startDate.AddDays(i);
                        if (date.DayOfWeek == DayOfWeek.Tuesday ||
                            date.DayOfWeek == DayOfWeek.Thursday)
                        {
                            // Random start hour between 9-11, random end hour between 15-18
                            var startHour = random.Next(9, 12);
                            var endHour = random.Next(15, 19);

                            for (var hour = startHour; hour < endHour; hour++)
                            {
                                // Randomly skip some slots (80% chance to include)
                                if (random.NextDouble() > 0.2)
                                {
                                    availabilities.Add(new Availability
                                    {
                                        CaregiverId = caregiver2.UserId,
                                        Date = date,
                                        StartTime = new TimeSpan(hour, 0, 0).ToString(@"hh\:mm"),
                                        EndTime = new TimeSpan(hour + 1, 0, 0).ToString(@"hh\:mm")
                                    });
                                }
                            }
                        }
                    }
                }

                // Add any other caregivers with varied schedule (Mon-Fri with random hours)
                var otherCaregivers = caregivers.Where(c => c.Name != "Caregiver1" && c.Name != "Caregiver2");
                foreach (var cg in otherCaregivers)
                {
                    for (int i = 0; i < 14; i++)
                    {
                        var date = startDate.AddDays(i);
                        if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
                        {
                            // Random start hour between 7-10, random end hour between 14-17
                            var startHour = random.Next(7, 11);
                            var endHour = random.Next(14, 18);

                            for (var hour = startHour; hour < endHour; hour++)
                            {
                                // Randomly skip some slots (75% chance to include)
                                if (random.NextDouble() > 0.25)
                                {
                                    availabilities.Add(new Availability
                                    {
                                        CaregiverId = cg.UserId,
                                        Date = date,
                                        StartTime = new TimeSpan(hour, 0, 0).ToString(@"hh\:mm"),
                                        EndTime = new TimeSpan(hour + 1, 0, 0).ToString(@"hh\:mm")
                                    });
                                }
                            }
                        }
                    }
                }

                context.Availabilities.AddRange(availabilities);
                context.SaveChanges();
            }
        }

        // Seed a mock pending change request
        if (!context.AppointmentChangeRequests.Any())
        {
            var appointment = context.Appointments.FirstOrDefault();

            if (appointment != null)
            {
                // Find an available slot for the change request (use an existing availability slot)
                var availableSlot = context.Availabilities
                    .Where(a => a.CaregiverId == appointment.CaregiverId && a.Date > DateTime.Now)
                    .OrderBy(a => a.Date)
                    .FirstOrDefault();

                DateTime? newDateTime = null;
                if (availableSlot != null)
                {
                    // Parse the time from the availability slot
                    var timeParts = availableSlot.StartTime.Split(':');
                    if (timeParts.Length >= 2 && int.TryParse(timeParts[0], out int hour))
                    {
                        newDateTime = availableSlot.Date.Date.AddHours(hour);
                    }
                }

                var changeRequest1 = new AppointmentChangeRequest
                {
                    AppointmentId = appointment.AppointmentId,
                    RequestedByUserId = appointment.ClientId,
                    OldTask = appointment.Task,
                    OldDateTime = appointment.Date,
                    NewTask = AppointmentTask.MedicationReminders,
                    NewDateTime = newDateTime, // Use an actual available timeslot
                    Status = ChangeRequestStatus.Pending,
                    RequestedAt = DateTime.UtcNow
                };

                context.AppointmentChangeRequests.Add(changeRequest1);

                // Create a second change request for the second appointment (if it exists)
                var appointment2 = context.Appointments.Skip(1).FirstOrDefault();
                if (appointment2 != null)
                {
                    // Find an available slot for the second change request
                    var availableSlot2 = context.Availabilities
                        .Where(a => a.CaregiverId == appointment2.CaregiverId && a.Date > DateTime.Now)
                        .OrderBy(a => a.Date)
                        .Skip(1) // Skip the first slot to get a different one
                        .FirstOrDefault();

                    DateTime? newDateTime2 = null;
                    if (availableSlot2 != null)
                    {
                        var timeParts2 = availableSlot2.StartTime.Split(':');
                        if (timeParts2.Length >= 2 && int.TryParse(timeParts2[0], out int hour2))
                        {
                            newDateTime2 = availableSlot2.Date.Date.AddHours(hour2);
                        }
                    }

                    var changeRequest2 = new AppointmentChangeRequest
                    {
                        AppointmentId = appointment2.AppointmentId,
                        RequestedByUserId = appointment2.CaregiverId,
                        OldTask = appointment2.Task,
                        OldDateTime = appointment2.Date,
                        NewTask = AppointmentTask.Companionship,
                        NewDateTime = newDateTime2,
                        Status = ChangeRequestStatus.Pending,
                        RequestedAt = DateTime.UtcNow
                    };

                    context.AppointmentChangeRequests.Add(changeRequest2);
                }

                context.SaveChanges();
            }
        }
    }
}
