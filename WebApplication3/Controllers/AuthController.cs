using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AuctionHouse.API.DTOs;
using AuctionHouse.API.Services;
using System.Security.Claims;

namespace AuctionHouse.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(IAuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService;
            _logger = logger;
        }

        /// <summary>
        /// Register a new user
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new ErrorResponseDto
                    {
                        Message = "Validation failed",
                        Errors = errors
                    });
                }

                var response = await _authService.RegisterAsync(registerDto);
                _logger.LogInformation("User registered successfully: {Email}", registerDto.Email);
                
                return Ok(new
                {
                    success = true,
                    message = "User registered successfully",
                    data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Registration failed for email: {Email}", registerDto.Email);
                return BadRequest(new ErrorResponseDto
                {
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Login user
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto loginDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new ErrorResponseDto
                    {
                        Message = "Validation failed",
                        Errors = errors
                    });
                }

                var response = await _authService.LoginAsync(loginDto);
                _logger.LogInformation("User logged in successfully: {Email}", loginDto.Email);
                
                return Ok(new
                {
                    success = true,
                    message = "Login successful",
                    data = response
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Login failed for email: {Email} - {Error}", loginDto.Email, ex.Message);
                return Unauthorized(new ErrorResponseDto
                {
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get current user profile
        /// </summary>
        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var userId = GetUserIdFromToken();
                var profile = await _authService.GetUserProfileAsync(userId);
                
                return Ok(new
                {
                    success = true,
                    data = profile
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting profile for user: {UserId}", GetUserIdFromToken());
                return BadRequest(new ErrorResponseDto
                {
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Update user profile
        /// </summary>
        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto updateProfileDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new ErrorResponseDto
                    {
                        Message = "Validation failed",
                        Errors = errors
                    });
                }

                var userId = GetUserIdFromToken();
                var updatedProfile = await _authService.UpdateUserProfileAsync(userId, updateProfileDto);
                
                _logger.LogInformation("Profile updated successfully for user: {UserId}", userId);
                
                return Ok(new
                {
                    success = true,
                    message = "Profile updated successfully",
                    data = updatedProfile
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for user: {UserId}", GetUserIdFromToken());
                return BadRequest(new ErrorResponseDto
                {
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Change user password
        /// </summary>
        [HttpPut("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto changePasswordDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new ErrorResponseDto
                    {
                        Message = "Validation failed",
                        Errors = errors
                    });
                }

                var userId = GetUserIdFromToken();
                await _authService.ChangePasswordAsync(userId, changePasswordDto);
                
                _logger.LogInformation("Password changed successfully for user: {UserId}", userId);
                
                return Ok(new
                {
                    success = true,
                    message = "Password changed successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user: {UserId}", GetUserIdFromToken());
                return BadRequest(new ErrorResponseDto
                {
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Deactivate user account
        /// </summary>
        [HttpDelete("deactivate")]
        [Authorize]
        public async Task<IActionResult> DeactivateAccount([FromBody] DeactivateAccountDto deactivateDto)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    return BadRequest(new ErrorResponseDto
                    {
                        Message = "Validation failed",
                        Errors = errors
                    });
                }

                var userId = GetUserIdFromToken();
                await _authService.DeactivateAccountAsync(userId, deactivateDto.Password);
                
                _logger.LogInformation("Account deactivated for user: {UserId}", userId);
                
                return Ok(new
                {
                    success = true,
                    message = "Account deactivated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating account for user: {UserId}", GetUserIdFromToken());
                return BadRequest(new ErrorResponseDto
                {
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Health check endpoint
        /// </summary>
        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new
            {
                status = "healthy",
                timestamp = DateTime.UtcNow
            });
        }

        private int GetUserIdFromToken()
        {
            // Try different possible claim types for user ID
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
                              User.FindFirst("sub")?.Value ??
                              User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;
            
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new Exception("Invalid token: User ID not found");
            }
            return userId;
        }
    }
}