using Microsoft.EntityFrameworkCore;
using AuctionHouse.API.Models;

namespace AuctionHouse.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Auction> Auctions { get; set; }
        public DbSet<AuctionImage> AuctionImages { get; set; }
        public DbSet<Bid> Bids { get; set; }
        public DbSet<WatchlistItem> WatchlistItems { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure User entity
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasKey(e => e.UserId);

                entity.HasIndex(e => e.Email)
                    .IsUnique();

                entity.Property(e => e.Email)
                    .IsRequired()
                    .HasMaxLength(255);

                entity.Property(e => e.FirstName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.LastName)
                    .IsRequired()
                    .HasMaxLength(100);

                entity.Property(e => e.PasswordHash)
                    .IsRequired();

                entity.Property(e => e.AccountType)
                    .IsRequired()
                    .HasMaxLength(20);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");
            });

            // Configure Auction entity
            modelBuilder.Entity<Auction>(entity =>
            {
                entity.HasKey(e => e.AuctionId);

                entity.Property(e => e.Title)
                    .IsRequired()
                    .HasMaxLength(200);

                entity.Property(e => e.Description)
                    .IsRequired()
                    .HasMaxLength(2000);

                entity.Property(e => e.StartingPrice)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.ReservePrice)
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.CurrentPrice)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.Category)
                    .IsRequired()
                    .HasMaxLength(50);

                entity.Property(e => e.Condition)
                    .HasMaxLength(100);

                entity.Property(e => e.Location)
                    .HasMaxLength(500);

                entity.Property(e => e.ShippingInfo)
                    .HasMaxLength(500);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.HasIndex(e => e.Category);
                entity.HasIndex(e => e.EndTime);
                entity.HasIndex(e => e.IsActive);

                // Foreign key relationship with User (Seller)
                entity.HasOne(a => a.Seller)
                    .WithMany(u => u.Auctions)
                    .HasForeignKey(a => a.SellerId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure AuctionImage entity
            modelBuilder.Entity<AuctionImage>(entity =>
            {
                entity.HasKey(e => e.ImageId);

                entity.Property(e => e.ImageUrl)
                    .IsRequired()
                    .HasMaxLength(500);

                entity.Property(e => e.AltText)
                    .HasMaxLength(200);

                entity.Property(e => e.IsPrimary)
                    .IsRequired()
                    .HasDefaultValue(false);

                entity.Property(e => e.DisplayOrder)
                    .IsRequired()
                    .HasDefaultValue(0);

                entity.Property(e => e.CreatedAt)
                    .IsRequired()
                    .HasDefaultValueSql("GETUTCDATE()");

                // Foreign key relationship with Auction
                entity.HasOne(ai => ai.Auction)
                    .WithMany(a => a.Images)
                    .HasForeignKey(ai => ai.AuctionId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Index for efficient queries
                entity.HasIndex(e => new { e.AuctionId, e.IsPrimary });
                entity.HasIndex(e => e.DisplayOrder);
            });

            // Configure Bid entity
            modelBuilder.Entity<Bid>(entity =>
            {
                entity.HasKey(e => e.BidId);

                entity.Property(e => e.Amount)
                    .IsRequired()
                    .HasColumnType("decimal(18,2)");

                entity.Property(e => e.BidTime)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.HasIndex(e => new { e.AuctionId, e.BidTime });
                entity.HasIndex(e => e.BidderId);

                // Foreign key relationship with Auction
                entity.HasOne(b => b.Auction)
                    .WithMany(a => a.Bids)
                    .HasForeignKey(b => b.AuctionId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Foreign key relationship with User (Bidder)
                entity.HasOne(b => b.Bidder)
                    .WithMany(u => u.Bids)
                    .HasForeignKey(b => b.BidderId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configure WatchlistItem entity
            modelBuilder.Entity<WatchlistItem>(entity =>
            {
                entity.HasKey(e => e.WatchlistId);

                entity.Property(e => e.CreatedAt)
                    .HasDefaultValueSql("GETUTCDATE()");

                entity.HasIndex(e => new { e.UserId, e.AuctionId })
                    .IsUnique();

                // Foreign key relationship with User
                entity.HasOne(w => w.User)
                    .WithMany(u => u.WatchlistItems)
                    .HasForeignKey(w => w.UserId)
                    .OnDelete(DeleteBehavior.Cascade);

                // Foreign key relationship with Auction
                entity.HasOne(w => w.Auction)
                    .WithMany(a => a.WatchlistItems)
                    .HasForeignKey(w => w.AuctionId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}