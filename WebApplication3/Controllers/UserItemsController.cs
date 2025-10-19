using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using AuctionHouse.API.DTOs;
using AuctionHouse.API.Services;
using System.Security.Claims;

namespace AuctionHouse.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class UserItemsController : ControllerBase
    {
        private readonly IAuctionService _auctionService;
        private readonly IBiddingService _biddingService;
        private readonly ILogger<UserItemsController> _logger;

        public UserItemsController(
            IAuctionService auctionService, 
            IBiddingService biddingService,
            ILogger<UserItemsController> logger)
        {
            _auctionService = auctionService;
            _biddingService = biddingService;
            _logger = logger;
        }

        /// <summary>
        /// Get all user's auctions (for sellers)
        /// </summary>
        [HttpGet("my-auctions")]
        public async Task<IActionResult> GetMyAuctions(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string status = null) // "active", "ended", "upcoming"
        {
            try
            {
                var userId = GetUserIdFromToken();
                var userAccountType = GetUserAccountTypeFromToken();

                if (userAccountType != "Seller")
                {
                    return Forbid("Only sellers can view their auctions");
                }

                _logger.LogInformation("Getting auctions for seller {UserId}, status: {Status}", userId, status);

                var auctions = await _auctionService.GetSellerAuctionsAsync(userId, page, pageSize);

                // Filter by status if provided
                if (!string.IsNullOrEmpty(status))
                {
                    var now = DateTime.UtcNow;
                    auctions = status.ToLower() switch
                    {
                        "active" => auctions.Where(a => a.IsActive && a.StartTime <= now && a.EndTime > now).ToList(),
                        "ended" => auctions.Where(a => a.EndTime <= now).ToList(),
                        "upcoming" => auctions.Where(a => a.StartTime > now).ToList(),
                        _ => auctions
                    };
                }

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
                _logger.LogError(ex, "Error getting auctions for user {UserId}: {ErrorMessage}", GetUserIdFromToken(), ex.Message);
                return BadRequest(new ErrorResponseDto
                {
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Remove/Delete an auction (sellers only, with restrictions)
        /// </summary>
        [HttpDelete("auctions/{auctionId}")]
        public async Task<IActionResult> RemoveAuction(int auctionId, [FromBody] RemoveAuctionDto removeDto)
        {
            try
            {
                var userId = GetUserIdFromToken();
                var userAccountType = GetUserAccountTypeFromToken();

                if (userAccountType != "Seller")
                {
                    return Forbid("Only sellers can remove their auctions");
                }

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

                _logger.LogInformation("User {UserId} attempting to remove auction {AuctionId}", userId, auctionId);

                // Get auction details first to validate removal conditions
                var auction = await _auctionService.GetAuctionByIdAsync(auctionId);
                
                if (auction.Seller.UserId != userId)
                {
                    return Forbid("You can only remove your own auctions");
                }

                // Check if auction has active bids
                var bids = await _biddingService.GetAuctionBidsAsync(auctionId, 1, 1);
                var now = DateTime.UtcNow;

                // Prevent deletion if auction has bids and is active/ended
                if (bids.Any() && auction.EndTime <= now)
                {
                    return BadRequest(new ErrorResponseDto
                    {
                        Message = "Cannot remove auction that has ended with bids. Contact support for assistance.",
                        Errors = new List<string> { "Auction has concluded with bids" }
                    });
                }

                // Allow removal with confirmation if auction is active but has bids
                if (bids.Any() && auction.EndTime > now)
                {
                    if (!removeDto.ConfirmRemoval)
                    {
                        return BadRequest(new ErrorResponseDto
                        {
                            Message = "This auction has active bids. Removing it will refund all bidders. Please confirm removal.",
                            Errors = new List<string> { "Confirmation required for auction with bids" }
                        });
                    }

                    _logger.LogWarning("Removing active auction {AuctionId} with {BidCount} bids", auctionId, bids.Count);
                }

                // Perform the deletion
                await _auctionService.DeleteAuctionAsync(auctionId, userId);

                _logger.LogInformation("Auction {AuctionId} removed successfully by user {UserId}", auctionId, userId);

                return Ok(new
                {
                    success = true,
                    message = "Auction removed successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error removing auction {AuctionId} for user {UserId}: {ErrorMessage}", 
                    auctionId, GetUserIdFromToken(), ex.Message);
                
                return BadRequest(new ErrorResponseDto
                {
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Deactivate an auction (soft delete - makes it inactive but keeps data)
        /// </summary>
        [HttpPut("auctions/{auctionId}/deactivate")]
        public async Task<IActionResult> DeactivateAuction(int auctionId, [FromBody] DeactivateAuctionDto deactivateDto)
        {
            try
            {
                var userId = GetUserIdFromToken();
                var userAccountType = GetUserAccountTypeFromToken();

                if (userAccountType != "Seller")
                {
                    return Forbid("Only sellers can deactivate their auctions");
                }

                _logger.LogInformation("User {UserId} attempting to deactivate auction {AuctionId}", userId, auctionId);

                // Get auction to verify ownership
                var auction = await _auctionService.GetAuctionByIdAsync(auctionId);
                
                if (auction.Seller.UserId != userId)
                {
                    return Forbid("You can only deactivate your own auctions");
                }

                // Check if auction has already ended
                if (auction.EndTime <= DateTime.UtcNow)
                {
                    return BadRequest(new ErrorResponseDto
                    {
                        Message = "Cannot deactivate an auction that has already ended",
                        Errors = new List<string> { "Auction has ended" }
                    });
                }

                // Update auction to inactive
                var updateDto = new UpdateAuctionDto
                {
                    Title = auction.Title,
                    Description = auction.Description,
                    ReservePrice = auction.ReservePrice,
                    EndTime = auction.EndTime,
                    Category = auction.Category,
                    Condition = auction.Condition,
                    Location = auction.Location,
                    ShippingInfo = auction.ShippingInfo,
                    IsFeatured = false // Remove featured status when deactivating
                };

                await _auctionService.UpdateAuctionAsync(auctionId, updateDto, userId);

                _logger.LogInformation("Auction {AuctionId} deactivated successfully by user {UserId}", auctionId, userId);

                return Ok(new
                {
                    success = true,
                    message = "Auction deactivated successfully"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deactivating auction {AuctionId} for user {UserId}: {ErrorMessage}", 
                    auctionId, GetUserIdFromToken(), ex.Message);
                
                return BadRequest(new ErrorResponseDto
                {
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get user's bidding activity summary
        /// </summary>
        [HttpGet("bidding-summary")]
        public async Task<IActionResult> GetBiddingSummary()
        {
            try
            {
                var userId = GetUserIdFromToken();
                var userAccountType = GetUserAccountTypeFromToken();

                if (userAccountType != "Buyer")
                {
                    return Forbid("Only buyers can view bidding summary");
                }

                _logger.LogInformation("Getting bidding summary for user {UserId}", userId);

                // Get user's bids
                var allBids = await _biddingService.GetUserBidsAsync(userId, 1, 1000);
                var winningBids = await _biddingService.GetUserWinningBidsAsync(userId, 1, 1000);

                var now = DateTime.UtcNow;
                var activeBids = allBids.Where(b => b.AuctionStatus == "Active").ToList();
                var completedBids = allBids.Where(b => b.AuctionStatus == "Ended").ToList();

                var summary = new
                {
                    TotalBids = allBids.Count,
                    ActiveBids = activeBids.Count,
                    WonAuctions = winningBids.Count,
                    LostAuctions = completedBids.Count(b => !b.IsWinningBid),
                    TotalAmountBid = allBids.Sum(b => b.Amount),
                    TotalAmountWon = winningBids.Sum(w => w.WinningAmount),
                    RecentBids = allBids.Take(5).ToList(),
                    RecentWins = winningBids.Take(3).ToList()
                };

                return Ok(new
                {
                    success = true,
                    data = summary
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting bidding summary for user {UserId}: {ErrorMessage}", 
                    GetUserIdFromToken(), ex.Message);
                
                return BadRequest(new ErrorResponseDto
                {
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
                });
            }
        }

        /// <summary>
        /// Get user's auction statistics (for sellers)
        /// </summary>
        [HttpGet("auction-stats")]
        public async Task<IActionResult> GetAuctionStats()
        {
            try
            {
                var userId = GetUserIdFromToken();
                var userAccountType = GetUserAccountTypeFromToken();

                if (userAccountType != "Seller")
                {
                    return Forbid("Only sellers can view auction statistics");
                }

                _logger.LogInformation("Getting auction stats for seller {UserId}", userId);

                var auctions = await _auctionService.GetSellerAuctionsAsync(userId, 1, 1000);
                var now = DateTime.UtcNow;

                var activeAuctions = auctions.Where(a => a.IsActive && a.StartTime <= now && a.EndTime > now).ToList();
                var endedAuctions = auctions.Where(a => a.EndTime <= now).ToList();
                var upcomingAuctions = auctions.Where(a => a.StartTime > now).ToList();

                var stats = new
                {
                    TotalAuctions = auctions.Count,
                    ActiveAuctions = activeAuctions.Count,
                    EndedAuctions = endedAuctions.Count,
                    UpcomingAuctions = upcomingAuctions.Count,
                    TotalRevenue = endedAuctions.Sum(a => a.CurrentPrice),
                    AverageSellingPrice = endedAuctions.Any() ? endedAuctions.Average(a => a.CurrentPrice) : 0,
                    TotalViews = auctions.Sum(a => a.ViewCount),
                    FeaturedAuctions = auctions.Count(a => a.IsFeatured),
                    MostPopularCategory = auctions.GroupBy(a => a.Category)
                        .OrderByDescending(g => g.Count())
                        .FirstOrDefault()?.Key ?? "None"
                };

                return Ok(new
                {
                    success = true,
                    data = stats
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting auction stats for user {UserId}: {ErrorMessage}", 
                    GetUserIdFromToken(), ex.Message);
                
                return BadRequest(new ErrorResponseDto
                {
                    Message = ex.Message,
                    Errors = new List<string> { ex.Message }
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

        private string GetUserAccountTypeFromToken()
        {
            var accountTypeClaim = User.FindFirst("AccountType")?.Value;
            if (string.IsNullOrEmpty(accountTypeClaim))
            {
                throw new Exception("Invalid token: Account type not found");
            }
            return accountTypeClaim;
        }
    }
}