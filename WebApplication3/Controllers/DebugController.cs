using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;
using AuctionHouse.API.Data;
using System.Security.Claims;

namespace AuctionHouse.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class DebugController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DebugController> _logger;

        public DebugController(ApplicationDbContext context, ILogger<DebugController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Test database connection
        /// </summary>
        [HttpGet("db-test")]
        public async Task<IActionResult> TestDatabase()
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync();
                var userCount = await _context.Users.CountAsync();
                var auctionCount = await _context.Auctions.CountAsync();

                return Ok(new
                {
                    success = true,
                    canConnect,
                    userCount,
                    auctionCount,
                    message = "Database connection successful"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Database connection failed: {ErrorMessage}", ex.Message);
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message,
                    innerException = ex.InnerException?.Message
                });
            }
        }

        /// <summary>
        /// Get current user info from token
        /// </summary>
        [HttpGet("user-info")]
        [Authorize]
        public async Task<IActionResult> GetUserInfo()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var emailClaim = User.FindFirst(ClaimTypes.Email)?.Value;
                var allClaims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();

                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid token: User ID not found",
                        claims = allClaims
                    });
                }

                var user = await _context.Users.FindAsync(userId);

                return Ok(new
                {
                    success = true,
                    user = user != null ? new
                    {
                        userId = user.UserId,
                        email = user.Email,
                        firstName = user.FirstName,
                        lastName = user.LastName,
                        accountType = user.AccountType,
                        isActive = user.IsActive
                    } : null,
                    claims = allClaims,
                    message = "User info retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user info: {ErrorMessage}", ex.Message);
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        /// <summary>
        /// Get recent auctions
        /// </summary>
        [HttpGet("recent-auctions")]
        public async Task<IActionResult> GetRecentAuctions()
        {
            try
            {
                var auctions = await _context.Auctions
                    .Include(a => a.Seller)
                    .OrderByDescending(a => a.CreatedAt)
                    .Take(5)
                    .Select(a => new
                    {
                        auctionId = a.AuctionId,
                        title = a.Title,
                        createdAt = a.CreatedAt,
                        isActive = a.IsActive,
                        sellerId = a.SellerId,
                        sellerName = $"{a.Seller.FirstName} {a.Seller.LastName}",
                        sellerAccountType = a.Seller.AccountType
                    })
                    .ToListAsync();

                return Ok(new
                {
                    success = true,
                    data = auctions,
                    message = "Recent auctions retrieved successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting recent auctions: {ErrorMessage}", ex.Message);
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        /// <summary>
        /// Test auction creation (for debugging)
        /// </summary>
        [HttpPost("test-auction")]
        [Authorize]
        public async Task<IActionResult> TestAuctionCreation()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "Invalid token: User ID not found"
                    });
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = "User not found in database"
                    });
                }

                if (user.AccountType != "Seller")
                {
                    return BadRequest(new
                    {
                        success = false,
                        message = $"User account type is '{user.AccountType}', but 'Seller' is required"
                    });
                }

                // Create a test auction
                var testAuction = new AuctionHouse.API.Models.Auction
                {
                    Title = "Test Auction " + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
                    Description = "This is a test auction created for debugging purposes",
                    StartingPrice = 10.00m,
                    CurrentPrice = 10.00m,
                    StartTime = DateTime.UtcNow,
                    EndTime = DateTime.UtcNow.AddDays(7),
                    Category = "Other",
                    Condition = "New",
                    Location = "Test Location",
                    ShippingInfo = "Test Shipping Info",
                    IsActive = true,
                    IsFeatured = false,
                    SellerId = userId,
                    CreatedAt = DateTime.UtcNow
                };

                _context.Auctions.Add(testAuction);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    auctionId = testAuction.AuctionId,
                    message = "Test auction created successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating test auction: {ErrorMessage}", ex.Message);
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message,
                    innerException = ex.InnerException?.Message
                });
            }
        }
    }
}