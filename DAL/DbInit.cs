using Microsoft.EntityFrameworkCore;
using app_ointment_backend.Models;

namespace app_ointment_backend.DAL;

public static class DBInit
{
    public static void Seed(IApplicationBuilder app)
    {
        using var serviceScope = app.ApplicationServices.CreateScope();
        UserDbContext _userDbContext = serviceScope.ServiceProvider.GetRequiredService<UserDbContext>();
        //_userDbContext.Database.EnsureDeleted();
        //_userDbContext.Database.EnsureCreated();

        if (!_userDbContext.Users.Any())
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
            _userDbContext.AddRange(users);
            _userDbContext.SaveChanges();
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

        if (!_userDbContext.Appointments.Any())
        {
            var appointments = new List<Appointment>
            {
                new Appointment {
                    AppointmentId = 2,
                    Date = DateTime.Now,
                    ClientId = 2,
                    Client = new Client {
                    Name = "Eskil",
                    Role = UserRole.Client,
                    Adress = "Gokk",
                    Email = "s371414@oslomet.no",
                    Phone = "99884432",
                    ImageUrl = "/images/eskil.jpg"
                },
                CaregiverId = 1,
                Caregiver = new Caregiver
                {
                    Name = "Jesper",
                    Role = UserRole.Caregiver,
                    Adress = "Bislett",
                    Email = "jemel7762@oslomet.no",
                    Phone = "82888222",
                    ImageUrl = "/images/jeppe.jpg"
                },
                Location = "Home"},
            };
            _userDbContext.AddRange(appointments);
            _userDbContext.SaveChanges();
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