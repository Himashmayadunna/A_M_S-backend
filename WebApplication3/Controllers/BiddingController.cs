using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AuctionHouse.API.DTOs;
using AuctionHouse.API.Services;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;

namespace AuctionHouse.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class BiddingController : ControllerBase
    {
        private readonly IBiddingService _biddingService;
        private readonly ILogger<BiddingController> _logger;

        public BiddingController(IBiddingService biddingService, ILogger<BiddingController> logger)
        {
            _biddingService = biddingService;
            _logger = logger;
        }

        /// <summary>
        /// Place a bid on an auction (Buyers only)
        /// </summary>
        [HttpPost("auctions/{auctionId}/bid")]
        [Authorize]
        public async Task<IActionResult> PlaceBid(int auctionId, [FromBody] PlaceBidDto placeBidDto)
        {
            try
            {
                _logger.LogInformation("=== Starting bid placement process ===");
                _logger.LogInformation("Auction ID: {AuctionId}, Bid Amount: {Amount}", auctionId, placeBidDto?.Amount);

                // Validate input
                if (placeBidDto == null)
                {
                    _logger.LogWarning("PlaceBidDto is null");
                    return BadRequest(new ErrorResponseDto
                    {
                        Message = "Invalid bid data",
                        Errors = new List<string> { "Bid data is required" }
                    });
                }

                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    _logger.LogWarning("Model validation failed: {Errors}", string.Join(", ", errors));

                    return BadRequest(new ErrorResponseDto
                    {
                        Message = "Validation failed",
                        Errors = errors
                    });
                }

                // Get user information from token
                int userId;
                string userAccountType;
                
                try
                {
                    userId = GetUserIdFromToken();
                    userAccountType = GetUserAccountTypeFromToken();
                    _logger.LogInformation("User extracted from token - ID: {UserId}, Type: {AccountType}", userId, userAccountType);
                }
                catch (Exception tokenEx)
                {
                    _logger.LogError(tokenEx, "Error extracting user information from token");
                    return Unauthorized(new ErrorResponseDto
                    {
                        Message = "Invalid authentication token",
                        Errors = new List<string> { tokenEx.Message }
                    });
                }

                // Ensure only buyers can place bids
                if (userAccountType != "Buyer")
                {
                    _logger.LogWarning("Non-buyer user {UserId} attempted to place bid. Account type: {AccountType}", userId, userAccountType);
                    return Forbid("Only buyers can place bids on auctions");
                }

                _logger.LogInformation("User {UserId} ({AccountType}) attempting to place bid of ${Amount} on auction {AuctionId}", 
                    userId, userAccountType, placeBidDto.Amount, auctionId);

                // Place the bid
                BidResponseDto bid;
                try
                {
                    bid = await _biddingService.PlaceBidAsync(auctionId, userId, placeBidDto.Amount);
                    _logger.LogInformation("Bid placed successfully - Bid ID: {BidId}, Amount: ${Amount}", 
                        bid.BidId, bid.Amount);
                }
                catch (Exception bidEx)
                {
                    _logger.LogError(bidEx, "Error in PlaceBidAsync: {ErrorMessage}", bidEx.Message);
                    return BadRequest(new ErrorResponseDto
                    {
                        Message = bidEx.Message,
                        Errors = new List<string> { bidEx.Message }
                    });
                }

                return Ok(new
                {
                    success = true,
                    message = "Bid placed successfully",
                    data = bid
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in PlaceBid endpoint for auction {AuctionId}: {ErrorMessage}. StackTrace: {StackTrace}", 
                    auctionId, ex.Message, ex.StackTrace);
                
                return StatusCode(500, new ErrorResponseDto
                {
                    Message = "An internal server error occurred while processing your bid",
                    Errors = new List<string> { 
                        ex.Message,
                        "Please check server logs for detailed error information"
                    }
                });
            }
        }

        /// <summary>
        /// Get all bids for a specific auction
        /// </summary>
        [HttpGet("auctions/{auctionId}/bids")]
        public async Task<IActionResult> GetAuctionBids(int auctionId, 
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 50)
        {
            try
            {
                _logger.LogInformation("Getting bids for auction {AuctionId}", auctionId);
                
                var bids = await _biddingService.GetAuctionBidsAsync(auctionId, page, pageSize);
                
                return Ok(new
                {
                    success = true,
                    data = bids,
                    pagination = new
                    {
                        page,
                        pageSize,
                        totalItems = bids.Count
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bids for auction {AuctionId}: {ErrorMessage}", auctionId, ex.Message);
                
                return BadRequest(new ErrorResponseDto
                {
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get user's bidding history
        /// </summary>
        [HttpGet("my-bids")]
        [Authorize]
        public async Task<IActionResult> GetMyBids(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string status = null) // "active", "won", "lost"
        {
            try
            {
                var userId = GetUserIdFromToken();
                _logger.LogInformation("Getting bidding history for user {UserId}", userId);
                
                var bids = await _biddingService.GetUserBidsAsync(userId, page, pageSize, status);
                
                return Ok(new
                {
                    success = true,
                    data = bids,
                    pagination = new
                    {
                        page,
                        pageSize,
                        totalItems = bids.Count
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user bids for user {UserId}: {ErrorMessage}", GetUserIdFromToken(), ex.Message);
                
                return BadRequest(new ErrorResponseDto
                {
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get winning bids for a user
        /// </summary>
        [HttpGet("my-wins")]
        [Authorize]
        public async Task<IActionResult> GetMyWinningBids(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = GetUserIdFromToken();
                _logger.LogInformation("Getting winning bids for user {UserId}", userId);
                
                var winningBids = await _biddingService.GetUserWinningBidsAsync(userId, page, pageSize);
                
                return Ok(new
                {
                    success = true,
                    data = winningBids,
                    pagination = new
                    {
                        page,
                        pageSize,
                        totalItems = winningBids.Count
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting winning bids for user {UserId}: {ErrorMessage}", GetUserIdFromToken(), ex.Message);
                
                return BadRequest(new ErrorResponseDto
                {
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get bid statistics for an auction
        /// </summary>
        [HttpGet("auctions/{auctionId}/stats")]
        public async Task<IActionResult> GetBidStatistics(int auctionId)
        {
            try
            {
                _logger.LogInformation("Getting bid statistics for auction {AuctionId}", auctionId);
                
                var stats = await _biddingService.GetBidStatisticsAsync(auctionId);
                
                return Ok(new
                {
                    success = true,
                    data = stats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bid statistics for auction {AuctionId}: {ErrorMessage}", auctionId, ex.Message);
                
                return BadRequest(new ErrorResponseDto
                {
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get current highest bid for an auction
        /// </summary>
        [HttpGet("auctions/{auctionId}/highest-bid")]
        public async Task<IActionResult> GetHighestBid(int auctionId)
        {
            try
            {
                _logger.LogInformation("Getting highest bid for auction {AuctionId}", auctionId);
                
                var highestBid = await _biddingService.GetHighestBidAsync(auctionId);
                
                return Ok(new
                {
                    success = true,
                    data = highestBid
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting highest bid for auction {AuctionId}: {ErrorMessage}", auctionId, ex.Message);
                
                return BadRequest(new ErrorResponseDto
                {
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Debug endpoint to check JWT token claims
        /// </summary>
        [HttpGet("debug/token-claims")]
        [Authorize]
        public IActionResult GetTokenClaims()
        {
            try
            {
                var claims = User.Claims.Select(c => new { 
                    Type = c.Type, 
                    Value = c.Value 
                }).ToList();

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        claims = claims,
                        userIdFromNameIdentifier = User.FindFirst(ClaimTypes.NameIdentifier)?.Value,
                        userIdFromSub = User.FindFirst("sub")?.Value,
                        accountType = User.FindFirst("AccountType")?.Value,
                        email = User.FindFirst(ClaimTypes.Email)?.Value
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting token claims");
                return BadRequest(new { error = ex.Message });
            }
        }

        /// <summary>
        /// Simple diagnostic endpoint to test authentication and user extraction
        /// </summary>
        [HttpGet("debug/auth-test")]
        [Authorize]
        public IActionResult TestAuthentication()
        {
            try
            {
                _logger.LogInformation("=== Authentication Test Started ===");
                
                var baseData = new
                {
                    isAuthenticated = User.Identity.IsAuthenticated,
                    authType = User.Identity.AuthenticationType,
                    claims = User.Claims.Select(c => new { 
                        Type = c.Type, 
                        Value = c.Value 
                    }).ToList()
                };

                object resultData;

                // Test user extraction
                try
                {
                    var userId = GetUserIdFromToken();
                    var accountType = GetUserAccountTypeFromToken();
                    
                    resultData = new
                    {
                        baseData.isAuthenticated,
                        baseData.authType,
                        baseData.claims,
                        extractedUserId = userId,
                        extractedAccountType = accountType,
                        extractionSuccessful = true
                    };
                }
                catch (Exception extractEx)
                {
                    _logger.LogError(extractEx, "Error extracting user information in auth test");
                    resultData = new
                    {
                        baseData.isAuthenticated,
                        baseData.authType,
                        baseData.claims,
                        extractionError = extractEx.Message,
                        extractionSuccessful = false
                    };
                }

                var result = new
                {
                    success = true,
                    message = "Authentication test successful",
                    data = resultData
                };

                _logger.LogInformation("Authentication test completed successfully");
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in authentication test endpoint");
                return StatusCode(500, new { 
                    success = false, 
                    error = ex.Message,
                    message = "Authentication test failed"
                });
            }
        }

        private int GetUserIdFromToken()
        {
            try
            {
                _logger.LogDebug("Attempting to extract User ID from token claims");
                
                // Log all available claims for debugging
                var allClaims = User.Claims.Select(c => $"{c.Type}: {c.Value}").ToList();
                _logger.LogDebug("Available claims: {Claims}", string.Join(", ", allClaims));

                // Try different possible claim types for user ID
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? 
                                  User.FindFirst("sub")?.Value ??
                                  User.FindFirst("UserId")?.Value ??
                                  User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value ??
                                  User.FindFirst("http://schemas.microsoft.com/ws/2008/06/identity/claims/nameidentifier")?.Value;
                
                _logger.LogDebug("User ID claim value: {UserIdClaim}", userIdClaim);

                if (string.IsNullOrEmpty(userIdClaim))
                {
                    _logger.LogError("User ID claim not found in token. Available claims: {Claims}", string.Join(", ", allClaims));
                    throw new Exception("User ID not found in authentication token");
                }

                if (!int.TryParse(userIdClaim, out int userId))
                {
                    _logger.LogError("User ID claim value '{UserIdClaim}' is not a valid integer", userIdClaim);
                    throw new Exception($"Invalid User ID format in token: {userIdClaim}");
                }

                _logger.LogDebug("Successfully extracted User ID: {UserId}", userId);
                return userId;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting User ID from token");
                throw;
            }
        }

        private string GetUserAccountTypeFromToken()
        {
            try
            {
                _logger.LogDebug("Attempting to extract Account Type from token claims");
                
                var accountTypeClaim = User.FindFirst("AccountType")?.Value;
                _logger.LogDebug("Account Type claim value: {AccountType}", accountTypeClaim);

                if (string.IsNullOrEmpty(accountTypeClaim))
                {
                    // Log all available claims for debugging
                    var allClaims = User.Claims.Select(c => $"{c.Type}: {c.Value}").ToList();
                    _logger.LogError("Account Type claim not found in token. Available claims: {Claims}", string.Join(", ", allClaims));
                    throw new Exception("Account type not found in authentication token");
                }

                _logger.LogDebug("Successfully extracted Account Type: {AccountType}", accountTypeClaim);
                return accountTypeClaim;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error extracting Account Type from token");
                throw;
            }
        }
    }
}