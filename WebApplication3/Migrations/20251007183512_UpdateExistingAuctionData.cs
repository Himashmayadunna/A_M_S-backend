using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace WebApplication3.Migrations
{
    /// <inheritdoc />
    public partial class UpdateExistingAuctionData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Update existing records that have empty or default values
            migrationBuilder.Sql(@"
                UPDATE Auctions 
                SET Status = 'Active' 
                WHERE Status = '' OR Status IS NULL
            ");

            migrationBuilder.Sql(@"
                UPDATE Auctions 
                SET AuctionType = 'Standard' 
                WHERE AuctionType = '' OR AuctionType IS NULL
            ");

            migrationBuilder.Sql(@"
                UPDATE Auctions 
                SET Duration = 7 
                WHERE Duration = 0
            ");

            migrationBuilder.Sql(@"
                UPDATE Auctions 
                SET Tags = '' 
                WHERE Tags IS NULL
            ");

            migrationBuilder.Sql(@"
                UPDATE Auctions 
                SET Shipping = '' 
                WHERE Shipping IS NULL
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // No rollback needed for data updates
        }
    }
}
