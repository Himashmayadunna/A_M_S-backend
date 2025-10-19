# Database Setup and Seeding Script for Auction House API

Write-Host "=== Auction House Database Setup ===" -ForegroundColor Green

# Check if we're in the correct directory
if (-not (Test-Path "WebApplication3.csproj")) {
    Write-Host "Error: Please run this script from the WebApplication3 project directory." -ForegroundColor Red
    exit 1
}

Write-Host "1. Cleaning previous migration files..." -ForegroundColor Yellow
if (Test-Path "Migrations") {
    Remove-Item -Recurse -Force "Migrations"
    Write-Host "   Previous migrations cleaned." -ForegroundColor Green
}

Write-Host "2. Adding initial migration..." -ForegroundColor Yellow
try {
    dotnet ef migrations add InitialMigration
    Write-Host "   Migration added successfully." -ForegroundColor Green
} catch {
    Write-Host "   Error adding migration: $_" -ForegroundColor Red
    exit 1
}

Write-Host "3. Updating database..." -ForegroundColor Yellow
try {
    dotnet ef database update
    Write-Host "   Database updated successfully." -ForegroundColor Green
} catch {
    Write-Host "   Error updating database: $_" -ForegroundColor Red
    exit 1
}

Write-Host "4. Building the application..." -ForegroundColor Yellow
try {
    dotnet build
    Write-Host "   Application built successfully." -ForegroundColor Green
} catch {
    Write-Host "   Error building application: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "=== Database Setup Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "Your database 'AuctionHouseDB' has been created with the following tables:" -ForegroundColor Cyan
Write-Host "  - Users (for buyers and sellers)" -ForegroundColor White
Write-Host "  - Auctions (auction listings)" -ForegroundColor White
Write-Host "  - AuctionImages (auction photos)" -ForegroundColor White
Write-Host "  - Bids (bidding records)" -ForegroundColor White
Write-Host "  - WatchlistItems (user watchlists)" -ForegroundColor White
Write-Host ""
Write-Host "To seed the database with sample data, run the application and it will auto-seed on first startup." -ForegroundColor Yellow
Write-Host ""
Write-Host "Connection String: Server=localhost;Database=AuctionHouseDB;Integrated Security=true;TrustServerCertificate=true;" -ForegroundColor Cyan
Write-Host ""
Write-Host "You can now:" -ForegroundColor Yellow
Write-Host "  1. Open SQL Server Management Studio" -ForegroundColor White
Write-Host "  2. Connect to localhost" -ForegroundColor White
Write-Host "  3. Find the 'AuctionHouseDB' database" -ForegroundColor White
Write-Host "  4. Explore the tables and data" -ForegroundColor White
Write-Host ""
Write-Host "To start the API server, run: dotnet run" -ForegroundColor Green