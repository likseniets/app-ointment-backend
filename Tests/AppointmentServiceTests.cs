using Xunit;
using Moq;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using app_ointment_backend.DAL;
using app_ointment_backend.Models;
using app_ointment_backend.Services;

namespace app_ointment_backend.Tests;

public class AppointmentServiceTests
{
    private readonly Mock<IAppointmentRepository> _mockAppointmentRepo;
    private readonly Mock<IAvailabilityRepository> _mockAvailabilityRepo;
    private readonly Mock<IUserRepository> _mockUserRepo;
    private readonly UserDbContext _context;
    private readonly Mock<ILogger<AppointmentService>> _mockLogger;
    private readonly AppointmentService _service;

    public AppointmentServiceTests()
    {
        // Setup in-memory database
        var options = new DbContextOptionsBuilder<UserDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new UserDbContext(options);

        // Setup mocks
        _mockAppointmentRepo = new Mock<IAppointmentRepository>();
        _mockAvailabilityRepo = new Mock<IAvailabilityRepository>();
        _mockUserRepo = new Mock<IUserRepository>();
        _mockLogger = new Mock<ILogger<AppointmentService>>();

        // Create service
        _service = new AppointmentService(
            _mockAppointmentRepo.Object,
            _mockAvailabilityRepo.Object,
            _mockUserRepo.Object,
            _context,
            _mockLogger.Object
        );
    }

    // Test 1: CREATE - Positive Test
    [Fact]
    public async Task CreateAppointment_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var client = new Client { UserId = 1, Name = "John Doe", Role = UserRole.Client, PasswordHash = "hash", Email = "john@test.com" };
        var caregiver = new Caregiver { UserId = 2, Name = "Jane Smith", Role = UserRole.Caregiver, PasswordHash = "hash", Email = "jane@test.com" };
        var availability = new Availability
        {
            AvailabilityId = 1,
            CaregiverId = 2,
            Date = DateTime.Now.AddDays(1).Date,
            StartTime = "10:00",
            EndTime = "11:00"
        };

        _context.Users.AddRange(client, caregiver);
        await _context.SaveChangesAsync();

        var dto = new CreateAppointmentDto
        {
            AvailabilityId = 1,
            ClientId = 1,
            Task = AppointmentTask.Companionship
        };

        _mockAvailabilityRepo.Setup(x => x.GetAvailabilityById(1))
            .ReturnsAsync(availability);
        _mockAppointmentRepo.Setup(x => x.CreateAppointment(It.IsAny<Appointment>()))
            .ReturnsAsync(true);
        _mockAvailabilityRepo.Setup(x => x.DeleteAvailability(1))
            .ReturnsAsync(true);

        // Act
        var result = await _service.CreateAppointment(dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Appointment created successfully");
        result.Appointment.Should().NotBeNull();
        result.Appointment!.ClientId.Should().Be(1);
        result.Appointment.CaregiverId.Should().Be(2);
        _mockAppointmentRepo.Verify(x => x.CreateAppointment(It.IsAny<Appointment>()), Times.Once);
        _mockAvailabilityRepo.Verify(x => x.DeleteAvailability(1), Times.Once);
    }

    // Test 2: CREATE - Negative Test (Availability Not Found)
    [Fact]
    public async Task CreateAppointment_WithInvalidAvailability_ShouldReturnFailure()
    {
        // Arrange
        var dto = new CreateAppointmentDto
        {
            AvailabilityId = 999,
            ClientId = 1,
            Task = AppointmentTask.Companionship
        };

        _mockAvailabilityRepo.Setup(x => x.GetAvailabilityById(999))
            .ReturnsAsync((Availability?)null);

        // Act
        var result = await _service.CreateAppointment(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Selected availability slot not found");
        result.Appointment.Should().BeNull();
        _mockAppointmentRepo.Verify(x => x.CreateAppointment(It.IsAny<Appointment>()), Times.Never);
    }

    // Test 3: CREATE - Negative Test (Client Not Found)
    [Fact]
    public async Task CreateAppointment_WithInvalidClient_ShouldReturnFailure()
    {
        // Arrange
        var caregiver = new Caregiver { UserId = 2, Name = "Jane Smith", Role = UserRole.Caregiver, PasswordHash = "hash", Email = "jane@test.com" };
        var availability = new Availability
        {
            AvailabilityId = 1,
            CaregiverId = 2,
            Date = DateTime.Now.AddDays(1).Date,
            StartTime = "10:00",
            EndTime = "11:00"
        };

        _context.Users.Add(caregiver);
        await _context.SaveChangesAsync();

        var dto = new CreateAppointmentDto
        {
            AvailabilityId = 1,
            ClientId = 999, // Non-existent client
            Task = AppointmentTask.Companionship
        };

        _mockAvailabilityRepo.Setup(x => x.GetAvailabilityById(1))
            .ReturnsAsync(availability);

        // Act
        var result = await _service.CreateAppointment(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Client not found");
        result.Appointment.Should().BeNull();
    }

    // Test 4: CREATE - Negative Test (Slot Already Booked)
    [Fact]
    public async Task CreateAppointment_WithAlreadyBookedSlot_ShouldReturnFailure()
    {
        // Arrange
        var client = new Client { UserId = 1, Name = "John Doe", Role = UserRole.Client, PasswordHash = "hash", Email = "john@test.com" };
        var caregiver = new Caregiver { UserId = 2, Name = "Jane Smith", Role = UserRole.Caregiver, PasswordHash = "hash", Email = "jane@test.com" };
        var appointmentDate = DateTime.Now.AddDays(1).Date.AddHours(10);
        
        var availability = new Availability
        {
            AvailabilityId = 1,
            CaregiverId = 2,
            Date = DateTime.Now.AddDays(1).Date,
            StartTime = "10:00",
            EndTime = "11:00"
        };

        var existingAppointment = new Appointment
        {
            AppointmentId = 1,
            CaregiverId = 2,
            ClientId = 1,
            Date = appointmentDate,
            Task = AppointmentTask.Shopping
        };

        _context.Users.AddRange(client, caregiver);
        _context.Appointments.Add(existingAppointment);
        await _context.SaveChangesAsync();

        var dto = new CreateAppointmentDto
        {
            AvailabilityId = 1,
            ClientId = 1,
            Task = AppointmentTask.Companionship
        };

        _mockAvailabilityRepo.Setup(x => x.GetAvailabilityById(1))
            .ReturnsAsync(availability);

        // Act
        var result = await _service.CreateAppointment(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Selected time slot is already booked");
        result.Appointment.Should().BeNull();
    }

    // Test 5: READ - Get All Appointments (Positive)
    [Fact]
    public async Task GetAllAppointments_ShouldReturnAllAppointments()
    {
        // Arrange
        var appointments = new List<Appointment>
        {
            new Appointment { AppointmentId = 1, Date = DateTime.Now.AddDays(1), CaregiverId = 1, ClientId = 2, Task = AppointmentTask.Shopping },
            new Appointment { AppointmentId = 2, Date = DateTime.Now.AddDays(2), CaregiverId = 1, ClientId = 3, Task = AppointmentTask.Companionship }
        };

        _mockAppointmentRepo.Setup(x => x.GetAll())
            .ReturnsAsync(appointments);

        // Act
        var result = await _service.GetAllAppointments();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        _mockAppointmentRepo.Verify(x => x.GetAll(), Times.Once);
    }

    // Test 6: READ - Get Appointment by ID (Positive)
    [Fact]
    public async Task GetAppointmentById_WithValidId_ShouldReturnAppointment()
    {
        // Arrange
        var appointment = new Appointment
        {
            AppointmentId = 1,
            Date = DateTime.Now.AddDays(1),
            CaregiverId = 1,
            ClientId = 2,
            Task = AppointmentTask.MealPreparation
        };

        _mockAppointmentRepo.Setup(x => x.GetAppointmentById(1))
            .ReturnsAsync(appointment);

        // Act
        var result = await _service.GetAppointmentById(1);

        // Assert
        result.Should().NotBeNull();
        result!.AppointmentId.Should().Be(1);
        result.Task.Should().Be(AppointmentTask.MealPreparation);
    }

    // Test 7: UPDATE - Positive Test
    [Fact]
    public async Task UpdateAppointment_WithValidData_ShouldReturnSuccess()
    {
        // Arrange
        var existingAppointment = new Appointment
        {
            AppointmentId = 1,
            Date = DateTime.Now.AddDays(1),
            CaregiverId = 2,
            ClientId = 1,
            Task = AppointmentTask.Shopping
        };

        var updateDto = new UpdateAppointmentDto
        {
            Date = DateTime.Now.AddDays(2),
            Task = AppointmentTask.MealPreparation
        };

        _mockAppointmentRepo.Setup(x => x.GetAppointmentById(1))
            .ReturnsAsync(existingAppointment);
        _mockAppointmentRepo.Setup(x => x.UpdateAppointment(It.IsAny<Appointment>()))
            .ReturnsAsync(true);

        // Act
        var result = await _service.UpdateAppointment(1, updateDto);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Appointment updated successfully");
        _mockAppointmentRepo.Verify(x => x.UpdateAppointment(It.Is<Appointment>(a =>
            a.AppointmentId == 1 &&
            a.Date == updateDto.Date &&
            a.Task == updateDto.Task
        )), Times.Once);
    }

    // Test 8: UPDATE - Negative Test (Appointment Not Found)
    [Fact]
    public async Task UpdateAppointment_WithInvalidId_ShouldReturnFailure()
    {
        // Arrange
        var updateDto = new UpdateAppointmentDto
        {
            Date = DateTime.Now.AddDays(2),
            Task = AppointmentTask.MealPreparation
        };

        _mockAppointmentRepo.Setup(x => x.GetAppointmentById(999))
            .ReturnsAsync((Appointment?)null);

        // Act
        var result = await _service.UpdateAppointment(999, updateDto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Appointment not found");
        _mockAppointmentRepo.Verify(x => x.UpdateAppointment(It.IsAny<Appointment>()), Times.Never);
    }

    // Test 9: UPDATE - Negative Test (Past Date)
    [Fact]
    public async Task UpdateAppointment_WithPastDate_ShouldReturnFailure()
    {
        // Arrange
        var existingAppointment = new Appointment
        {
            AppointmentId = 1,
            Date = DateTime.Now.AddDays(1),
            CaregiverId = 2,
            ClientId = 1,
            Task = AppointmentTask.Shopping
        };

        var updateDto = new UpdateAppointmentDto
        {
            Date = DateTime.Now.AddDays(-1), // Past date
            Task = AppointmentTask.MealPreparation
        };

        _mockAppointmentRepo.Setup(x => x.GetAppointmentById(1))
            .ReturnsAsync(existingAppointment);

        // Act
        var result = await _service.UpdateAppointment(1, updateDto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Appointment date must be in the future");
        _mockAppointmentRepo.Verify(x => x.UpdateAppointment(It.IsAny<Appointment>()), Times.Never);
    }

    // Test 10: DELETE - Positive Test
    [Fact]
    public async Task DeleteAppointment_WithValidId_ShouldReturnSuccess()
    {
        // Arrange
        var appointment = new Appointment
        {
            AppointmentId = 1,
            Date = DateTime.Now.AddDays(1),
            CaregiverId = 2,
            ClientId = 1,
            Task = AppointmentTask.Companionship
        };

        _mockAppointmentRepo.Setup(x => x.GetAppointmentById(1))
            .ReturnsAsync(appointment);
        _mockAppointmentRepo.Setup(x => x.DeleteAppointment(1))
            .ReturnsAsync(true);

        // Act
        var result = await _service.DeleteAppointment(1);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Appointment deleted successfully");
        _mockAppointmentRepo.Verify(x => x.DeleteAppointment(1), Times.Once);
    }

    // Test 11: DELETE - Negative Test (Appointment Not Found)
    [Fact]
    public async Task DeleteAppointment_WithInvalidId_ShouldReturnFailure()
    {
        // Arrange
        _mockAppointmentRepo.Setup(x => x.GetAppointmentById(999))
            .ReturnsAsync((Appointment?)null);

        // Act
        var result = await _service.DeleteAppointment(999);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Appointment not found");
        _mockAppointmentRepo.Verify(x => x.DeleteAppointment(It.IsAny<int>()), Times.Never);
    }

    // Test 12: IsAppointmentSlotAvailable - Check Availability
    [Fact]
    public async Task IsAppointmentSlotAvailable_WithAvailableSlot_ShouldReturnTrue()
    {
        // Arrange
        var dateTime = DateTime.Now.AddDays(1);
        // No appointments exist for this slot

        // Act
        var result = await _service.IsAppointmentSlotAvailable(1, dateTime);

        // Assert
        result.Should().BeTrue();
    }

    // Test 13: IsAppointmentSlotAvailable - Slot Taken
    [Fact]
    public async Task IsAppointmentSlotAvailable_WithBookedSlot_ShouldReturnFalse()
    {
        // Arrange
        var dateTime = DateTime.Now.AddDays(1);
        var appointment = new Appointment
        {
            AppointmentId = 1,
            CaregiverId = 1,
            ClientId = 2,
            Date = dateTime,
            Task = AppointmentTask.Shopping
        };

        _context.Appointments.Add(appointment);
        await _context.SaveChangesAsync();

        // Act
        var result = await _service.IsAppointmentSlotAvailable(1, dateTime);

        // Assert
        result.Should().BeFalse();
    }
}
