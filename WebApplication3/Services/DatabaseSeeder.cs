using Microsoft.EntityFrameworkCore;
using AuctionHouse.API.Data;
using AuctionHouse.API.Models;
using BCrypt.Net;

namespace AuctionHouse.API.Services
{
    public class DatabaseSeeder
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<DatabaseSeeder> _logger;

        public DatabaseSeeder(ApplicationDbContext context, ILogger<DatabaseSeeder> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedAsync()
        {
            try
            {
                // Ensure database is created
                await _context.Database.EnsureCreatedAsync();

                // Check if data already exists
                if (await _context.Users.AnyAsync())
                {
                    _logger.LogInformation("Database already seeded");
                    return;
                }

                _logger.LogInformation("Starting database seeding...");

                // Create sample users
                var users = await CreateSampleUsers();
                
                // Create sample auctions
                var auctions = await CreateSampleAuctions(users);
                
                // Create sample bids
                await CreateSampleBids(auctions, users);

                await _context.SaveChangesAsync();
                _logger.LogInformation("Database seeding completed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during database seeding: {ErrorMessage}", ex.Message);
                throw;
            }
        }

        private async Task<List<User>> CreateSampleUsers()
        {
            var users = new List<User>
            {
                // Sellers
                new User
                {
                    FirstName = "John",
                    LastName = "Seller",
                    Email = "john.seller@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                    AccountType = "Seller",
                    AgreeToTerms = true,
                    ReceiveUpdates = true,
                    IsActive = true
                },
                new User
                {
                    FirstName = "Sarah",
                    LastName = "Merchant",
                    Email = "sarah.merchant@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                    AccountType = "Seller",
                    AgreeToTerms = true,
                    ReceiveUpdates = false,
                    IsActive = true
                },
                // Buyers
                new User
                {
                    FirstName = "Mike",
                    LastName = "Buyer",
                    Email = "mike.buyer@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                    AccountType = "Buyer",
                    AgreeToTerms = true,
                    ReceiveUpdates = true,
                    IsActive = true
                },
                new User
                {
                    FirstName = "Lisa",
                    LastName = "Collector",
                    Email = "lisa.collector@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                    AccountType = "Buyer",
                    AgreeToTerms = true,
                    ReceiveUpdates = true,
                    IsActive = true
                },
                new User
                {
                    FirstName = "David",
                    LastName = "Bidder",
                    Email = "david.bidder@example.com",
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123"),
                    AccountType = "Buyer",
                    AgreeToTerms = true,
                    ReceiveUpdates = false,
                    IsActive = true
                }
            };

            _context.Users.AddRange(users);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created {Count} sample users", users.Count);
            return users;
        }

        private async Task<List<Auction>> CreateSampleAuctions(List<User> users)
        {
            var sellers = users.Where(u => u.AccountType == "Seller").ToList();
            var baseTime = DateTime.UtcNow;

            var auctions = new List<Auction>
            {
                new Auction
                {
                    Title = "Vintage Apple iPhone 12 Pro Max - Excellent Condition",
                    Description = "Like-new iPhone 12 Pro Max in Space Gray. Includes original box, charger, and screen protector already applied. No scratches or dents, well maintained.",
                    StartingPrice = 599.99m,
                    CurrentPrice = 599.99m,
                    ReservePrice = 650.00m,
                    StartTime = baseTime.AddDays(-2),
                    EndTime = baseTime.AddDays(5),
                    Category = "Electronics",
                    Condition = "Excellent",
                    Location = "New York, NY",
                    ShippingInfo = "Free shipping within US, $25 international",
                    SellerId = sellers[0].UserId,
                    Tags = "iPhone, Apple, Smartphone, iOS",
                    Duration = 7,
                    Shipping = "Free",
                    AuthenticityGuarantee = true,
                    AcceptReturns = true
                },
                new Auction
                {
                    Title = "Rare 1995 Pokemon Card Collection - Charizard Included",
                    Description = "Amazing collection of original 1995 Pokemon cards including holographic Charizard in near-mint condition. Perfect for collectors or Pokemon fans.",
                    StartingPrice = 299.99m,
                    CurrentPrice = 299.99m,
                    ReservePrice = 400.00m,
                    StartTime = baseTime.AddDays(-1),
                    EndTime = baseTime.AddDays(6),
                    Category = "Collectibles",
                    Condition = "Near Mint",
                    Location = "Los Angeles, CA",
                    ShippingInfo = "Insured shipping included",
                    SellerId = sellers[1].UserId,
                    Tags = "Pokemon, Cards, Charizard, Collectible, Vintage",
                    Duration = 7,
                    Shipping = "Insured",
                    AuthenticityGuarantee = true,
                    PremiumListing = true
                },
                new Auction
                {
                    Title = "Professional DSLR Camera - Canon EOS R5",
                    Description = "Canon EOS R5 mirrorless camera body only. Excellent condition with low shutter count. Perfect for professional photography and video recording.",
                    StartingPrice = 2499.99m,
                    CurrentPrice = 2499.99m,
                    ReservePrice = 2800.00m,
                    StartTime = baseTime,
                    EndTime = baseTime.AddDays(7),
                    Category = "Electronics",
                    Condition = "Like New",
                    Location = "Chicago, IL",
                    ShippingInfo = "Express shipping available",
                    SellerId = sellers[0].UserId,
                    Tags = "Canon, Camera, DSLR, Photography, Professional",
                    Duration = 7,
                    Shipping = "Express Available"
                },
                new Auction
                {
                    Title = "Vintage Leather Jacket - Genuine Italian Leather",
                    Description = "Beautiful vintage Italian leather jacket from the 1980s. Size Medium. Shows minimal wear and has been well-maintained. Classic style that never goes out of fashion.",
                    StartingPrice = 199.99m,
                    CurrentPrice = 199.99m,
                    StartTime = baseTime.AddHours(-6),
                    EndTime = baseTime.AddDays(4),
                    Category = "Fashion",
                    Condition = "Good",
                    Location = "Miami, FL",
                    ShippingInfo = "Standard shipping $15",
                    SellerId = sellers[1].UserId,
                    Tags = "Leather, Jacket, Vintage, Fashion, Italian",
                    Duration = 5,
                    Shipping = "$15",
                    AcceptReturns = true
                },
                new Auction
                {
                    Title = "Limited Edition Sneakers - Nike Air Jordan 1 Retro",
                    Description = "Brand new, unworn Nike Air Jordan 1 Retro High in Chicago colorway. Size 10. Still in original box with all tags. Perfect for collectors or sneaker enthusiasts.",
                    StartingPrice = 349.99m,
                    CurrentPrice = 349.99m,
                    ReservePrice = 450.00m,
                    StartTime = baseTime.AddHours(-12),
                    EndTime = baseTime.AddDays(3),
                    Category = "Fashion",
                    Condition = "New",
                    Location = "Atlanta, GA",
                    ShippingInfo = "Free shipping, signature required",
                    SellerId = sellers[0].UserId,
                    Tags = "Nike, Jordan, Sneakers, Basketball, Limited Edition",
                    Duration = 4,
                    Shipping = "Free",
                    AuthenticityGuarantee = true,
                    PremiumListing = true
                }
            };

            _context.Auctions.AddRange(auctions);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Created {Count} sample auctions", auctions.Count);
            return auctions;
        }

        private async Task CreateSampleBids(List<Auction> auctions, List<User> users)
        {
            var buyers = users.Where(u => u.AccountType == "Buyer").ToList();
            var bids = new List<Bid>();
            var baseTime = DateTime.UtcNow;

            // Add some bids to the first auction (iPhone)
            var iPhoneAuction = auctions[0];
            bids.AddRange(new[]
            {
                new Bid
                {
                    AuctionId = iPhoneAuction.AuctionId,
                    BidderId = buyers[0].UserId,
                    Amount = 625.00m,
                    BidTime = baseTime.AddHours(-20),
                    IsWinningBid = false
                },
                new Bid
                {
                    AuctionId = iPhoneAuction.AuctionId,
                    BidderId = buyers[1].UserId,
                    Amount = 645.00m,
                    BidTime = baseTime.AddHours(-18),
                    IsWinningBid = false
                },
                new Bid
                {
                    AuctionId = iPhoneAuction.AuctionId,
                    BidderId = buyers[2].UserId,
                    Amount = 665.00m,
                    BidTime = baseTime.AddHours(-15),
                    IsWinningBid = true
                }
            });

            // Add some bids to the Pokemon auction
            var pokemonAuction = auctions[1];
            bids.AddRange(new[]
            {
                new Bid
                {
                    AuctionId = pokemonAuction.AuctionId,
                    BidderId = buyers[1].UserId,
                    Amount = 325.00m,
                    BidTime = baseTime.AddHours(-10),
                    IsWinningBid = false
                },
                new Bid
                {
                    AuctionId = pokemonAuction.AuctionId,
                    BidderId = buyers[0].UserId,
                    Amount = 375.00m,
                    BidTime = baseTime.AddHours(-8),
                    IsWinningBid = true
                }
            });

            // Add bids to the sneaker auction
            var sneakerAuction = auctions[4];
            bids.AddRange(new[]
            {
                new Bid
                {
                    AuctionId = sneakerAuction.AuctionId,
                    BidderId = buyers[2].UserId,
                    Amount = 375.00m,
                    BidTime = baseTime.AddHours(-5),
                    IsWinningBid = false
                },
                new Bid
                {
                    AuctionId = sneakerAuction.AuctionId,
                    BidderId = buyers[0].UserId,
                    Amount = 425.00m,
                    BidTime = baseTime.AddHours(-2),
                    IsWinningBid = true
                }
            });

            _context.Bids.AddRange(bids);

            // Update auction current prices
            iPhoneAuction.CurrentPrice = 665.00m;
            pokemonAuction.CurrentPrice = 375.00m;
            sneakerAuction.CurrentPrice = 425.00m;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Created {Count} sample bids", bids.Count);
        }
    }
}