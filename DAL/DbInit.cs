using Microsoft.EntityFrameworkCore;
using app_ointment_backend.Models;

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
                    ImageUrl = "/images/artur.jpg"
                },
                new Caregiver
                {
                    Name = "Jesper",
                    Role = UserRole.Caregiver,
                    Adress = "Bislett",
                    Email = "jemel7762@oslomet.no",
                    Phone = "82888222",
                    ImageUrl = "/images/jeppe.jpg"
                },
                new Caregiver
                {
                    Name = "Eskil",
                    Role = UserRole.Caregiver,
                    Adress = "Gokk",
                    Email = "s371414@oslomet.no",
                    Phone = "99884432",
                    ImageUrl = "/images/eskil.jpg"
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
                    Location = "Home"
                };

                // Insert appointment directly so it's immediately available
                context.Appointments.Add(appointment);
                context.SaveChanges();
            }
        }

        // Seed initial availability for caregivers
        if (!context.Availabilities.Any())
        {
            var caregiver = context.Users.FirstOrDefault(u => u.Role == UserRole.Caregiver);

            if (caregiver != null)
            {
                var availabilities = new List<Availability>
                {
                    new Availability
                    {
                        CaregiverId = caregiver.UserId,
                        DayOfWeek = DayOfWeek.Monday,
                        StartTime = "09:00",
                        EndTime = "17:00"
                    },
                    new Availability
                    {
                        CaregiverId = caregiver.UserId,
                        DayOfWeek = DayOfWeek.Wednesday,
                        StartTime = "09:00",
                        EndTime = "17:00"
                    }
                };

                // Insert availability slots directly so they're available immediately
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