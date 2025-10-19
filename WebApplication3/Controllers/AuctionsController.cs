using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AuctionHouse.API.DTOs;
using AuctionHouse.API.Services;
using System.Security.Claims;

namespace AuctionHouse.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuctionsController : ControllerBase
    {
        private readonly IAuctionService _auctionService;
        private readonly ILogger<AuctionsController> _logger;

        public AuctionsController(IAuctionService auctionService, ILogger<AuctionsController> logger)
        {
            _auctionService = auctionService;
            _logger = logger;
        }

        /// <summary>
        /// Create a new auction (Sellers only)
        /// </summary>
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateAuction([FromBody] CreateAuctionDto createAuctionDto)
        {
            try
            {
                _logger.LogInformation("Creating new auction for user");
                _logger.LogInformation("Received auction data: {@AuctionData}", createAuctionDto);
                
                if (!ModelState.IsValid)
                {
                    var errors = ModelState.Values
                        .SelectMany(v => v.Errors)
                        .Select(e => e.ErrorMessage)
                        .ToList();

                    _logger.LogWarning("Auction creation failed due to validation errors: {Errors}", string.Join(", ", errors));
                    
                    return BadRequest(new ErrorResponseDto
                    {
                        Message = "Validation failed",
                        Errors = errors
                    });
                }

                var userId = GetUserIdFromToken();
                _logger.LogInformation("Creating auction for user ID: {UserId}", userId);
                
                var auction = await _auctionService.CreateAuctionAsync(createAuctionDto, userId);

                _logger.LogInformation("Auction created successfully with ID: {AuctionId}", auction.AuctionId);

                return CreatedAtAction(nameof(GetAuction), new { id = auction.AuctionId }, new
                {
                    success = true,
                    message = "Auction created successfully",
                    data = auction
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating auction: {ErrorMessage}", ex.Message);
                
                return BadRequest(new ErrorResponseDto
                {
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get auction by ID
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetAuction(int id)
        {
            try
            {
                _logger.LogInformation("Getting auction with ID: {AuctionId}", id);
                
                var auction = await _auctionService.GetAuctionByIdAsync(id);
                return Ok(new
                {
                    success = true,
                    data = auction
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting auction {AuctionId}: {ErrorMessage}", id, ex.Message);
                
                return NotFound(new ErrorResponseDto
                {
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get all auctions with optional filtering
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAuctions(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string category = null,
            [FromQuery] string search = null)
        {
            try
            {
                _logger.LogInformation("Getting auctions - Page: {Page}, PageSize: {PageSize}, Category: {Category}, Search: {Search}", 
                    page, pageSize, category, search);
                
                var auctions = await _auctionService.GetAuctionsAsync(page, pageSize, category, search);
                return Ok(new
                {
                    success = true,
                    data = auctions,
                    pagination = new
                    {
                        page,
                        pageSize,
                        totalItems = auctions.Count
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting auctions: {ErrorMessage}", ex.Message);
                
                return BadRequest(new ErrorResponseDto
                {
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get seller's auctions
        /// </summary>
        [HttpGet("seller")]
        [Authorize]
        public async Task<IActionResult> GetSellerAuctions(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            try
            {
                var userId = GetUserIdFromToken();
                _logger.LogInformation("Getting auctions for seller ID: {UserId}", userId);
                
                var auctions = await _auctionService.GetSellerAuctionsAsync(userId, page, pageSize);

                return Ok(new
                {
                    success = true,
                    data = auctions,
                    pagination = new
                    {
                        page,
                        pageSize,
                        totalItems = auctions.Count
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting seller auctions: {ErrorMessage}", ex.Message);
                
                return BadRequest(new ErrorResponseDto
                {
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Update auction
        /// </summary>
        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> UpdateAuction(int id, [FromBody] UpdateAuctionDto updateAuctionDto)
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
                _logger.LogInformation("Updating auction {AuctionId} for user {UserId}", id, userId);
                
                var auction = await _auctionService.UpdateAuctionAsync(id, updateAuctionDto, userId);

                return Ok(new
                {
                    success = true,
                    message = "Auction updated successfully",
                    data = auction
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating auction {AuctionId}: {ErrorMessage}", id, ex.Message);
                
                return BadRequest(new ErrorResponseDto
                {
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Delete auction
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteAuction(int id)
        {
            try
            {
                var userId = GetUserIdFromToken();
                _logger.LogInformation("Deleting auction {AuctionId} for user {UserId}", id, userId);
                
                await _auctionService.DeleteAuctionAsync(id, userId);

                return Ok(new
                {
                    success = true,
                    message = "Auction deleted successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting auction {AuctionId}: {ErrorMessage}", id, ex.Message);
                
                return BadRequest(new ErrorResponseDto
                {
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Place a bid on an auction
        /// </summary>
        [HttpPost("{id}/bids")]
        [Authorize]
        public async Task<IActionResult> PlaceBid(int id, [FromBody] PlaceBidDto placeBidDto)
        {
            try
            {
                var userId = GetUserIdFromToken();
                var bid = await _auctionService.PlaceBidAsync(id, placeBidDto, userId);

                return Ok(new
                {
                    success = true,
                    message = "Bid placed successfully",
                    data = bid
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorResponseDto
                {
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get bids for an auction
        /// </summary>
        [HttpGet("{id}/bids")]
        public async Task<IActionResult> GetAuctionBids(int id)
        {
            try
            {
                var bids = await _auctionService.GetAuctionBidsAsync(id);
                return Ok(new
                {
                    success = true,
                    data = bids
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorResponseDto
                {
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Add auction to watchlist
        /// </summary>
        [HttpPost("{id}/watchlist")]
        [Authorize]
        public async Task<IActionResult> AddToWatchlist(int id)
        {
            try
            {
                var userId = GetUserIdFromToken();
                var result = await _auctionService.AddToWatchlistAsync(id, userId);

                if (result)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Auction added to watchlist"
                    });
                }
                else
                {
                    return BadRequest(new ErrorResponseDto
                    {
                        Message = "Auction is already in your watchlist",
                        Errors = new List<string> { "Already in watchlist" }
                    });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorResponseDto
                {
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Remove auction from watchlist
        /// </summary>
        [HttpDelete("{id}/watchlist")]
        [Authorize]
        public async Task<IActionResult> RemoveFromWatchlist(int id)
        {
            try
            {
                var userId = GetUserIdFromToken();
                var result = await _auctionService.RemoveFromWatchlistAsync(id, userId);

                if (result)
                {
                    return Ok(new
                    {
                        success = true,
                        message = "Auction removed from watchlist"
                    });
                }
                else
                {
                    return NotFound(new ErrorResponseDto
                    {
                        Message = "Auction not found in watchlist",
                        Errors = new List<string> { "Not in watchlist" }
                    });
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorResponseDto
                {
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get user's watchlist
        /// </summary>
        [HttpGet("watchlist")]
        [Authorize]
        public async Task<IActionResult> GetWatchlist()
        {
            try
            {
                var userId = GetUserIdFromToken();
                var watchlist = await _auctionService.GetWatchlistAsync(userId);

                return Ok(new
                {
                    success = true,
                    data = watchlist
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new ErrorResponseDto
                {
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get auction categories
        /// </summary>
        [HttpGet("categories")]
        public IActionResult GetCategories()
        {
            var categories = new[]
            {
                "Electronics", "Fashion", "Home & Garden", "Sports & Recreation",
                "Collectibles", "Art", "Jewelry", "Books", "Music", "Movies & TV",
                "Toys & Hobbies", "Health & Beauty", "Automotive", "Business & Industrial",
                "Real Estate", "Services", "Other"
            };

            return Ok(new
            {
                success = true,
                data = categories
            });
        }

        /// <summary>
        /// Test endpoint for debugging
        /// </summary>
        [HttpGet("test")]
        [Authorize]
        public IActionResult TestAuth()
        {
            try
            {
                var userId = GetUserIdFromToken();
                var claims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
                
                return Ok(new
                {
                    success = true,
                    userId = userId,
                    claims = claims,
                    message = "Authentication working correctly"
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new
                {
                    success = false,
                    message = ex.Message
                });
            }
        }

        private int GetUserIdFromToken()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !int.TryParse(userIdClaim, out int userId))
            {
                throw new Exception("Invalid token: User ID not found");
            }
            return userId;
        }
    }
}