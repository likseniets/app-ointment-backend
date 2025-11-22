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
                    Name = "Artur",
                    Role = UserRole.Admin,
                    Adress = "Bever 8",
                    Email = "s371452@oslomet.no",
                    Phone = "46213657",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123")
                },
                new Caregiver
                {
                    Name = "Jesper",
                    Role = UserRole.Caregiver,
                    Adress = "Bislett",
                    Email = "jemel7762@oslomet.no",
                    Phone = "82888222",
                    ImageUrl = "https://media.istockphoto.com/id/1919265357/photo/close-up-portrait-of-confident-businessman-standing-in-office.jpg?s=2048x2048&w=is&k=20&c=b31q8IXUnas7j0DCl8eMrAWMVj8YG14cP7lw5eF8TrQ=",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123")
                },
                new Caregiver
                {
                    Name = "Kjos",
                    Role = UserRole.Caregiver,
                    Adress = "Gingertown",
                    Email = "s371393@oslomet.no",
                    Phone = "82888222",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123")
                },
                new Client
                {
                    Name = "Eskil",
                    Role = UserRole.Client,
                    Adress = "Gokk",
                    Email = "s371414@oslomet.no",
                    Phone = "99884432",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123")
                }
            };

            // Insert users directly so they're immediately available in the created database
            context.AddRange(users);
            context.SaveChanges();
        }

        //if (!context.Customers.Any())
        //{
        //    var customers = new List<Customer>
        //    {
        //        new Customer { Name = "Alice Hansen", Address = "Osloveien 1"},
        //        new Customer { Name = "Bob Johansen", Address = "Oslomet gata 2"},
        //    };
        //    context.AddRange(customers);
        //    context.SaveChanges();
        //}

        if (!context.Appointments.Any())
        {
            var caregiver = context.Users.FirstOrDefault(u => u.Role == UserRole.Caregiver);
            var client = context.Users.FirstOrDefault(u => u.Role == UserRole.Client);

            if (caregiver != null && client != null)
            {
                var appointment = new Appointment
                {
                    Date = DateTime.Now,
                    ClientId = client.UserId,
                    CaregiverId = caregiver.UserId,
                    Location = "Home",
                    Description = "Initial appointment"
                };

                // Insert appointment directly so it's immediately available
                context.Appointments.Add(appointment);
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
                
                // Jesper - Available Monday, Wednesday, Friday with varying hours
                var jesper = caregivers.FirstOrDefault(c => c.Name == "Jesper");
                if (jesper != null)
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
                                        CaregiverId = jesper.UserId,
                                        Date = date,
                                        StartTime = new TimeSpan(hour, 0, 0).ToString(@"hh\:mm"),
                                        EndTime = new TimeSpan(hour + 1, 0, 0).ToString(@"hh\:mm")
                                    });
                                }
                            }
                        }
                    }
                }

                // Kjos - Available Tuesday, Thursday with varying hours
                var kjos = caregivers.FirstOrDefault(c => c.Name == "Kjos");
                if (kjos != null)
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
                                        CaregiverId = kjos.UserId,
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
                var otherCaregivers = caregivers.Where(c => c.Name != "Jesper" && c.Name != "Kjos");
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
    }

    /*        if (!context.OrderItems.Any())
            {
                var orderItems = new List<OrderItem>
                {
                    new OrderItem { ItemId = 1, Quantity = 2, OrderId = 1},
                    new OrderItem { ItemId = 2, Quantity = 1, OrderId = 1},
                    new OrderItem { ItemId = 3, Quantity = 4, OrderId = 2},
                };
                foreach (var orderItem in orderItems)
                {
                    var item = context.Items.Find(orderItem.ItemId);
                    orderItem.OrderItemPrice = orderItem.Quantity * item?.Price ?? 0;
                }
                context.AddRange(orderItems);
                context.SaveChanges();
            }
    */
}
