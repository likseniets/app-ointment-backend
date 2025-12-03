using app_ointment_backend.DAL;
using app_ointment_backend.Models;
using app_ointment_backend.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests;

public class AvailabilityServiceTests
{
    private readonly Mock<IAvailabilityRepository> _mockAvailabilityRepository;
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<ILogger<AvailabilityService>> _mockLogger;
    private readonly AvailabilityService _availabilityService;

    public AvailabilityServiceTests()
    {
        _mockAvailabilityRepository = new Mock<IAvailabilityRepository>();
        _mockUserRepository = new Mock<IUserRepository>();
        _mockLogger = new Mock<ILogger<AvailabilityService>>();
        _availabilityService = new AvailabilityService(
            _mockAvailabilityRepository.Object,
            _mockUserRepository.Object,
            _mockLogger.Object);
    }

    #region CREATE Tests

    [Fact]
    public async Task CreateAvailability_WithValidData_ShouldSucceed()
    {
        // Arrange
        var caregiverId = 1;
        var dto = new CreateAvailabilityDto
        {
            CaregiverId = caregiverId,
            Date = DateTime.Today.AddDays(1),
            StartTime = "09:00",
            EndTime = "10:00",
            SlotLengthMinutes = 60
        };

        var caregiver = new Caregiver
        {
            UserId = caregiverId,
            Name = "Jane Smith",
            Email = "jane@example.com",
            Role = UserRole.Caregiver,
            PasswordHash = "hashedPassword"
        };

        _mockUserRepository.Setup(r => r.GetUserById(caregiverId))
            .ReturnsAsync(caregiver);
        _mockAvailabilityRepository.Setup(r => r.AvailabilityExists(
                caregiverId, dto.Date, dto.StartTime, dto.EndTime))
            .ReturnsAsync(false);
        _mockAvailabilityRepository.Setup(r => r.CreateAvailability(It.IsAny<Availability>()))
            .ReturnsAsync(true);

        // Act
        var result = await _availabilityService.CreateAvailability(dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Availability created successfully");
        _mockAvailabilityRepository.Verify(r => r.CreateAvailability(It.IsAny<Availability>()), Times.Once);
    }

    [Fact]
    public async Task CreateAvailability_WithNonExistentCaregiver_ShouldFail()
    {
        // Arrange
        var dto = new CreateAvailabilityDto
        {
            CaregiverId = 999,
            Date = DateTime.Today.AddDays(1),
            StartTime = "09:00",
            EndTime = "10:00",
            SlotLengthMinutes = 60
        };

        _mockUserRepository.Setup(r => r.GetUserById(dto.CaregiverId))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _availabilityService.CreateAvailability(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Caregiver not found");
        _mockAvailabilityRepository.Verify(r => r.CreateAvailability(It.IsAny<Availability>()), Times.Never);
    }

    [Fact]
    public async Task CreateAvailability_WithNonCaregiverUser_ShouldFail()
    {
        // Arrange
        var userId = 1;
        var dto = new CreateAvailabilityDto
        {
            CaregiverId = userId,
            Date = DateTime.Today.AddDays(1),
            StartTime = "09:00",
            EndTime = "10:00",
            SlotLengthMinutes = 60
        };

        var client = new Client
        {
            UserId = userId,
            Name = "John Doe",
            Email = "john@example.com",
            Role = UserRole.Client,
            PasswordHash = "hashedPassword"
        };

        _mockUserRepository.Setup(r => r.GetUserById(userId))
            .ReturnsAsync(client);

        // Act
        var result = await _availabilityService.CreateAvailability(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Caregiver not found");
    }

    [Fact]
    public async Task CreateAvailability_WithInvalidTimeFormat_ShouldFail()
    {
        // Arrange
        var caregiverId = 1;
        var dto = new CreateAvailabilityDto
        {
            CaregiverId = caregiverId,
            Date = DateTime.Today.AddDays(1),
            StartTime = "invalid",
            EndTime = "10:00",
            SlotLengthMinutes = 60
        };

        var caregiver = new Caregiver
        {
            UserId = caregiverId,
            Name = "Jane Smith",
            Email = "jane@example.com",
            Role = UserRole.Caregiver,
            PasswordHash = "hashedPassword"
        };

        _mockUserRepository.Setup(r => r.GetUserById(caregiverId))
            .ReturnsAsync(caregiver);

        // Act
        var result = await _availabilityService.CreateAvailability(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Invalid time format. Please use HH:mm");
    }

    [Fact]
    public async Task CreateAvailability_WithEndTimeBeforeStartTime_ShouldFail()
    {
        // Arrange
        var caregiverId = 1;
        var dto = new CreateAvailabilityDto
        {
            CaregiverId = caregiverId,
            Date = DateTime.Today.AddDays(1),
            StartTime = "10:00",
            EndTime = "09:00",
            SlotLengthMinutes = 60
        };

        var caregiver = new Caregiver
        {
            UserId = caregiverId,
            Name = "Jane Smith",
            Email = "jane@example.com",
            Role = UserRole.Caregiver,
            PasswordHash = "hashedPassword"
        };

        _mockUserRepository.Setup(r => r.GetUserById(caregiverId))
            .ReturnsAsync(caregiver);

        // Act
        var result = await _availabilityService.CreateAvailability(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("End time must be after start time");
    }

    [Fact]
    public async Task CreateAvailability_WhenAlreadyExists_ShouldFail()
    {
        // Arrange
        var caregiverId = 1;
        var dto = new CreateAvailabilityDto
        {
            CaregiverId = caregiverId,
            Date = DateTime.Today.AddDays(1),
            StartTime = "09:00",
            EndTime = "10:00",
            SlotLengthMinutes = 60
        };

        var caregiver = new Caregiver
        {
            UserId = caregiverId,
            Name = "Jane Smith",
            Email = "jane@example.com",
            Role = UserRole.Caregiver,
            PasswordHash = "hashedPassword"
        };

        _mockUserRepository.Setup(r => r.GetUserById(caregiverId))
            .ReturnsAsync(caregiver);
        _mockAvailabilityRepository.Setup(r => r.AvailabilityExists(
                caregiverId, dto.Date, dto.StartTime, dto.EndTime))
            .ReturnsAsync(true);

        // Act
        var result = await _availabilityService.CreateAvailability(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("This availability slot already exists");
        _mockAvailabilityRepository.Verify(r => r.CreateAvailability(It.IsAny<Availability>()), Times.Never);
    }

    [Fact]
    public async Task CreateAvailabilitySlots_WithValidData_ShouldCreateMultipleSlots()
    {
        // Arrange
        var caregiverId = 1;
        var dto = new CreateAvailabilityDto
        {
            CaregiverId = caregiverId,
            Date = DateTime.Today.AddDays(1),
            StartTime = "09:00",
            EndTime = "12:00",
            SlotLengthMinutes = 60
        };

        var caregiver = new Caregiver
        {
            UserId = caregiverId,
            Name = "Jane Smith",
            Email = "jane@example.com",
            Role = UserRole.Caregiver,
            PasswordHash = "hashedPassword"
        };

        _mockUserRepository.Setup(r => r.GetUserById(caregiverId))
            .ReturnsAsync(caregiver);
        _mockAvailabilityRepository.Setup(r => r.AvailabilityExists(
                It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(false);
        _mockAvailabilityRepository.Setup(r => r.CreateAvailability(It.IsAny<Availability>()))
            .ReturnsAsync(true);

        // Act
        var result = await _availabilityService.CreateAvailabilitySlots(dto);

        // Assert
        result.Success.Should().BeTrue();
        result.SlotsCreated.Should().Be(3); // 09:00-10:00, 10:00-11:00, 11:00-12:00
        result.Message.Should().Contain("3 slot(s)");
        _mockAvailabilityRepository.Verify(r => r.CreateAvailability(It.IsAny<Availability>()), Times.Exactly(3));
    }

    [Fact]
    public async Task CreateAvailabilitySlots_WithSomeExisting_ShouldOnlyCreateNew()
    {
        // Arrange
        var caregiverId = 1;
        var dto = new CreateAvailabilityDto
        {
            CaregiverId = caregiverId,
            Date = DateTime.Today.AddDays(1),
            StartTime = "09:00",
            EndTime = "11:00",
            SlotLengthMinutes = 60
        };

        var caregiver = new Caregiver
        {
            UserId = caregiverId,
            Name = "Jane Smith",
            Email = "jane@example.com",
            Role = UserRole.Caregiver,
            PasswordHash = "hashedPassword"
        };

        _mockUserRepository.Setup(r => r.GetUserById(caregiverId))
            .ReturnsAsync(caregiver);
        
        // First slot exists, second doesn't
        _mockAvailabilityRepository.SetupSequence(r => r.AvailabilityExists(
                It.IsAny<int>(), It.IsAny<DateTime>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(true)  // 09:00-10:00 exists
            .ReturnsAsync(false); // 10:00-11:00 doesn't exist
        
        _mockAvailabilityRepository.Setup(r => r.CreateAvailability(It.IsAny<Availability>()))
            .ReturnsAsync(true);

        // Act
        var result = await _availabilityService.CreateAvailabilitySlots(dto);

        // Assert
        result.Success.Should().BeTrue();
        result.SlotsCreated.Should().Be(1); // Only created 10:00-11:00
        _mockAvailabilityRepository.Verify(r => r.CreateAvailability(It.IsAny<Availability>()), Times.Once);
    }

    #endregion

    #region READ Tests

    [Fact]
    public async Task GetAllAvailabilities_ShouldReturnAllAvailabilities()
    {
        // Arrange
        var availabilities = new List<Availability>
        {
            new Availability { AvailabilityId = 1, CaregiverId = 1, Date = DateTime.Today, StartTime = "09:00", EndTime = "10:00" },
            new Availability { AvailabilityId = 2, CaregiverId = 2, Date = DateTime.Today, StartTime = "10:00", EndTime = "11:00" }
        };

        _mockAvailabilityRepository.Setup(r => r.GetAll())
            .ReturnsAsync(availabilities);

        // Act
        var result = await _availabilityService.GetAllAvailabilities();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetAvailabilitiesByCaregiver_ShouldReturnCaregiversAvailabilities()
    {
        // Arrange
        var caregiverId = 1;
        var availabilities = new List<Availability>
        {
            new Availability { AvailabilityId = 1, CaregiverId = caregiverId, Date = DateTime.Today, StartTime = "09:00", EndTime = "10:00" },
            new Availability { AvailabilityId = 2, CaregiverId = caregiverId, Date = DateTime.Today, StartTime = "10:00", EndTime = "11:00" }
        };

        _mockAvailabilityRepository.Setup(r => r.GetAvailabilityByCaregiver(caregiverId))
            .ReturnsAsync(availabilities);

        // Act
        var result = await _availabilityService.GetAvailabilitiesByCaregiver(caregiverId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().AllSatisfy(a => a.CaregiverId.Should().Be(caregiverId));
    }

    [Fact]
    public async Task GetAvailabilityById_WhenExists_ShouldReturnAvailability()
    {
        // Arrange
        var availabilityId = 1;
        var availability = new Availability
        {
            AvailabilityId = availabilityId,
            CaregiverId = 1,
            Date = DateTime.Today,
            StartTime = "09:00",
            EndTime = "10:00"
        };

        _mockAvailabilityRepository.Setup(r => r.GetAvailabilityById(availabilityId))
            .ReturnsAsync(availability);

        // Act
        var result = await _availabilityService.GetAvailabilityById(availabilityId);

        // Assert
        result.Should().NotBeNull();
        result!.AvailabilityId.Should().Be(availabilityId);
    }

    [Fact]
    public async Task GetAvailabilityById_WhenNotExists_ShouldReturnNull()
    {
        // Arrange
        var availabilityId = 999;
        _mockAvailabilityRepository.Setup(r => r.GetAvailabilityById(availabilityId))
            .ReturnsAsync((Availability?)null);

        // Act
        var result = await _availabilityService.GetAvailabilityById(availabilityId);

        // Assert
        result.Should().BeNull();
    }

    #endregion

    #region UPDATE Tests

    [Fact]
    public async Task UpdateAvailability_WithValidData_ShouldSucceed()
    {
        // Arrange
        var availabilityId = 1;
        var availability = new Availability
        {
            AvailabilityId = availabilityId,
            CaregiverId = 1,
            Date = DateTime.Today,
            StartTime = "09:00",
            EndTime = "10:00"
        };

        var updateDto = new UpdateAvailabilityDto
        {
            Date = DateTime.Today.AddDays(1),
            StartTime = "10:00",
            EndTime = "11:00"
        };

        _mockAvailabilityRepository.Setup(r => r.GetAvailabilityById(availabilityId))
            .ReturnsAsync(availability);
        _mockAvailabilityRepository.Setup(r => r.AvailabilityConflictExists(
                availabilityId, availability.CaregiverId, updateDto.Date, updateDto.StartTime, updateDto.EndTime))
            .ReturnsAsync(false);
        _mockAvailabilityRepository.Setup(r => r.UpdateAvailability(It.IsAny<Availability>()))
            .ReturnsAsync(true);

        // Act
        var result = await _availabilityService.UpdateAvailability(availabilityId, updateDto);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Availability updated successfully");
        availability.Date.Should().Be(updateDto.Date);
        availability.StartTime.Should().Be(updateDto.StartTime);
        availability.EndTime.Should().Be(updateDto.EndTime);
        _mockAvailabilityRepository.Verify(r => r.UpdateAvailability(It.IsAny<Availability>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAvailability_WhenNotFound_ShouldFail()
    {
        // Arrange
        var availabilityId = 999;
        var updateDto = new UpdateAvailabilityDto
        {
            Date = DateTime.Today.AddDays(1),
            StartTime = "10:00",
            EndTime = "11:00"
        };

        _mockAvailabilityRepository.Setup(r => r.GetAvailabilityById(availabilityId))
            .ReturnsAsync((Availability?)null);

        // Act
        var result = await _availabilityService.UpdateAvailability(availabilityId, updateDto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Availability not found");
        _mockAvailabilityRepository.Verify(r => r.UpdateAvailability(It.IsAny<Availability>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAvailability_WithInvalidTimeFormat_ShouldFail()
    {
        // Arrange
        var availabilityId = 1;
        var availability = new Availability
        {
            AvailabilityId = availabilityId,
            CaregiverId = 1,
            Date = DateTime.Today,
            StartTime = "09:00",
            EndTime = "10:00"
        };

        var updateDto = new UpdateAvailabilityDto
        {
            Date = DateTime.Today.AddDays(1),
            StartTime = "invalid",
            EndTime = "11:00"
        };

        _mockAvailabilityRepository.Setup(r => r.GetAvailabilityById(availabilityId))
            .ReturnsAsync(availability);

        // Act
        var result = await _availabilityService.UpdateAvailability(availabilityId, updateDto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Invalid time format. Please use HH:mm");
    }

    [Fact]
    public async Task UpdateAvailability_WithEndTimeBeforeStartTime_ShouldFail()
    {
        // Arrange
        var availabilityId = 1;
        var availability = new Availability
        {
            AvailabilityId = availabilityId,
            CaregiverId = 1,
            Date = DateTime.Today,
            StartTime = "09:00",
            EndTime = "10:00"
        };

        var updateDto = new UpdateAvailabilityDto
        {
            Date = DateTime.Today.AddDays(1),
            StartTime = "11:00",
            EndTime = "10:00"
        };

        _mockAvailabilityRepository.Setup(r => r.GetAvailabilityById(availabilityId))
            .ReturnsAsync(availability);

        // Act
        var result = await _availabilityService.UpdateAvailability(availabilityId, updateDto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("End time must be after start time");
    }

    [Fact]
    public async Task UpdateAvailability_WithConflict_ShouldFail()
    {
        // Arrange
        var availabilityId = 1;
        var availability = new Availability
        {
            AvailabilityId = availabilityId,
            CaregiverId = 1,
            Date = DateTime.Today,
            StartTime = "09:00",
            EndTime = "10:00"
        };

        var updateDto = new UpdateAvailabilityDto
        {
            Date = DateTime.Today.AddDays(1),
            StartTime = "10:00",
            EndTime = "11:00"
        };

        _mockAvailabilityRepository.Setup(r => r.GetAvailabilityById(availabilityId))
            .ReturnsAsync(availability);
        _mockAvailabilityRepository.Setup(r => r.AvailabilityConflictExists(
                availabilityId, availability.CaregiverId, updateDto.Date, updateDto.StartTime, updateDto.EndTime))
            .ReturnsAsync(true);

        // Act
        var result = await _availabilityService.UpdateAvailability(availabilityId, updateDto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("This availability slot conflicts with an existing one");
        _mockAvailabilityRepository.Verify(r => r.UpdateAvailability(It.IsAny<Availability>()), Times.Never);
    }

    [Fact]
    public async Task UpdateAvailability_WhenRepositoryFails_ShouldReturnFailure()
    {
        // Arrange
        var availabilityId = 1;
        var availability = new Availability
        {
            AvailabilityId = availabilityId,
            CaregiverId = 1,
            Date = DateTime.Today,
            StartTime = "09:00",
            EndTime = "10:00"
        };

        var updateDto = new UpdateAvailabilityDto
        {
            Date = DateTime.Today.AddDays(1),
            StartTime = "10:00",
            EndTime = "11:00"
        };

        _mockAvailabilityRepository.Setup(r => r.GetAvailabilityById(availabilityId))
            .ReturnsAsync(availability);
        _mockAvailabilityRepository.Setup(r => r.AvailabilityConflictExists(
                availabilityId, availability.CaregiverId, updateDto.Date, updateDto.StartTime, updateDto.EndTime))
            .ReturnsAsync(false);
        _mockAvailabilityRepository.Setup(r => r.UpdateAvailability(It.IsAny<Availability>()))
            .ReturnsAsync(false);

        // Act
        var result = await _availabilityService.UpdateAvailability(availabilityId, updateDto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Failed to update availability");
    }

    #endregion

    #region DELETE Tests

    [Fact]
    public async Task DeleteAvailability_WhenExists_ShouldSucceed()
    {
        // Arrange
        var availabilityId = 1;
        var availability = new Availability
        {
            AvailabilityId = availabilityId,
            CaregiverId = 1,
            Date = DateTime.Today,
            StartTime = "09:00",
            EndTime = "10:00"
        };

        _mockAvailabilityRepository.Setup(r => r.GetAvailabilityById(availabilityId))
            .ReturnsAsync(availability);
        _mockAvailabilityRepository.Setup(r => r.DeleteAvailability(availabilityId))
            .ReturnsAsync(true);

        // Act
        var result = await _availabilityService.DeleteAvailability(availabilityId);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Availability deleted successfully");
        _mockAvailabilityRepository.Verify(r => r.DeleteAvailability(availabilityId), Times.Once);
    }

    [Fact]
    public async Task DeleteAvailability_WhenNotFound_ShouldFail()
    {
        // Arrange
        var availabilityId = 999;
        _mockAvailabilityRepository.Setup(r => r.GetAvailabilityById(availabilityId))
            .ReturnsAsync((Availability?)null);

        // Act
        var result = await _availabilityService.DeleteAvailability(availabilityId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Availability not found");
        _mockAvailabilityRepository.Verify(r => r.DeleteAvailability(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAvailability_WhenRepositoryFails_ShouldReturnFailure()
    {
        // Arrange
        var availabilityId = 1;
        var availability = new Availability
        {
            AvailabilityId = availabilityId,
            CaregiverId = 1,
            Date = DateTime.Today,
            StartTime = "09:00",
            EndTime = "10:00"
        };

        _mockAvailabilityRepository.Setup(r => r.GetAvailabilityById(availabilityId))
            .ReturnsAsync(availability);
        _mockAvailabilityRepository.Setup(r => r.DeleteAvailability(availabilityId))
            .ReturnsAsync(false);

        // Act
        var result = await _availabilityService.DeleteAvailability(availabilityId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Failed to delete availability");
    }

    #endregion
}
