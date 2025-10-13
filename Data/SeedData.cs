using app_ointment_backend.Models;

namespace app_ointment_backend.Data;

public static class SeedData
{
    public static void Initialize(ApplicationDbContext context)
    {
        // Add healthcare personnel
        var healthcarePersonnel = new List<User>
        {
            new User
            {
                Name = "Anna Hansen",
                Email = "anna.hansen@healthcare.no",
                Phone = "98765432",
                Role = UserRole.HealthcarePersonnel
            },
            new User
            {
                Name = "Lars Olsen",
                Email = "lars.olsen@healthcare.no",
                Phone = "98765433",
                Role = UserRole.HealthcarePersonnel
            },
            new User
            {
                Name = "Maria Johansen",
                Email = "maria.johansen@healthcare.no",
                Phone = "98765434",
                Role = UserRole.HealthcarePersonnel
            }
        };

        // Add elderly users
        var elderlyUsers = new List<User>
        {
            new User
            {
                Name = "Olav Berg",
                Email = "olav.berg@epost.no",
                Phone = "12345678",
                Role = UserRole.Elderly
            },
            new User
            {
                Name = "Kari Nordmann",
                Email = "kari.nordmann@epost.no",
                Phone = "12345679",
                Role = UserRole.Elderly
            },
            new User
            {
                Name = "Per Svendsen",
                Email = "per.svendsen@epost.no",
                Phone = "12345680",
                Role = UserRole.Elderly
            }
        };

        context.Users.AddRange(healthcarePersonnel);
        context.Users.AddRange(elderlyUsers);
        context.SaveChanges();

        // Add available days for healthcare personnel
        var availableDays = new List<AvailableDay>();
        for (int i = 1; i <= 14; i++)
        {
            var date = DateTime.Today.AddDays(i);
            
            // Anna works Monday to Friday
            if (date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday)
            {
                availableDays.Add(new AvailableDay
                {
                    HealthcarePersonnelId = healthcarePersonnel[0].Id,
                    Date = date,
                    StartTime = new TimeSpan(8, 0, 0),
                    EndTime = new TimeSpan(16, 0, 0),
                    Notes = "Morning and afternoon shifts available"
                });
            }
            
            // Lars works Tuesday to Saturday
            if (date.DayOfWeek != DayOfWeek.Sunday && date.DayOfWeek != DayOfWeek.Monday)
            {
                availableDays.Add(new AvailableDay
                {
                    HealthcarePersonnelId = healthcarePersonnel[1].Id,
                    Date = date,
                    StartTime = new TimeSpan(9, 0, 0),
                    EndTime = new TimeSpan(17, 0, 0),
                    Notes = "Flexible schedule"
                });
            }
            
            // Maria works weekends and Wednesdays
            if (date.DayOfWeek == DayOfWeek.Saturday || date.DayOfWeek == DayOfWeek.Sunday || date.DayOfWeek == DayOfWeek.Wednesday)
            {
                availableDays.Add(new AvailableDay
                {
                    HealthcarePersonnelId = healthcarePersonnel[2].Id,
                    Date = date,
                    StartTime = new TimeSpan(10, 0, 0),
                    EndTime = new TimeSpan(18, 0, 0),
                    Notes = "Weekend specialist"
                });
            }
        }

        context.AvailableDays.AddRange(availableDays);
        context.SaveChanges();

        // Add sample appointments
        var appointments = new List<Appointment>
        {
            new Appointment
            {
                ElderlyUserId = elderlyUsers[0].Id,
                HealthcarePersonnelId = healthcarePersonnel[0].Id,
                AppointmentDate = DateTime.Today.AddDays(2),
                StartTime = new TimeSpan(10, 0, 0),
                EndTime = new TimeSpan(11, 0, 0),
                Status = AppointmentStatus.Scheduled,
                Notes = "Regular weekly checkup"
            },
            new Appointment
            {
                ElderlyUserId = elderlyUsers[1].Id,
                HealthcarePersonnelId = healthcarePersonnel[1].Id,
                AppointmentDate = DateTime.Today.AddDays(3),
                StartTime = new TimeSpan(14, 0, 0),
                EndTime = new TimeSpan(15, 30, 0),
                Status = AppointmentStatus.Scheduled,
                Notes = "Grocery shopping assistance"
            },
            new Appointment
            {
                ElderlyUserId = elderlyUsers[2].Id,
                HealthcarePersonnelId = healthcarePersonnel[2].Id,
                AppointmentDate = DateTime.Today.AddDays(5),
                StartTime = new TimeSpan(11, 0, 0),
                EndTime = new TimeSpan(12, 0, 0),
                Status = AppointmentStatus.Scheduled,
                Notes = "Medication reminder and companionship"
            }
        };

        context.Appointments.AddRange(appointments);
        context.SaveChanges();

        // Add tasks for appointments
        var appointmentTasks = new List<AppointmentTask>
        {
            // Tasks for first appointment
            new AppointmentTask
            {
                AppointmentId = appointments[0].Id,
                TaskName = "Health Check",
                TaskType = TaskType.HealthCheck,
                Description = "Check blood pressure and temperature"
            },
            new AppointmentTask
            {
                AppointmentId = appointments[0].Id,
                TaskName = "Medication Review",
                TaskType = TaskType.MedicationReminder,
                Description = "Review medication schedule"
            },
            
            // Tasks for second appointment
            new AppointmentTask
            {
                AppointmentId = appointments[1].Id,
                TaskName = "Grocery Shopping",
                TaskType = TaskType.Shopping,
                Description = "Buy groceries from shopping list"
            },
            new AppointmentTask
            {
                AppointmentId = appointments[1].Id,
                TaskName = "Put Away Groceries",
                TaskType = TaskType.HouseholdChores,
                Description = "Help organize groceries in kitchen"
            },
            
            // Tasks for third appointment
            new AppointmentTask
            {
                AppointmentId = appointments[2].Id,
                TaskName = "Morning Medication",
                TaskType = TaskType.MedicationReminder,
                Description = "Ensure medication is taken at 11:00"
            },
            new AppointmentTask
            {
                AppointmentId = appointments[2].Id,
                TaskName = "Social Companionship",
                TaskType = TaskType.Companionship,
                Description = "Chat and provide company"
            }
        };

        context.AppointmentTasks.AddRange(appointmentTasks);
        context.SaveChanges();
    }
}

