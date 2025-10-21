using Microsoft.EntityFrameworkCore;
using AuctionHouse.API.Data;
using AuctionHouse.API.Models;
using AuctionHouse.API.DTOs;

namespace AuctionHouse.API.Services
{
    public class BiddingService : IBiddingService
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<BiddingService> _logger;

        public BiddingService(ApplicationDbContext context, ILogger<BiddingService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task<BidResponseDto> PlaceBidAsync(int auctionId, int bidderId, decimal amount)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                _logger.LogInformation("Starting bid placement - Auction: {AuctionId}, Bidder: {BidderId}, Amount: ${Amount}", 
                    auctionId, bidderId, amount);

                // Get the auction with seller information
                var auction = await _context.Auctions
                    .Include(a => a.Seller)
                    .Include(a => a.Bids)
                    .FirstOrDefaultAsync(a => a.AuctionId == auctionId);

                if (auction == null)
                {
                    _logger.LogWarning("Auction not found - AuctionId: {AuctionId}", auctionId);
                    throw new Exception("Auction not found");
                }

                _logger.LogDebug("Found auction: {Title}, Seller: {SellerId}, IsActive: {IsActive}", 
                    auction.Title, auction.SellerId, auction.IsActive);

                // Verify bidder exists and is a buyer
                var bidder = await _context.Users.FindAsync(bidderId);
                if (bidder == null)
                {
                    _logger.LogWarning("Bidder not found - BidderId: {BidderId}", bidderId);
                    throw new Exception("Bidder not found");
                }

                _logger.LogDebug("Found bidder: {FirstName} {LastName}, AccountType: {AccountType}", 
                    bidder.FirstName, bidder.LastName, bidder.AccountType);

                if (bidder.AccountType != "Buyer")
                {
                    _logger.LogWarning("Non-buyer attempted to place bid - BidderId: {BidderId}, AccountType: {AccountType}", 
                        bidderId, bidder.AccountType);
                    throw new Exception("Only buyers can place bids");
                }

                // Check if auction is active and within time bounds
                var now = DateTime.UtcNow;
                _logger.LogDebug("Time check - Now: {Now}, Start: {StartTime}, End: {EndTime}", 
                    now, auction.StartTime, auction.EndTime);

                if (!auction.IsActive)
                {
                    _logger.LogWarning("Auction is not active - AuctionId: {AuctionId}", auctionId);
                    throw new Exception("This auction is not active");
                }

                if (now < auction.StartTime)
                {
                    _logger.LogWarning("Auction has not started yet - AuctionId: {AuctionId}, StartTime: {StartTime}", 
                        auctionId, auction.StartTime);
                    throw new Exception("This auction has not started yet");
                }

                if (now > auction.EndTime)
                {
                    _logger.LogWarning("Auction has ended - AuctionId: {AuctionId}, EndTime: {EndTime}", 
                        auctionId, auction.EndTime);
                    throw new Exception("This auction has already ended");
                }

                // Prevent seller from bidding on own auction
                if (auction.SellerId == bidderId)
                {
                    _logger.LogWarning("Seller attempted to bid on own auction - AuctionId: {AuctionId}, SellerId: {SellerId}", 
                        auctionId, auction.SellerId);
                    throw new Exception("Sellers cannot bid on their own auctions");
                }

                // Get current highest bid
                var currentHighestBid = await _context.Bids
                    .Where(b => b.AuctionId == auctionId)
                    .OrderByDescending(b => b.Amount)
                    .FirstOrDefaultAsync();

                var minimumBidAmount = currentHighestBid?.Amount ?? auction.StartingPrice;
                
                _logger.LogDebug("Bid amount validation - Current highest: ${CurrentHighest}, Minimum required: ${MinimumRequired}, Proposed: ${ProposedAmount}", 
                    currentHighestBid?.Amount ?? 0, minimumBidAmount, amount);

                // Check if bid amount is higher than current highest bid
                if (amount <= minimumBidAmount)
                {
                    _logger.LogWarning("Bid amount too low - AuctionId: {AuctionId}, ProposedAmount: ${Amount}, RequiredMinimum: ${MinimumAmount}", 
                        auctionId, amount, minimumBidAmount);
                    throw new Exception($"Bid must be higher than current highest bid of ${minimumBidAmount:F2}");
                }

                // Check if user is bidding against themselves
                if (currentHighestBid != null && currentHighestBid.BidderId == bidderId)
                {
                    _logger.LogWarning("User attempted to bid against themselves - AuctionId: {AuctionId}, BidderId: {BidderId}", 
                        auctionId, bidderId);
                    throw new Exception("You are already the highest bidder on this auction");
                }

                // Create the new bid
                var newBid = new Bid
                {
                    AuctionId = auctionId,
                    BidderId = bidderId,
                    Amount = amount,
                    BidTime = now,
                    IsWinningBid = true // This will be the highest bid
                };

                _context.Bids.Add(newBid);
                _logger.LogDebug("Created new bid entity");

                // Update previous winning bid status
                if (currentHighestBid != null)
                {
                    currentHighestBid.IsWinningBid = false;
                    _context.Bids.Update(currentHighestBid);
                    _logger.LogDebug("Updated previous winning bid status - BidId: {BidId}", currentHighestBid.BidId);
                }

                // Update auction current price
                auction.CurrentPrice = amount;
                auction.UpdatedAt = now;
                _context.Auctions.Update(auction);
                _logger.LogDebug("Updated auction current price to ${Amount}", amount);

                await _context.SaveChangesAsync();
                _logger.LogDebug("Saved changes to database");

                await transaction.CommitAsync();
                _logger.LogDebug("Transaction committed");

                _logger.LogInformation("Bid placed successfully - Auction: {AuctionId}, Bidder: {BidderId}, Amount: ${Amount}, BidId: {BidId}", 
                    auctionId, bidderId, amount, newBid.BidId);

                // Return the bid response
                return new BidResponseDto
                {
                    BidId = newBid.BidId,
                    AuctionId = auctionId,
                    Amount = amount,
                    BidTime = now,
                    IsWinningBid = true,
                    BidderName = $"{bidder.FirstName} {bidder.LastName[0]}." // Partial name for privacy
                };
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "Error placing bid on auction {AuctionId} by user {BidderId}: {ErrorMessage}. StackTrace: {StackTrace}", 
                    auctionId, bidderId, ex.Message, ex.StackTrace);
                throw;
            }
        }

        public async Task<List<BidResponseDto>> GetAuctionBidsAsync(int auctionId, int page = 1, int pageSize = 50)
        {
            var query = _context.Bids
                .Where(b => b.AuctionId == auctionId)
                .Include(b => b.Bidder)
                .OrderByDescending(b => b.BidTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize);

            var bids = await query.ToListAsync();

            return bids.Select(b => new BidResponseDto
            {
                BidId = b.BidId,
                AuctionId = b.AuctionId,
                Amount = b.Amount,
                BidTime = b.BidTime,
                IsWinningBid = b.IsWinningBid,
                BidderName = $"{b.Bidder.FirstName} {b.Bidder.LastName[0]}." // Partial name for privacy
            }).ToList();
        }

        public async Task<List<UserBidDto>> GetUserBidsAsync(int userId, int page = 1, int pageSize = 20, string? status = null)
        {
            IQueryable<Bid> query = _context.Bids
                .Where(b => b.BidderId == userId)
                .Include(b => b.Auction);

            // Filter by status if provided
            if (!string.IsNullOrEmpty(status))
            {
                var now = DateTime.UtcNow;
                switch (status.ToLower())
                {
                    case "active":
                        query = query.Where(b => b.Auction.IsActive && b.Auction.EndTime > now);
                        break;
                    case "won":
                        query = query.Where(b => b.IsWinningBid && b.Auction.EndTime <= now);
                        break;
                    case "lost":
                        query = query.Where(b => !b.IsWinningBid && b.Auction.EndTime <= now);
                        break;
                }
            }

            var bids = await query
                .OrderByDescending(b => b.BidTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return bids.Select(b => new UserBidDto
            {
                BidId = b.BidId,
                AuctionId = b.AuctionId,
                AuctionTitle = b.Auction.Title,
                Amount = b.Amount,
                BidTime = b.BidTime,
                IsWinningBid = b.IsWinningBid,
                AuctionEndTime = b.Auction.EndTime,
                AuctionCurrentPrice = b.Auction.CurrentPrice,
                AuctionStatus = b.Auction.EndTime <= DateTime.UtcNow ? "Ended" : "Active"
            }).ToList();
        }

        public async Task<List<WinningBidDto>> GetUserWinningBidsAsync(int userId, int page = 1, int pageSize = 20)
        {
            var now = DateTime.UtcNow;
            
            var winningBids = await _context.Bids
                .Where(b => b.BidderId == userId && b.IsWinningBid && b.Auction.EndTime <= now)
                .Include(b => b.Auction)
                .Include(b => b.Auction.Seller)
                .OrderByDescending(b => b.Auction.EndTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return winningBids.Select(b => new WinningBidDto
            {
                BidId = b.BidId,
                AuctionId = b.AuctionId,
                AuctionTitle = b.Auction.Title,
                WinningAmount = b.Amount,
                AuctionEndTime = b.Auction.EndTime,
                SellerName = $"{b.Auction.Seller.FirstName} {b.Auction.Seller.LastName}",
                SellerEmail = b.Auction.Seller.Email,
                Location = b.Auction.Location,
                ShippingInfo = b.Auction.ShippingInfo
            }).ToList();
        }

        public async Task<BidStatisticsDto> GetBidStatisticsAsync(int auctionId)
        {
            var auction = await _context.Auctions
                .Include(a => a.Bids)
                .FirstOrDefaultAsync(a => a.AuctionId == auctionId);

            if (auction == null)
            {
                throw new Exception("Auction not found");
            }

            var bids = auction.Bids.ToList();
            var totalBids = bids.Count;
            var uniqueBidders = bids.Select(b => b.BidderId).Distinct().Count();

            return new BidStatisticsDto
            {
                AuctionId = auctionId,
                TotalBids = totalBids,
                UniqueBidders = uniqueBidders,
                StartingPrice = auction.StartingPrice,
                CurrentPrice = auction.CurrentPrice,
                AverageIncrease = totalBids > 0 ? 
                    (auction.CurrentPrice - auction.StartingPrice) / totalBids : 0,
                HighestBid = bids.OrderByDescending(b => b.Amount).FirstOrDefault()?.Amount ?? auction.StartingPrice,
                LastBidTime = bids.OrderByDescending(b => b.BidTime).FirstOrDefault()?.BidTime
            };
        }

        public async Task<BidResponseDto> GetHighestBidAsync(int auctionId)
        {
            var highestBid = await _context.Bids
                .Where(b => b.AuctionId == auctionId)
                .Include(b => b.Bidder)
                .OrderByDescending(b => b.Amount)
                .FirstOrDefaultAsync();

            if (highestBid == null)
            {
                return null;
            }

            return new BidResponseDto
            {
                BidId = highestBid.BidId,
                AuctionId = highestBid.AuctionId,
                Amount = highestBid.Amount,
                BidTime = highestBid.BidTime,
                IsWinningBid = highestBid.IsWinningBid,
                BidderName = $"{highestBid.Bidder.FirstName} {highestBid.Bidder.LastName[0]}."
            };
        }

        public async Task UpdateBidWinnerStatusAsync(int auctionId)
        {
            var highestBid = await _context.Bids
                .Where(b => b.AuctionId == auctionId)
                .OrderByDescending(b => b.Amount)
                .FirstOrDefaultAsync();

            if (highestBid != null)
            {
                // Reset all bids for this auction
                var allBids = await _context.Bids
                    .Where(b => b.AuctionId == auctionId)
                    .ToListAsync();

                foreach (var bid in allBids)
                {
                    bid.IsWinningBid = (bid.BidId == highestBid.BidId);
                }

                await _context.SaveChangesAsync();
            }
        }

        public async Task<bool> IsUserWinningBidderAsync(int auctionId, int userId)
        {
            var winningBid = await _context.Bids
                .Where(b => b.AuctionId == auctionId && b.IsWinningBid)
                .FirstOrDefaultAsync();

            return winningBid?.BidderId == userId;
        }
    }
}