using System.ComponentModel.DataAnnotations;
using WebApplication3.DTOs;

namespace AuctionHouse.API.DTOs
{
    public class CreateAuctionDto
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "Title must be between 3 and 200 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(2000, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 2000 characters")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Starting price is required")]
        [Range(0.01, 999999.99, ErrorMessage = "Starting price must be between $0.01 and $999,999.99")]
        public decimal StartingPrice { get; set; }

        [Range(0.01, 999999.99, ErrorMessage = "Reserve price must be between $0.01 and $999,999.99")]
        public decimal? ReservePrice { get; set; }

        [Required(ErrorMessage = "Start time is required")]
        public DateTime StartTime { get; set; } = DateTime.UtcNow;

        [Required(ErrorMessage = "End time is required")]
        public DateTime EndTime { get; set; } = DateTime.UtcNow.AddDays(7);

        [Required(ErrorMessage = "Category is required")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Category must be between 2 and 50 characters")]
        public string Category { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Condition cannot exceed 100 characters")]
        public string Condition { get; set; } = "New";

        [StringLength(500, ErrorMessage = "Location cannot exceed 500 characters")]
        public string Location { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Shipping info cannot exceed 500 characters")]
        public string ShippingInfo { get; set; } = string.Empty;

        public bool IsFeatured { get; set; } = false;

        // New fields to match frontend
        [StringLength(1000, ErrorMessage = "Tags cannot exceed 1000 characters")]
        public string Tags { get; set; } = string.Empty;

        [Range(1, 30, ErrorMessage = "Duration must be between 1 and 30 days")]
        public int Duration { get; set; } = 7;

        [StringLength(100, ErrorMessage = "Shipping cost info cannot exceed 100 characters")]
        public string Shipping { get; set; } = string.Empty;

        // Optional: SellerId (can be set from token, but frontend might send it)
        public int? SellerId { get; set; }

        // Optional: Status and AuctionType from frontend
        [StringLength(20, ErrorMessage = "Status cannot exceed 20 characters")]
        public string Status { get; set; } = "Active";

        [StringLength(20, ErrorMessage = "Auction type cannot exceed 20 characters")]
        public string AuctionType { get; set; } = "Standard";

        // Features object properties
        public bool AuthenticityGuarantee { get; set; } = false;
        public bool AcceptReturns { get; set; } = false;
        public bool PremiumListing { get; set; } = false;
    }

    public class UpdateAuctionDto
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(200, MinimumLength = 3, ErrorMessage = "Title must be between 3 and 200 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(2000, MinimumLength = 10, ErrorMessage = "Description must be between 10 and 2000 characters")]
        public string Description { get; set; } = string.Empty;

        [Range(0.01, 999999.99, ErrorMessage = "Reserve price must be between $0.01 and $999,999.99")]
        public decimal? ReservePrice { get; set; }

        [Required(ErrorMessage = "End time is required")]
        public DateTime EndTime { get; set; }

        [Required(ErrorMessage = "Category is required")]
        [StringLength(50, MinimumLength = 2, ErrorMessage = "Category must be between 2 and 50 characters")]
        public string Category { get; set; } = string.Empty;

        [StringLength(100, ErrorMessage = "Condition cannot exceed 100 characters")]
        public string Condition { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Location cannot exceed 500 characters")]
        public string Location { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Shipping info cannot exceed 500 characters")]
        public string ShippingInfo { get; set; } = string.Empty;

        public bool IsFeatured { get; set; } = false;
    }

    public class AuctionResponseDto
    {
        public int AuctionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal StartingPrice { get; set; }
        public decimal? ReservePrice { get; set; }
        public decimal CurrentPrice { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Condition { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string ShippingInfo { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsFeatured { get; set; }
        public int ViewCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public SellerInfoDto Seller { get; set; } = new SellerInfoDto();
        public int TotalBids { get; set; }
        public TimeSpan TimeRemaining { get; set; }
        public string Status { get; set; } = "Active"; // "Active", "Ended", "Upcoming"

        // New fields to match frontend
        public string Tags { get; set; } = string.Empty;
        public int Duration { get; set; }
        public string Shipping { get; set; } = string.Empty;
        public string AuctionType { get; set; } = "Standard";
        public bool AuthenticityGuarantee { get; set; }
        public bool AcceptReturns { get; set; }
        public bool PremiumListing { get; set; }

        // Image properties
        public List<ImageResponseDto> Images { get; set; } = new List<ImageResponseDto>();
        public string? PrimaryImageUrl { get; set; }
        public List<string> ImageUrls { get; set; } = new List<string>();
    }

    public class SellerInfoDto
    {
        public int UserId { get; set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class AuctionListDto
    {
        public int AuctionId { get; set; }
        public string Title { get; set; } = string.Empty;
        public decimal StartingPrice { get; set; }
        public decimal CurrentPrice { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public string Category { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public bool IsFeatured { get; set; }
        public int ViewCount { get; set; }
        public int TotalBids { get; set; }
        public TimeSpan TimeRemaining { get; set; }
        public string Status { get; set; } = "Active";
        public SellerInfoDto Seller { get; set; } = new SellerInfoDto();

        // Image properties
        public string? PrimaryImageUrl { get; set; }
    }

    public class PlaceBidDto
    {
        [Required(ErrorMessage = "Bid amount is required")]
        [Range(0.01, 999999.99, ErrorMessage = "Bid amount must be between $0.01 and $999,999.99")]
        public decimal Amount { get; set; }
    }

    public class BidResponseDto
    {
        public int BidId { get; set; }
        public int AuctionId { get; set; }
        public decimal Amount { get; set; }
        public DateTime BidTime { get; set; }
        public bool IsWinningBid { get; set; }
        public string BidderName { get; set; } = string.Empty;
    }

    // New DTOs for enhanced bidding functionality
    public class UserBidDto
    {
        public int BidId { get; set; }
        public int AuctionId { get; set; }
        public string AuctionTitle { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime BidTime { get; set; }
        public bool IsWinningBid { get; set; }
        public DateTime AuctionEndTime { get; set; }
        public decimal AuctionCurrentPrice { get; set; }
        public string AuctionStatus { get; set; } = string.Empty;
    }

    public class WinningBidDto
    {
        public int BidId { get; set; }
        public int AuctionId { get; set; }
        public string AuctionTitle { get; set; } = string.Empty;
        public decimal WinningAmount { get; set; }
        public DateTime AuctionEndTime { get; set; }
        public string SellerName { get; set; } = string.Empty;
        public string SellerEmail { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;
        public string ShippingInfo { get; set; } = string.Empty;
    }

    public class BidStatisticsDto
    {
        public int AuctionId { get; set; }
        public int TotalBids { get; set; }
        public int UniqueBidders { get; set; }
        public decimal StartingPrice { get; set; }
        public decimal CurrentPrice { get; set; }
        public decimal AverageIncrease { get; set; }
        public decimal HighestBid { get; set; }
        public DateTime? LastBidTime { get; set; }
    }
}