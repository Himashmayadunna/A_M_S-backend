using Microsoft.EntityFrameworkCore;
using AuctionHouse.API.Data;
using AuctionHouse.API.Models;
using AuctionHouse.API.DTOs;
using BCrypt.Net;

namespace AuctionHouse.API.Services
{
    public interface IAuthService
    {
        Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto);
        Task<AuthResponseDto> LoginAsync(LoginDto loginDto);
        Task<UserProfileDto> GetUserProfileAsync(int userId);
        Task<UserProfileDto> UpdateUserProfileAsync(int userId, UpdateProfileDto updateProfileDto);
        Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto);
        Task<bool> DeactivateAccountAsync(int userId, string password);
    }

    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IJwtService _jwtService;
        private readonly IConfiguration _configuration;

        public AuthService(
            ApplicationDbContext context,
            IJwtService jwtService,
            IConfiguration configuration)
        {
            _context = context;
            _jwtService = jwtService;
            _configuration = configuration;
        }

        public async Task<AuthResponseDto> RegisterAsync(RegisterDto registerDto)
        {
            // Check if user already exists
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == registerDto.Email.ToLower());

            if (existingUser != null)
            {
                throw new Exception("User with this email already exists");
            }

            // Hash password
            var passwordHash = BCrypt.Net.BCrypt.HashPassword(registerDto.Password);

            // Create new user
            var user = new User
            {
                FirstName = registerDto.FirstName,
                LastName = registerDto.LastName,
                Email = registerDto.Email.ToLower(),
                PasswordHash = passwordHash,
                AccountType = registerDto.AccountType,
                AgreeToTerms = registerDto.AgreeToTerms,
                ReceiveUpdates = registerDto.ReceiveUpdates,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // Generate JWT token
            var token = _jwtService.GenerateToken(user);
            var expiryInHours = int.Parse(_configuration["JwtSettings:ExpiryInHours"]);

            return new AuthResponseDto
            {
                UserId = user.UserId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                AccountType = user.AccountType,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(expiryInHours)
            };
        }

        public async Task<AuthResponseDto> LoginAsync(LoginDto loginDto)
        {
            // Find user by email
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email.ToLower() == loginDto.Email.ToLower());

            if (user == null)
            {
                throw new Exception("Invalid email or password");
            }

            // Check if account is active
            if (!user.IsActive)
            {
                throw new Exception("Account is deactivated");
            }

            // Verify password
            if (!BCrypt.Net.BCrypt.Verify(loginDto.Password, user.PasswordHash))
            {
                throw new Exception("Invalid email or password");
            }

            // Generate JWT token
            var token = _jwtService.GenerateToken(user);
            var expiryInHours = int.Parse(_configuration["JwtSettings:ExpiryInHours"]);

            // Update last login time (optional)
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return new AuthResponseDto
            {
                UserId = user.UserId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                AccountType = user.AccountType,
                Token = token,
                ExpiresAt = DateTime.UtcNow.AddHours(expiryInHours)
            };
        }

        public async Task<UserProfileDto> GetUserProfileAsync(int userId)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId && u.IsActive);

            if (user == null)
            {
                throw new Exception("User not found");
            }

            return new UserProfileDto
            {
                UserId = user.UserId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                AccountType = user.AccountType,
                AgreeToTerms = user.AgreeToTerms,
                ReceiveUpdates = user.ReceiveUpdates,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                IsActive = user.IsActive
            };
        }

        public async Task<UserProfileDto> UpdateUserProfileAsync(int userId, UpdateProfileDto updateProfileDto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId && u.IsActive);

            if (user == null)
            {
                throw new Exception("User not found");
            }

            // Check if email is being changed and if it's already in use
            if (!string.IsNullOrEmpty(updateProfileDto.Email) && 
                updateProfileDto.Email.ToLower() != user.Email.ToLower())
            {
                var emailExists = await _context.Users
                    .AnyAsync(u => u.Email.ToLower() == updateProfileDto.Email.ToLower() && u.UserId != userId);

                if (emailExists)
                {
                    throw new Exception("Email is already in use by another account");
                }

                user.Email = updateProfileDto.Email.ToLower();
            }

            // Update other fields
            if (!string.IsNullOrEmpty(updateProfileDto.FirstName))
                user.FirstName = updateProfileDto.FirstName;

            if (!string.IsNullOrEmpty(updateProfileDto.LastName))
                user.LastName = updateProfileDto.LastName;

            if (updateProfileDto.ReceiveUpdates.HasValue)
                user.ReceiveUpdates = updateProfileDto.ReceiveUpdates.Value;

            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return new UserProfileDto
            {
                UserId = user.UserId,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                AccountType = user.AccountType,
                AgreeToTerms = user.AgreeToTerms,
                ReceiveUpdates = user.ReceiveUpdates,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                IsActive = user.IsActive
            };
        }

        public async Task<bool> ChangePasswordAsync(int userId, ChangePasswordDto changePasswordDto)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId && u.IsActive);

            if (user == null)
            {
                throw new Exception("User not found");
            }

            // Verify current password
            if (!BCrypt.Net.BCrypt.Verify(changePasswordDto.CurrentPassword, user.PasswordHash))
            {
                throw new Exception("Current password is incorrect");
            }

            // Hash new password
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(changePasswordDto.NewPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeactivateAccountAsync(int userId, string password)
        {
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.UserId == userId && u.IsActive);

            if (user == null)
            {
                throw new Exception("User not found");
            }

            // Verify password before deactivation
            if (!BCrypt.Net.BCrypt.Verify(password, user.PasswordHash))
            {
                throw new Exception("Password is incorrect");
            }

            user.IsActive = false;
            user.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }
    }
}