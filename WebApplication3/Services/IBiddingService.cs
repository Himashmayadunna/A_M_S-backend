using AuctionHouse.API.DTOs;

namespace AuctionHouse.API.Services
{
    public interface IBiddingService
    {
        Task<BidResponseDto> PlaceBidAsync(int auctionId, int bidderId, decimal amount);
        Task<List<BidResponseDto>> GetAuctionBidsAsync(int auctionId, int page = 1, int pageSize = 50);
        Task<List<UserBidDto>> GetUserBidsAsync(int userId, int page = 1, int pageSize = 20, string status = null);
        Task<List<WinningBidDto>> GetUserWinningBidsAsync(int userId, int page = 1, int pageSize = 20);
        Task<BidStatisticsDto> GetBidStatisticsAsync(int auctionId);
        Task<BidResponseDto> GetHighestBidAsync(int auctionId);
        Task UpdateBidWinnerStatusAsync(int auctionId);
        Task<bool> IsUserWinningBidderAsync(int auctionId, int userId);
    }
}