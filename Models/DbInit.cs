using Microsoft.EntityFrameworkCore;

namespace app_ointment_backend.Models;

public static class DBInit
{
    public static void Seed(IApplicationBuilder app)
    {
        using var serviceScope = app.ApplicationServices.CreateScope();
        UserDbContext context = serviceScope.ServiceProvider.GetRequiredService<UserDbContext>();
        context.Database.EnsureDeleted();
        context.Database.EnsureCreated();

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

                new User
                {
                    Name = "Jesper",
                    Role = UserRole.Caregiver,
                    Adress = "Bislett",
                    Email = "jemel7762@oslomet.no",
                    Phone = "82888222",
                    ImageUrl = "/images/jeppe.jpg"
                },

                new User
                {
                    Name = "Eskil",
                    Role = UserRole.Client,
                    Adress = "Gokk",
                    Email = "s371414@oslomet.no",
                    Phone = "99884432",
                    ImageUrl = "/images/eskil.jpg"
                },

            };
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
            var appointments = new List<Appointment>
            {
                new Appointment {AppointmentDate = DateTime.Today.ToString(), ClientId = 1, CaregiverId = 1},
                new Appointment {AppointmentDate = DateTime.Today.ToString(), ClientId = 2, CaregiverId = 1},
            };
            context.AddRange(appointments);
            context.SaveChanges();
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
}