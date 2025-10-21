using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace AuctionHouse.API.Models
{
    public class Auction
    {
        [Key]
        public int AuctionId { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; }

        [Required]
        [StringLength(2000)]
        public string Description { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal StartingPrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? ReservePrice { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal CurrentPrice { get; set; }

        [Required]
        public DateTime StartTime { get; set; }

        [Required]
        public DateTime EndTime { get; set; }

        [Required]
        [StringLength(50)]
        public string Category { get; set; }

        [StringLength(100)]
        public string Condition { get; set; }

        [StringLength(500)]
        public string Location { get; set; }

        [StringLength(500)]
        public string ShippingInfo { get; set; }

        public bool IsActive { get; set; } = true;

        public bool IsFeatured { get; set; } = false;

        public int ViewCount { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? UpdatedAt { get; set; }

        // Foreign Key
        [Required]
        public int SellerId { get; set; }

        // New fields to match frontend
        [StringLength(1000)]
        public string Tags { get; set; } = string.Empty;

        public int Duration { get; set; } = 7;

        [StringLength(100)]
        public string Shipping { get; set; } = string.Empty;

        [StringLength(20)]
        public string Status { get; set; } = "Active";

        [StringLength(20)]
        public string AuctionType { get; set; } = "Standard";

        // Feature flags
        public bool AuthenticityGuarantee { get; set; } = false;
        public bool AcceptReturns { get; set; } = false;
        public bool PremiumListing { get; set; } = false;

    // Navigation Properties
    [ForeignKey("SellerId")]
    public User Seller { get; set; }

    public ICollection<Bid> Bids { get; set; } = new List<Bid>();
    public ICollection<WatchlistItem> WatchlistItems { get; set; } = new List<WatchlistItem>();
    public ICollection<AuctionImage> Images { get; set; } = new List<AuctionImage>();

    // Computed Properties
    [NotMapped]
    public string? PrimaryImageUrl => Images?.FirstOrDefault(i => i.IsPrimary)?.ImageUrl 
                                      ?? Images?.OrderBy(i => i.DisplayOrder).FirstOrDefault()?.ImageUrl;

    [NotMapped]
    public List<string> ImageUrls => Images?.OrderBy(i => i.DisplayOrder)
                                            .Select(i => i.ImageUrl)
                                            .ToList() ?? new List<string>();
}

    public class AuctionImage
    {
        [Key]
        public int ImageId { get; set; }

        [Required]
        public int AuctionId { get; set; }

        [Required]
        [StringLength(500)]
        public string ImageUrl { get; set; } = string.Empty;

        [StringLength(200)]
        public string? AltText { get; set; }

        public bool IsPrimary { get; set; } = false;

        public int DisplayOrder { get; set; } = 0;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Property
        [ForeignKey("AuctionId")]
        public Auction Auction { get; set; } = null!;
    }

    public class Bid
    {
        [Key]
        public int BidId { get; set; }

        [Required]
        public int AuctionId { get; set; }

        [Required]
        public int BidderId { get; set; }

        [Required]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        public DateTime BidTime { get; set; } = DateTime.UtcNow;

        public bool IsWinningBid { get; set; } = false;

        // Navigation Properties
        [ForeignKey("AuctionId")]
        public Auction Auction { get; set; }

        [ForeignKey("BidderId")]
        public User Bidder { get; set; }
    }

    public class WatchlistItem
    {
        [Key]
        public int WatchlistId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int AuctionId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        [ForeignKey("UserId")]
        public User User { get; set; }

        [ForeignKey("AuctionId")]
        public Auction Auction { get; set; }
    }
}