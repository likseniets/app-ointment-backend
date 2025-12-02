using app_ointment_backend.DAL;
using app_ointment_backend.Models;
using BCrypt.Net;

namespace app_ointment_backend.Services;

public class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly ILogger<UserService> _logger;

    public UserService(IUserRepository userRepository, ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task<User?> GetUserById(int userId)
    {
        return await _userRepository.GetUserById(userId);
    }

    public async Task<User?> GetUserByEmail(string email)
    {
        return await _userRepository.GetUserByEmail(email);
    }

    public async Task<IEnumerable<User>?> GetAllUsers()
    {
        return await _userRepository.GetAll();
    }

    public async Task<IEnumerable<User>?> GetCaregivers()
    {
        return await _userRepository.GetCaregivers();
    }

    public async Task<IEnumerable<User>?> GetClients()
    {
        return await _userRepository.GetClients();
    }

    // Create a new client user, this is the default registration method
    public async Task<(bool Success, string Message, User? User)> CreateClient(CreateUserDto dto)
    {
        try
        {
            // Check if email already exists
            var existingUser = await _userRepository.GetUserByEmail(dto.Email);
            if (existingUser != null)
            {
                _logger.LogWarning("[UserService] Email {Email} already exists", dto.Email);
                return (false, "Email already registered", null);
            }

            // Hash the password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            // Create client
            var newUser = new Client
            {
                Name = dto.Name,
                Role = UserRole.Client,
                Adress = dto.Adress,
                Phone = dto.Phone,
                Email = dto.Email,
                PasswordHash = passwordHash,
                ImageUrl = dto.ImageUrl ?? string.Empty
            };

            bool created = await _userRepository.CreateUser(newUser);
            if (created)
            {
                _logger.LogInformation("[UserService] Client {Email} registered successfully", newUser.Email);
                return (true, "Client created successfully", newUser);
            }

            return (false, "Failed to create client", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserService] Error creating client");
            return (false, "An error occurred while creating the client", null);
        }
    }

    // Create a new caregiver user, only admin can do this
    public async Task<(bool Success, string Message, User? User)> CreateCaregiver(CreateUserDto dto)
    {
        try
        {
            // Check if email already exists
            var existingUser = await _userRepository.GetUserByEmail(dto.Email);
            if (existingUser != null)
            {
                _logger.LogWarning("[UserService] Email {Email} already exists", dto.Email);
                return (false, "Email already registered", null);
            }

            // Hash the password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            // Create caregiver
            var newUser = new Caregiver
            {
                Name = dto.Name,
                Role = UserRole.Caregiver,
                Adress = dto.Adress,
                Phone = dto.Phone,
                Email = dto.Email,
                PasswordHash = passwordHash,
                ImageUrl = dto.ImageUrl ?? string.Empty
            };

            bool created = await _userRepository.CreateUser(newUser);
            if (created)
            {
                _logger.LogInformation("[UserService] Caregiver {Email} created successfully", newUser.Email);
                return (true, "Caregiver created successfully", newUser);
            }

            return (false, "Failed to create caregiver", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserService] Error creating caregiver");
            return (false, "An error occurred while creating the caregiver", null);
        }
    }

    // Create a new admin user, only admin can do this
    public async Task<(bool Success, string Message, User? User)> CreateAdmin(CreateUserDto dto)
    {
        try
        {
            // Check if email already exists
            var existingUser = await _userRepository.GetUserByEmail(dto.Email);
            if (existingUser != null)
            {
                _logger.LogWarning("[UserService] Email {Email} already exists", dto.Email);
                return (false, "Email already registered", null);
            }

            // Hash the password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

            // Create admin
            var newUser = new User
            {
                Name = dto.Name,
                Role = UserRole.Admin,
                Adress = dto.Adress,
                Phone = dto.Phone,
                Email = dto.Email,
                PasswordHash = passwordHash,
                ImageUrl = dto.ImageUrl ?? string.Empty
            };

            bool created = await _userRepository.CreateUser(newUser);
            if (created)
            {
                _logger.LogInformation("[UserService] Admin {Email} created successfully", newUser.Email);
                return (true, "Admin created successfully", newUser);
            }

            return (false, "Failed to create admin", null);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserService] Error creating admin");
            return (false, "An error occurred while creating the admin", null);
        }
    }

    // Update an existing user
    public async Task<(bool Success, string Message)> UpdateUser(int userId, UpdateUserDto dto)
    {
        try
        {
            var user = await _userRepository.GetUserById(userId);
            if (user == null)
            {
                return (false, "User not found");
            }

            user.Name = dto.Name;
            user.Adress = dto.Adress;
            user.Phone = dto.Phone;
            user.ImageUrl = dto.ImageUrl ?? user.ImageUrl;

            bool updated = await _userRepository.UpdateUser(user);
            if (updated)
            {
                _logger.LogInformation("[UserService] User {UserId} updated successfully", userId);
                return (true, "User updated successfully");
            }

            return (false, "Failed to update user");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserService] Error updating user {UserId}", userId);
            return (false, "An error occurred while updating the user");
        }
    }

    // Change the password for an existing user
    public async Task<(bool Success, string Message)> ChangePassword(int userId, string oldPassword, string newPassword)
    {
        try
        {
            var user = await _userRepository.GetUserById(userId);
            if (user == null)
            {
                return (false, "User not found");
            }

            // Verify old password
            if (!BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash))
            {
                _logger.LogWarning("[UserService] Invalid old password for user {UserId}", userId);
                return (false, "Current password is incorrect");
            }

            // Hash and update new password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);

            bool updated = await _userRepository.UpdateUser(user);
            if (updated)
            {
                _logger.LogInformation("[UserService] Password changed successfully for user {UserId}", userId);
                return (true, "Password changed successfully");
            }

            return (false, "Failed to change password");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserService] Error changing password for user {UserId}", userId);
            return (false, "An error occurred while changing the password");
        }
    }

    // Delete an existing user
    public async Task<(bool Success, string Message)> DeleteUser(int userId)
    {
        try
        {
            var user = await _userRepository.GetUserById(userId);
            if (user == null)
            {
                return (false, "User not found");
            }

            bool deleted = await _userRepository.DeleteUser(userId);
            if (deleted)
            {
                _logger.LogInformation("[UserService] User {UserId} deleted successfully", userId);
                return (true, "User deleted successfully");
            }

            return (false, "Failed to delete user");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserService] Error deleting user {UserId}", userId);
            return (false, "An error occurred while deleting the user");
        }
    }

    // Validate user password
    public async Task<bool> ValidatePassword(int userId, string password)
    {
        try
        {
            var user = await _userRepository.GetUserById(userId);
            if (user == null)
            {
                return false;
            }

            return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "[UserService] Error validating password for user {UserId}", userId);
            return false;
        }
    }
}
