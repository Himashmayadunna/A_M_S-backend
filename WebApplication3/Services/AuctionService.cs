using Microsoft.EntityFrameworkCore;
using AuctionHouse.API.Data;
using AuctionHouse.API.Models;
using AuctionHouse.API.DTOs;
using WebApplication3.DTOs;

namespace AuctionHouse.API.Services
{
    public interface IAuctionService
    {
        Task<AuctionResponseDto> CreateAuctionAsync(CreateAuctionDto createAuctionDto, int sellerId);
        Task<AuctionResponseDto> GetAuctionByIdAsync(int auctionId);
        Task<List<AuctionListDto>> GetAuctionsAsync(int page = 1, int pageSize = 20, string? category = null, string? search = null);
        Task<List<AuctionListDto>> GetSellerAuctionsAsync(int sellerId, int page = 1, int pageSize = 20);
        Task<AuctionResponseDto> UpdateAuctionAsync(int auctionId, UpdateAuctionDto updateAuctionDto, int sellerId);
        Task<bool> DeleteAuctionAsync(int auctionId, int sellerId);
        Task<BidResponseDto> PlaceBidAsync(int auctionId, PlaceBidDto placeBidDto, int bidderId);
        Task<List<BidResponseDto>> GetAuctionBidsAsync(int auctionId);
        Task<bool> AddToWatchlistAsync(int auctionId, int userId);
        Task<bool> RemoveFromWatchlistAsync(int auctionId, int userId);
        Task<List<AuctionListDto>> GetWatchlistAsync(int userId);
    }

    public class AuctionService : IAuctionService
    {
        private readonly ApplicationDbContext _context;

        public AuctionService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<AuctionResponseDto> CreateAuctionAsync(CreateAuctionDto createAuctionDto, int sellerId)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                // Use sellerId from token, but allow override from DTO if provided (for compatibility)
                var finalSellerId = createAuctionDto.SellerId ?? sellerId;
                
                // Validate that the user exists and is a seller
                var seller = await _context.Users.FindAsync(finalSellerId);
                if (seller == null)
                {
                    throw new Exception("User not found");
                }
                
                if (seller.AccountType != "Seller")
                {
                    throw new Exception("Only sellers can create auctions");
                }

                // Calculate EndTime from Duration if not provided
                DateTime calculatedEndTime = createAuctionDto.EndTime;
                if (createAuctionDto.Duration > 0)
                {
                    calculatedEndTime = createAuctionDto.StartTime.AddDays(createAuctionDto.Duration);
                }

                // Validate dates
                if (createAuctionDto.StartTime < DateTime.UtcNow.AddMinutes(-5)) // Allow 5 minute tolerance
                {
                    createAuctionDto.StartTime = DateTime.UtcNow;
                }

                if (calculatedEndTime <= createAuctionDto.StartTime)
                {
                    throw new Exception("End time must be after start time");
                }

                if (createAuctionDto.ReservePrice.HasValue && createAuctionDto.ReservePrice < createAuctionDto.StartingPrice)
                {
                    throw new Exception("Reserve price cannot be less than starting price");
                }

                var auction = new Auction
                {
                    Title = createAuctionDto.Title?.Trim(),
                    Description = createAuctionDto.Description?.Trim(),
                    StartingPrice = createAuctionDto.StartingPrice,
                    ReservePrice = createAuctionDto.ReservePrice,
                    CurrentPrice = createAuctionDto.StartingPrice,
                    StartTime = createAuctionDto.StartTime,
                    EndTime = calculatedEndTime,
                    Category = createAuctionDto.Category?.Trim(),
                    Condition = createAuctionDto.Condition?.Trim() ?? "New",
                    Location = createAuctionDto.Location?.Trim() ?? "",
                    ShippingInfo = createAuctionDto.ShippingInfo?.Trim() ?? "",
                    IsFeatured = createAuctionDto.IsFeatured || createAuctionDto.PremiumListing,
                    IsActive = true,
                    SellerId = finalSellerId,
                    CreatedAt = DateTime.UtcNow,
                    ViewCount = 0,
                    
                    // New fields from frontend
                    Tags = createAuctionDto.Tags?.Trim() ?? "",
                    Duration = createAuctionDto.Duration,
                    Shipping = createAuctionDto.Shipping?.Trim() ?? "",
                    Status = createAuctionDto.Status?.Trim() ?? "Active",
                    AuctionType = createAuctionDto.AuctionType?.Trim() ?? "Standard",
                    AuthenticityGuarantee = createAuctionDto.AuthenticityGuarantee,
                    AcceptReturns = createAuctionDto.AcceptReturns,
                    PremiumListing = createAuctionDto.PremiumListing
                };

                _context.Auctions.Add(auction);
                await _context.SaveChangesAsync();

                await transaction.CommitAsync();
                return await GetAuctionByIdAsync(auction.AuctionId);
            }
            catch (Exception)
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        public async Task<AuctionResponseDto> GetAuctionByIdAsync(int auctionId)
        {
            var auction = await _context.Auctions
                .Include(a => a.Seller)
                .Include(a => a.Bids)
                .Include(a => a.Images)
                .FirstOrDefaultAsync(a => a.AuctionId == auctionId);

            if (auction == null)
            {
                throw new Exception("Auction not found");
            }

            // Increment view count
            auction.ViewCount++;
            await _context.SaveChangesAsync();

            return MapToAuctionResponseDto(auction);
        }

        public async Task<List<AuctionListDto>> GetAuctionsAsync(int page = 1, int pageSize = 20, string? category = null, string? search = null)
        {
            var query = _context.Auctions
                .Include(a => a.Seller)
                .Include(a => a.Bids)
                .Include(a => a.Images)
                .Where(a => a.IsActive);

            if (!string.IsNullOrEmpty(category))
            {
                query = query.Where(a => a.Category.ToLower() == category.ToLower());
            }

            if (!string.IsNullOrEmpty(search))
            {
                query = query.Where(a => a.Title.Contains(search) || a.Description.Contains(search));
            }

            var auctions = await query
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return auctions.Select(MapToAuctionListDto).ToList();
        }

        public async Task<List<AuctionListDto>> GetSellerAuctionsAsync(int sellerId, int page = 1, int pageSize = 20)
        {
            var auctions = await _context.Auctions
                .Include(a => a.Seller)
                .Include(a => a.Bids)
                .Include(a => a.Images)
                .Where(a => a.SellerId == sellerId)
                .OrderByDescending(a => a.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return auctions.Select(MapToAuctionListDto).ToList();
        }

        public async Task<AuctionResponseDto> UpdateAuctionAsync(int auctionId, UpdateAuctionDto updateAuctionDto, int sellerId)
        {
            var auction = await _context.Auctions.FindAsync(auctionId);

            if (auction == null)
            {
                throw new Exception("Auction not found");
            }

            if (auction.SellerId != sellerId)
            {
                throw new Exception("You can only update your own auctions");
            }

            // Check if auction has started (can't modify certain fields after start)
            if (auction.StartTime <= DateTime.UtcNow && auction.Bids.Any())
            {
                throw new Exception("Cannot modify auction details after bidding has started");
            }

            auction.Title = updateAuctionDto.Title;
            auction.Description = updateAuctionDto.Description;
            auction.ReservePrice = updateAuctionDto.ReservePrice;
            auction.EndTime = updateAuctionDto.EndTime;
            auction.Category = updateAuctionDto.Category;
            auction.Condition = updateAuctionDto.Condition;
            auction.Location = updateAuctionDto.Location;
            auction.ShippingInfo = updateAuctionDto.ShippingInfo;
            auction.IsFeatured = updateAuctionDto.IsFeatured;
            auction.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return await GetAuctionByIdAsync(auctionId);
        }

        public async Task<bool> DeleteAuctionAsync(int auctionId, int sellerId)
        {
            var auction = await _context.Auctions
                .Include(a => a.Bids)
                .FirstOrDefaultAsync(a => a.AuctionId == auctionId);

            if (auction == null)
            {
                throw new Exception("Auction not found");
            }

            if (auction.SellerId != sellerId)
            {
                throw new Exception("You can only delete your own auctions");
            }

            if (auction.Bids.Any())
            {
                throw new Exception("Cannot delete auction with existing bids");
            }

            _context.Auctions.Remove(auction);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<BidResponseDto> PlaceBidAsync(int auctionId, PlaceBidDto placeBidDto, int bidderId)
        {
            var auction = await _context.Auctions
                .Include(a => a.Bids)
                .FirstOrDefaultAsync(a => a.AuctionId == auctionId);

            if (auction == null)
            {
                throw new Exception("Auction not found");
            }

            if (auction.SellerId == bidderId)
            {
                throw new Exception("Sellers cannot bid on their own auctions");
            }

            if (DateTime.UtcNow < auction.StartTime)
            {
                throw new Exception("Auction has not started yet");
            }

            if (DateTime.UtcNow > auction.EndTime)
            {
                throw new Exception("Auction has ended");
            }

            if (placeBidDto.Amount <= auction.CurrentPrice)
            {
                throw new Exception($"Bid must be higher than current price of ${auction.CurrentPrice}");
            }

            // Update previous winning bid
            var previousWinningBid = auction.Bids.FirstOrDefault(b => b.IsWinningBid);
            if (previousWinningBid != null)
            {
                previousWinningBid.IsWinningBid = false;
            }

            var bid = new Bid
            {
                AuctionId = auctionId,
                BidderId = bidderId,
                Amount = placeBidDto.Amount,
                BidTime = DateTime.UtcNow,
                IsWinningBid = true
            };

            _context.Bids.Add(bid);

            // Update auction current price
            auction.CurrentPrice = placeBidDto.Amount;
            auction.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            var bidder = await _context.Users.FindAsync(bidderId);
            return new BidResponseDto
            {
                BidId = bid.BidId,
                AuctionId = bid.AuctionId,
                Amount = bid.Amount,
                BidTime = bid.BidTime,
                IsWinningBid = bid.IsWinningBid,
                BidderName = $"{bidder.FirstName} {bidder.LastName}"
            };
        }

        public async Task<List<BidResponseDto>> GetAuctionBidsAsync(int auctionId)
        {
            var bids = await _context.Bids
                .Include(b => b.Bidder)
                .Where(b => b.AuctionId == auctionId)
                .OrderByDescending(b => b.BidTime)
                .ToListAsync();

            return bids.Select(b => new BidResponseDto
            {
                BidId = b.BidId,
                AuctionId = b.AuctionId,
                Amount = b.Amount,
                BidTime = b.BidTime,
                IsWinningBid = b.IsWinningBid,
                BidderName = $"{b.Bidder.FirstName} {b.Bidder.LastName}"
            }).ToList();
        }

        public async Task<bool> AddToWatchlistAsync(int auctionId, int userId)
        {
            var existingItem = await _context.WatchlistItems
                .FirstOrDefaultAsync(w => w.AuctionId == auctionId && w.UserId == userId);

            if (existingItem != null)
            {
                return false; // Already in watchlist
            }

            var watchlistItem = new WatchlistItem
            {
                AuctionId = auctionId,
                UserId = userId,
                CreatedAt = DateTime.UtcNow
            };

            _context.WatchlistItems.Add(watchlistItem);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<bool> RemoveFromWatchlistAsync(int auctionId, int userId)
        {
            var watchlistItem = await _context.WatchlistItems
                .FirstOrDefaultAsync(w => w.AuctionId == auctionId && w.UserId == userId);

            if (watchlistItem == null)
            {
                return false;
            }

            _context.WatchlistItems.Remove(watchlistItem);
            await _context.SaveChangesAsync();

            return true;
        }

        public async Task<List<AuctionListDto>> GetWatchlistAsync(int userId)
        {
            var watchlistAuctions = await _context.WatchlistItems
                .Include(w => w.Auction)
                    .ThenInclude(a => a.Seller)
                .Include(w => w.Auction)
                    .ThenInclude(a => a.Bids)
                .Where(w => w.UserId == userId)
                .Select(w => w.Auction)
                .ToListAsync();

            return watchlistAuctions.Select(MapToAuctionListDto).ToList();
        }

        private AuctionResponseDto MapToAuctionResponseDto(Auction auction)
        {
            var timeRemaining = auction.EndTime - DateTime.UtcNow;
            var status = GetAuctionStatus(auction);

            return new AuctionResponseDto
            {
                AuctionId = auction.AuctionId,
                Title = auction.Title,
                Description = auction.Description,
                StartingPrice = auction.StartingPrice,
                ReservePrice = auction.ReservePrice,
                CurrentPrice = auction.CurrentPrice,
                StartTime = auction.StartTime,
                EndTime = auction.EndTime,
                Category = auction.Category,
                Condition = auction.Condition,
                Location = auction.Location,
                ShippingInfo = auction.ShippingInfo,
                IsActive = auction.IsActive,
                IsFeatured = auction.IsFeatured,
                ViewCount = auction.ViewCount,
                CreatedAt = auction.CreatedAt,
                Seller = new SellerInfoDto
                {
                    UserId = auction.Seller.UserId,
                    FirstName = auction.Seller.FirstName,
                    LastName = auction.Seller.LastName,
                    Email = auction.Seller.Email
                },
                TotalBids = auction.Bids?.Count ?? 0,
                TimeRemaining = timeRemaining > TimeSpan.Zero ? timeRemaining : TimeSpan.Zero,
                Status = status,
                
                // New fields
                Tags = auction.Tags ?? "",
                Duration = auction.Duration,
                Shipping = auction.Shipping ?? "",
                AuctionType = auction.AuctionType ?? "Standard",
                AuthenticityGuarantee = auction.AuthenticityGuarantee,
                AcceptReturns = auction.AcceptReturns,
                PremiumListing = auction.PremiumListing,

                // Image properties
                Images = auction.Images?.Select(img => new ImageResponseDto
                {
                    ImageId = img.ImageId,
                    AuctionId = img.AuctionId,
                    ImageUrl = img.ImageUrl,
                    AltText = img.AltText,
                    IsPrimary = img.IsPrimary,
                    DisplayOrder = img.DisplayOrder,
                    CreatedAt = img.CreatedAt
                }).ToList() ?? new List<ImageResponseDto>(),
                PrimaryImageUrl = auction.PrimaryImageUrl,
                ImageUrls = auction.ImageUrls
            };
        }

        private AuctionListDto MapToAuctionListDto(Auction auction)
        {
            var timeRemaining = auction.EndTime - DateTime.UtcNow;
            var status = GetAuctionStatus(auction);

            return new AuctionListDto
            {
                AuctionId = auction.AuctionId,
                Title = auction.Title,
                StartingPrice = auction.StartingPrice,
                CurrentPrice = auction.CurrentPrice,
                StartTime = auction.StartTime,
                EndTime = auction.EndTime,
                Category = auction.Category,
                IsActive = auction.IsActive,
                IsFeatured = auction.IsFeatured,
                ViewCount = auction.ViewCount,
                TotalBids = auction.Bids?.Count ?? 0,
                TimeRemaining = timeRemaining > TimeSpan.Zero ? timeRemaining : TimeSpan.Zero,
                Status = status,
                Seller = new SellerInfoDto
                {
                    UserId = auction.Seller.UserId,
                    FirstName = auction.Seller.FirstName,
                    LastName = auction.Seller.LastName,
                    Email = auction.Seller.Email
                },
                PrimaryImageUrl = auction.PrimaryImageUrl
            };
        }

        private string GetAuctionStatus(Auction auction)
        {
            var now = DateTime.UtcNow;

            if (now < auction.StartTime)
                return "Upcoming";
            else if (now > auction.EndTime)
                return "Ended";
            else
                return "Active";
        }
    }
}