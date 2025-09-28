using System.Security.Cryptography;
using System.Text;
using CashApp.Models;
using Microsoft.Extensions.Logging;
using Serilog;

namespace CashApp.Services
{
    public class AuthService
    {
        private readonly DatabaseService _databaseService;
        private readonly ILogger<AuthService> _logger;
        private User? _currentUser;

        public AuthService(DatabaseService databaseService)
        {
            _databaseService = databaseService;
            _logger = LoggerFactory.Create(builder =>
            {
                builder.AddSerilog();
            }).CreateLogger<AuthService>();
        }

        public User? CurrentUser => _currentUser;

        public bool IsAuthenticated => _currentUser != null;

        public bool IsAdmin => _currentUser?.IsAdmin ?? false;

        public bool CanManageProducts => _currentUser?.CanManageProducts ?? false;

        public bool CanViewStatistics => _currentUser?.CanViewStatistics ?? false;

        public bool CanManageUsers => _currentUser?.CanManageUsers ?? false;

        public async Task<bool> LoginAsync(string username, string password)
        {
            try
            {
                using var context = _databaseService.GetContext();

                var user = await context.Users
                    .FirstOrDefaultAsync(u => u.Username == username && u.IsActive);

                if (user == null)
                {
                    await _databaseService.LogActivityAsync(0, AuditAction.LoginFailed,
                        $"Login failed for user: {username}", AuditLogLevel.Warning);
                    return false;
                }

                if (!VerifyPassword(password, user.PasswordHash))
                {
                    await _databaseService.LogActivityAsync(user.Id, AuditAction.LoginFailed,
                        "Invalid password", AuditLogLevel.Warning);
                    return false;
                }

                _currentUser = user;
                _currentUser.UpdateLastLogin();
                await context.SaveChangesAsync();

                await _databaseService.LogActivityAsync(user.Id, AuditAction.Login,
                    $"User {username} logged in successfully", AuditLogLevel.Info);

                _logger.LogInformation("User {Username} logged in successfully", username);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Login failed for user: {Username}", username);
                await _databaseService.LogActivityAsync(0, AuditAction.LoginFailed,
                    $"Login error for user: {username}", AuditLogLevel.Error);
                return false;
            }
        }

        public void Logout()
        {
            if (_currentUser != null)
            {
                var username = _currentUser.Username;
                _databaseService.LogActivityAsync(_currentUser.Id, AuditAction.Logout,
                    $"User {username} logged out", AuditLogLevel.Info);

                _logger.LogInformation("User {Username} logged out", username);
                _currentUser = null;
            }
        }

        public async Task<bool> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            try
            {
                using var context = _databaseService.GetContext();

                var user = await context.Users.FindAsync(userId);
                if (user == null || !VerifyPassword(currentPassword, user.PasswordHash))
                {
                    return false;
                }

                user.PasswordHash = HashPassword(newPassword);
                user.UpdateTimestamp();
                await context.SaveChangesAsync();

                await _databaseService.LogActivityAsync(userId, AuditAction.UserModified,
                    "Password changed", AuditLogLevel.Info);

                _logger.LogInformation("Password changed for user: {Username}", user.Username);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to change password for user ID: {UserId}", userId);
                return false;
            }
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            try
            {
                using var context = _databaseService.GetContext();
                return await context.Users
                    .FirstOrDefaultAsync(u => u.Id == userId && u.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get user by ID: {UserId}", userId);
                return null;
            }
        }

        public async Task<IEnumerable<User>> GetAllUsersAsync()
        {
            try
            {
                using var context = _databaseService.GetContext();
                return await context.Users
                    .Where(u => u.IsActive)
                    .OrderBy(u => u.Role)
                    .ThenBy(u => u.Username)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get all users");
                return new List<User>();
            }
        }

        public async Task<bool> CreateUserAsync(string username, string password, string? firstName,
            string? lastName, UserRole role, string? email = null)
        {
            try
            {
                using var context = _databaseService.GetContext();

                // Check if username already exists
                if (await context.Users.AnyAsync(u => u.Username == username))
                {
                    return false;
                }

                var user = new User
                {
                    Username = username,
                    PasswordHash = HashPassword(password),
                    FirstName = firstName,
                    LastName = lastName,
                    Role = role,
                    Email = email,
                    IsActive = true
                };

                await context.Users.AddAsync(user);
                await context.SaveChangesAsync();

                await _databaseService.LogActivityAsync(_currentUser?.Id ?? 0, AuditAction.UserCreated,
                    $"User {username} created with role {role}", AuditLogLevel.Info);

                _logger.LogInformation("User {Username} created successfully", username);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to create user: {Username}", username);
                return false;
            }
        }

        public async Task<bool> UpdateUserAsync(int userId, string? firstName, string? lastName,
            UserRole? role, string? email, bool? isActive)
        {
            try
            {
                using var context = _databaseService.GetContext();

                var user = await context.Users.FindAsync(userId);
                if (user == null)
                {
                    return false;
                }

                var changes = new List<string>();

                if (!string.IsNullOrEmpty(firstName) && user.FirstName != firstName)
                {
                    user.FirstName = firstName;
                    changes.Add($"FirstName: {user.FirstName}");
                }

                if (!string.IsNullOrEmpty(lastName) && user.LastName != lastName)
                {
                    user.LastName = lastName;
                    changes.Add($"LastName: {user.LastName}");
                }

                if (role.HasValue && user.Role != role.Value)
                {
                    user.Role = role.Value;
                    changes.Add($"Role: {user.Role}");
                }

                if (!string.IsNullOrEmpty(email) && user.Email != email)
                {
                    user.Email = email;
                    changes.Add($"Email: {user.Email}");
                }

                if (isActive.HasValue && user.IsActive != isActive.Value)
                {
                    user.IsActive = isActive.Value;
                    changes.Add($"IsActive: {user.IsActive}");
                }

                if (changes.Any())
                {
                    user.UpdateTimestamp();
                    await context.SaveChangesAsync();

                    await _databaseService.LogActivityAsync(_currentUser?.Id ?? 0, AuditAction.UserModified,
                        $"User {user.Username} updated: {string.Join(", ", changes)}", AuditLogLevel.Info);

                    _logger.LogInformation("User {Username} updated: {Changes}", user.Username, string.Join(", ", changes));
                }

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update user ID: {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            try
            {
                using var context = _databaseService.GetContext();

                var user = await context.Users.FindAsync(userId);
                if (user == null)
                {
                    return false;
                }

                // Don't actually delete, just deactivate
                user.IsActive = false;
                user.UpdateTimestamp();
                await context.SaveChangesAsync();

                await _databaseService.LogActivityAsync(_currentUser?.Id ?? 0, AuditAction.UserDeleted,
                    $"User {user.Username} deactivated", AuditLogLevel.Info);

                _logger.LogInformation("User {Username} deactivated", user.Username);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to delete user ID: {UserId}", userId);
                return false;
            }
        }

        public async Task<bool> ResetPasswordAsync(int userId, string newPassword)
        {
            try
            {
                using var context = _databaseService.GetContext();

                var user = await context.Users.FindAsync(userId);
                if (user == null)
                {
                    return false;
                }

                user.PasswordHash = HashPassword(newPassword);
                user.UpdateTimestamp();
                await context.SaveChangesAsync();

                await _databaseService.LogActivityAsync(_currentUser?.Id ?? 0, AuditAction.UserModified,
                    $"Password reset for user {user.Username}", AuditLogLevel.Info);

                _logger.LogInformation("Password reset for user: {Username}", user.Username);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reset password for user ID: {UserId}", userId);
                return false;
            }
        }

        private string HashPassword(string password)
        {
            // In a production environment, use a proper password hashing library like BCrypt.Net
            // For demonstration purposes, we'll use SHA256 with salt
            var salt = "CashAppSalt2024"; // In production, use a unique salt per user
            using var sha256 = SHA256.Create();
            var saltedPassword = password + salt;
            var bytes = Encoding.UTF8.GetBytes(saltedPassword);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        private bool VerifyPassword(string password, string hash)
        {
            var hashedInput = HashPassword(password);
            return hashedInput == hash;
        }

        public async Task<IEnumerable<User>> GetUsersByRoleAsync(UserRole role)
        {
            try
            {
                using var context = _databaseService.GetContext();
                return await context.Users
                    .Where(u => u.Role == role && u.IsActive)
                    .OrderBy(u => u.Username)
                    .ToListAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get users by role: {Role}", role);
                return new List<User>();
            }
        }

        public async Task<bool> IsUsernameAvailableAsync(string username, int? excludeUserId = null)
        {
            try
            {
                using var context = _databaseService.GetContext();
                var query = context.Users.Where(u => u.Username == username && u.IsActive);

                if (excludeUserId.HasValue)
                {
                    query = query.Where(u => u.Id != excludeUserId.Value);
                }

                return !await query.AnyAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check username availability: {Username}", username);
                return false;
            }
        }

        public async Task<int> GetActiveUserCountAsync()
        {
            try
            {
                using var context = _databaseService.GetContext();
                return await context.Users.CountAsync(u => u.IsActive);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get active user count");
                return 0;
            }
        }

        public async Task<Dictionary<UserRole, int>> GetUserRoleDistributionAsync()
        {
            try
            {
                using var context = _databaseService.GetContext();
                return await context.Users
                    .Where(u => u.IsActive)
                    .GroupBy(u => u.Role)
                    .ToDictionaryAsync(g => g.Key, g => g.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get user role distribution");
                return new Dictionary<UserRole, int>();
            }
        }
    }
}
