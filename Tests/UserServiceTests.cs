using app_ointment_backend.DAL;
using app_ointment_backend.Models;
using app_ointment_backend.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Tests;

public class UserServiceTests
{
    private readonly Mock<IUserRepository> _mockUserRepository;
    private readonly Mock<ILogger<UserService>> _mockLogger;
    private readonly UserService _userService;

    public UserServiceTests()
    {
        _mockUserRepository = new Mock<IUserRepository>();
        _mockLogger = new Mock<ILogger<UserService>>();
        _userService = new UserService(_mockUserRepository.Object, _mockLogger.Object);
    }

    #region CREATE Tests

    [Fact]
    public async Task CreateClient_WithValidData_ShouldSucceed()
    {
        // Arrange
        var dto = new CreateUserDto
        {
            Name = "John Doe",
            Email = "john@example.com",
            Password = "password123",
            Adress = "123 Main St",
            Phone = "555-1234",
            ImageUrl = "https://example.com/image.jpg"
        };

        _mockUserRepository.Setup(r => r.GetUserByEmail(dto.Email))
            .ReturnsAsync((User?)null);
        _mockUserRepository.Setup(r => r.CreateUser(It.IsAny<User>()))
            .ReturnsAsync(true);

        // Act
        var result = await _userService.CreateClient(dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Client created successfully");
        result.User.Should().NotBeNull();
        result.User!.Email.Should().Be(dto.Email);
        result.User.Role.Should().Be(UserRole.Client);
        _mockUserRepository.Verify(r => r.CreateUser(It.IsAny<Client>()), Times.Once);
    }

    [Fact]
    public async Task CreateClient_WithExistingEmail_ShouldFail()
    {
        // Arrange
        var email = "existing@example.com";
        var dto = new CreateUserDto
        {
            Name = "John Doe",
            Email = email,
            Password = "password123",
            Adress = "123 Main St",
            Phone = "555-1234"
        };

        var existingUser = new Client { UserId = 1, Email = email, Name = "Existing User", PasswordHash = "hashedPassword" };
        _mockUserRepository.Setup(r => r.GetUserByEmail(email))
            .ReturnsAsync(existingUser);

        // Act
        var result = await _userService.CreateClient(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Email already registered");
        result.User.Should().BeNull();
        _mockUserRepository.Verify(r => r.CreateUser(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task CreateCaregiver_WithValidData_ShouldSucceed()
    {
        // Arrange
        var dto = new CreateUserDto
        {
            Name = "Jane Smith",
            Email = "jane@example.com",
            Password = "password123",
            Adress = "456 Oak Ave",
            Phone = "555-5678"
        };

        _mockUserRepository.Setup(r => r.GetUserByEmail(dto.Email))
            .ReturnsAsync((User?)null);
        _mockUserRepository.Setup(r => r.CreateUser(It.IsAny<User>()))
            .ReturnsAsync(true);

        // Act
        var result = await _userService.CreateCaregiver(dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Caregiver created successfully");
        result.User.Should().NotBeNull();
        result.User!.Role.Should().Be(UserRole.Caregiver);
        _mockUserRepository.Verify(r => r.CreateUser(It.IsAny<Caregiver>()), Times.Once);
    }

    [Fact]
    public async Task CreateAdmin_WithValidData_ShouldSucceed()
    {
        // Arrange
        var dto = new CreateUserDto
        {
            Name = "Admin User",
            Email = "admin@example.com",
            Password = "password123",
            Adress = "789 Admin Blvd",
            Phone = "555-9999"
        };

        _mockUserRepository.Setup(r => r.GetUserByEmail(dto.Email))
            .ReturnsAsync((User?)null);
        _mockUserRepository.Setup(r => r.CreateUser(It.IsAny<User>()))
            .ReturnsAsync(true);

        // Act
        var result = await _userService.CreateAdmin(dto);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Admin created successfully");
        result.User.Should().NotBeNull();
        result.User!.Role.Should().Be(UserRole.Admin);
        _mockUserRepository.Verify(r => r.CreateUser(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task CreateUser_WhenRepositoryFails_ShouldReturnFailure()
    {
        // Arrange
        var dto = new CreateUserDto
        {
            Name = "John Doe",
            Email = "john@example.com",
            Password = "password123",
            Adress = "123 Main St",
            Phone = "555-1234"
        };

        _mockUserRepository.Setup(r => r.GetUserByEmail(dto.Email))
            .ReturnsAsync((User?)null);
        _mockUserRepository.Setup(r => r.CreateUser(It.IsAny<User>()))
            .ReturnsAsync(false);

        // Act
        var result = await _userService.CreateClient(dto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Failed to create client");
        result.User.Should().BeNull();
    }

    #endregion

    #region READ Tests

    [Fact]
    public async Task GetUserById_WhenUserExists_ShouldReturnUser()
    {
        // Arrange
        var userId = 1;
        var user = new Client { UserId = userId, Name = "John Doe", Email = "john@example.com", PasswordHash = "hashedPassword" };
        _mockUserRepository.Setup(r => r.GetUserById(userId))
            .ReturnsAsync(user);

        // Act
        var result = await _userService.GetUserById(userId);

        // Assert
        result.Should().NotBeNull();
        result!.UserId.Should().Be(userId);
        result.Name.Should().Be("John Doe");
    }

    [Fact]
    public async Task GetUserById_WhenUserDoesNotExist_ShouldReturnNull()
    {
        // Arrange
        var userId = 999;
        _mockUserRepository.Setup(r => r.GetUserById(userId))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _userService.GetUserById(userId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetUserByEmail_WhenUserExists_ShouldReturnUser()
    {
        // Arrange
        var email = "john@example.com";
        var user = new Client { UserId = 1, Name = "John Doe", Email = email, PasswordHash = "hashedPassword" };
        _mockUserRepository.Setup(r => r.GetUserByEmail(email))
            .ReturnsAsync(user);

        // Act
        var result = await _userService.GetUserByEmail(email);

        // Assert
        result.Should().NotBeNull();
        result!.Email.Should().Be(email);
    }

    [Fact]
    public async Task GetAllUsers_ShouldReturnAllUsers()
    {
        // Arrange
        var users = new List<User>
        {
            new Client { UserId = 1, Name = "John Doe", Email = "john@example.com", PasswordHash = "hashedPassword" },
            new Caregiver { UserId = 2, Name = "Jane Smith", Email = "jane@example.com", PasswordHash = "hashedPassword" },
            new User { UserId = 3, Name = "Admin", Email = "admin@example.com", Role = UserRole.Admin, PasswordHash = "hashedPassword" }
        };
        _mockUserRepository.Setup(r => r.GetAll())
            .ReturnsAsync(users);

        // Act
        var result = await _userService.GetAllUsers();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetCaregivers_ShouldReturnOnlyCaregivers()
    {
        // Arrange
        var caregivers = new List<Caregiver>
        {
            new Caregiver { UserId = 1, Name = "Jane Smith", Email = "jane@example.com", PasswordHash = "hashedPassword" },
            new Caregiver { UserId = 2, Name = "Bob Johnson", Email = "bob@example.com", PasswordHash = "hashedPassword" }
        };
        _mockUserRepository.Setup(r => r.GetCaregivers())
            .ReturnsAsync(caregivers);

        // Act
        var result = await _userService.GetCaregivers();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().AllBeOfType<Caregiver>();
    }

    [Fact]
    public async Task GetClients_ShouldReturnOnlyClients()
    {
        // Arrange
        var clients = new List<Client>
        {
            new Client { UserId = 1, Name = "John Doe", Email = "john@example.com", PasswordHash = "hashedPassword" },
            new Client { UserId = 2, Name = "Alice Brown", Email = "alice@example.com", PasswordHash = "hashedPassword" }
        };
        _mockUserRepository.Setup(r => r.GetClients())
            .ReturnsAsync(clients);

        // Act
        var result = await _userService.GetClients();

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(2);
        result.Should().AllBeOfType<Client>();
    }

    #endregion

    #region UPDATE Tests

    [Fact]
    public async Task UpdateUser_WithValidData_ShouldSucceed()
    {
        // Arrange
        var userId = 1;
        var user = new Client
        {
            UserId = userId,
            Name = "John Doe",
            Email = "john@example.com",
            Adress = "Old Address",
            Phone = "555-0000",
            PasswordHash = "hashedPassword"
        };

        var updateDto = new UpdateUserDto
        {
            Name = "John Updated",
            Adress = "New Address",
            Phone = "555-1111",
            ImageUrl = "https://example.com/new.jpg"
        };

        _mockUserRepository.Setup(r => r.GetUserById(userId))
            .ReturnsAsync(user);
        _mockUserRepository.Setup(r => r.UpdateUser(It.IsAny<User>()))
            .ReturnsAsync(true);

        // Act
        var result = await _userService.UpdateUser(userId, updateDto);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("User updated successfully");
        user.Name.Should().Be(updateDto.Name);
        user.Adress.Should().Be(updateDto.Adress);
        user.Phone.Should().Be(updateDto.Phone);
        _mockUserRepository.Verify(r => r.UpdateUser(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task UpdateUser_WhenUserNotFound_ShouldFail()
    {
        // Arrange
        var userId = 999;
        var updateDto = new UpdateUserDto
        {
            Name = "Updated Name",
            Adress = "New Address",
            Phone = "555-1111"
        };

        _mockUserRepository.Setup(r => r.GetUserById(userId))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _userService.UpdateUser(userId, updateDto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("User not found");
        _mockUserRepository.Verify(r => r.UpdateUser(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task UpdateUser_WhenRepositoryFails_ShouldReturnFailure()
    {
        // Arrange
        var userId = 1;
        var user = new Client { UserId = userId, Name = "John Doe", Email = "john@example.com", PasswordHash = "hashedPassword" };
        var updateDto = new UpdateUserDto
        {
            Name = "Updated Name",
            Adress = "New Address",
            Phone = "555-1111"
        };

        _mockUserRepository.Setup(r => r.GetUserById(userId))
            .ReturnsAsync(user);
        _mockUserRepository.Setup(r => r.UpdateUser(It.IsAny<User>()))
            .ReturnsAsync(false);

        // Act
        var result = await _userService.UpdateUser(userId, updateDto);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Failed to update user");
    }

    [Fact]
    public async Task ChangePassword_WithCorrectOldPassword_ShouldSucceed()
    {
        // Arrange
        var userId = 1;
        var oldPassword = "oldPassword123";
        var newPassword = "newPassword456";
        var hashedOldPassword = BCrypt.Net.BCrypt.HashPassword(oldPassword);

        var user = new Client
        {
            UserId = userId,
            Name = "John Doe",
            Email = "john@example.com",
            PasswordHash = hashedOldPassword
        };

        _mockUserRepository.Setup(r => r.GetUserById(userId))
            .ReturnsAsync(user);
        _mockUserRepository.Setup(r => r.UpdateUser(It.IsAny<User>()))
            .ReturnsAsync(true);

        // Act
        var result = await _userService.ChangePassword(userId, oldPassword, newPassword);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Password changed successfully");
        BCrypt.Net.BCrypt.Verify(newPassword, user.PasswordHash).Should().BeTrue();
        _mockUserRepository.Verify(r => r.UpdateUser(It.IsAny<User>()), Times.Once);
    }

    [Fact]
    public async Task ChangePassword_WithIncorrectOldPassword_ShouldFail()
    {
        // Arrange
        var userId = 1;
        var oldPassword = "wrongPassword";
        var newPassword = "newPassword456";
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword("correctPassword");

        var user = new Client
        {
            UserId = userId,
            Name = "John Doe",
            Email = "john@example.com",
            PasswordHash = hashedPassword
        };

        _mockUserRepository.Setup(r => r.GetUserById(userId))
            .ReturnsAsync(user);

        // Act
        var result = await _userService.ChangePassword(userId, oldPassword, newPassword);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Current password is incorrect");
        _mockUserRepository.Verify(r => r.UpdateUser(It.IsAny<User>()), Times.Never);
    }

    [Fact]
    public async Task ChangePassword_WhenUserNotFound_ShouldFail()
    {
        // Arrange
        var userId = 999;
        _mockUserRepository.Setup(r => r.GetUserById(userId))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _userService.ChangePassword(userId, "oldPass", "newPass");

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("User not found");
    }

    #endregion

    #region DELETE Tests

    [Fact]
    public async Task DeleteUser_WhenUserExists_ShouldSucceed()
    {
        // Arrange
        var userId = 1;
        var user = new Client { UserId = userId, Name = "John Doe", Email = "john@example.com", PasswordHash = "hashedPassword" };

        _mockUserRepository.Setup(r => r.GetUserById(userId))
            .ReturnsAsync(user);
        _mockUserRepository.Setup(r => r.DeleteUser(userId))
            .ReturnsAsync(true);

        // Act
        var result = await _userService.DeleteUser(userId);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("User deleted successfully");
        _mockUserRepository.Verify(r => r.DeleteUser(userId), Times.Once);
    }

    [Fact]
    public async Task DeleteUser_WhenUserNotFound_ShouldFail()
    {
        // Arrange
        var userId = 999;
        _mockUserRepository.Setup(r => r.GetUserById(userId))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _userService.DeleteUser(userId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("User not found");
        _mockUserRepository.Verify(r => r.DeleteUser(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DeleteUser_WhenRepositoryFails_ShouldReturnFailure()
    {
        // Arrange
        var userId = 1;
        var user = new Client { UserId = userId, Name = "John Doe", Email = "john@example.com", PasswordHash = "hashedPassword" };

        _mockUserRepository.Setup(r => r.GetUserById(userId))
            .ReturnsAsync(user);
        _mockUserRepository.Setup(r => r.DeleteUser(userId))
            .ReturnsAsync(false);

        // Act
        var result = await _userService.DeleteUser(userId);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Be("Failed to delete user");
    }

    #endregion

    #region VALIDATION Tests

    [Fact]
    public async Task ValidatePassword_WithCorrectPassword_ShouldReturnTrue()
    {
        // Arrange
        var userId = 1;
        var password = "correctPassword123";
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(password);

        var user = new Client
        {
            UserId = userId,
            Name = "John Doe",
            Email = "john@example.com",
            PasswordHash = hashedPassword
        };

        _mockUserRepository.Setup(r => r.GetUserById(userId))
            .ReturnsAsync(user);

        // Act
        var result = await _userService.ValidatePassword(userId, password);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task ValidatePassword_WithIncorrectPassword_ShouldReturnFalse()
    {
        // Arrange
        var userId = 1;
        var correctPassword = "correctPassword123";
        var wrongPassword = "wrongPassword";
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(correctPassword);

        var user = new Client
        {
            UserId = userId,
            Name = "John Doe",
            Email = "john@example.com",
            PasswordHash = hashedPassword
        };

        _mockUserRepository.Setup(r => r.GetUserById(userId))
            .ReturnsAsync(user);

        // Act
        var result = await _userService.ValidatePassword(userId, wrongPassword);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public async Task ValidatePassword_WhenUserNotFound_ShouldReturnFalse()
    {
        // Arrange
        var userId = 999;
        _mockUserRepository.Setup(r => r.GetUserById(userId))
            .ReturnsAsync((User?)null);

        // Act
        var result = await _userService.ValidatePassword(userId, "anyPassword");

        // Assert
        result.Should().BeFalse();
    }

    #endregion
}
