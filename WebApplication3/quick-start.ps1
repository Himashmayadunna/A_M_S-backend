# Quick Start Script for Auction House API
# This script sets up everything you need to get started

Write-Host "=== Auction House API Quick Start ===" -ForegroundColor Green
Write-Host ""

# Check if we're in the correct directory
if (-not (Test-Path "WebApplication3.csproj")) {
    Write-Host "Error: Please run this script from the WebApplication3 project directory." -ForegroundColor Red
    Write-Host "Expected to find WebApplication3.csproj in current directory." -ForegroundColor Yellow
    exit 1
}

Write-Host "Step 1: Restoring NuGet packages..." -ForegroundColor Yellow
try {
    dotnet restore
    Write-Host "? NuGet packages restored successfully" -ForegroundColor Green
} catch {
    Write-Host "? Error restoring packages: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Step 2: Setting up database..." -ForegroundColor Yellow

# Clean previous migrations if they exist
if (Test-Path "Migrations") {
    Write-Host "  Cleaning previous migrations..." -ForegroundColor White
    Remove-Item -Recurse -Force "Migrations"
}

# Add new migration
try {
    Write-Host "  Adding new migration..." -ForegroundColor White
    dotnet ef migrations add InitialCreateComplete
    Write-Host "? Migration added successfully" -ForegroundColor Green
} catch {
    Write-Host "? Error adding migration: $_" -ForegroundColor Red
    exit 1
}

# Update database
try {
    Write-Host "  Updating database..." -ForegroundColor White
    dotnet ef database update
    Write-Host "? Database updated successfully" -ForegroundColor Green
} catch {
    Write-Host "? Error updating database: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "Step 3: Building application..." -ForegroundColor Yellow
try {
    dotnet build
    Write-Host "? Application built successfully" -ForegroundColor Green
} catch {
    Write-Host "? Error building application: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "=== Setup Complete! ===" -ForegroundColor Green
Write-Host ""
Write-Host "?? Your Auction House API is ready to use!" -ForegroundColor Cyan
Write-Host ""
Write-Host "What's been created:" -ForegroundColor Yellow
Write-Host "? SQL Server database 'AuctionHouseDB'" -ForegroundColor White
Write-Host "? Complete table structure with relationships" -ForegroundColor White
Write-Host "? Sample data (users, auctions, bids)" -ForegroundColor White
Write-Host "? RESTful API endpoints" -ForegroundColor White
Write-Host "? JWT authentication system" -ForegroundColor White
Write-Host "? Role-based access control" -ForegroundColor White
Write-Host ""
Write-Host "To start the API server:" -ForegroundColor Cyan
Write-Host "  dotnet run" -ForegroundColor White
Write-Host ""
Write-Host "Once running, you can access:" -ForegroundColor Cyan
Write-Host "  API: http://localhost:5277" -ForegroundColor White
Write-Host "  Swagger UI: http://localhost:5277/swagger" -ForegroundColor White
Write-Host ""
Write-Host "To test the complete system:" -ForegroundColor Cyan
Write-Host "  .\test-bidding-system.ps1" -ForegroundColor White
Write-Host ""
Write-Host "Database Connection String:" -ForegroundColor Cyan
Write-Host "  Server=localhost;Database=AuctionHouseDB;Integrated Security=true;TrustServerCertificate=true;" -ForegroundColor White
Write-Host ""
Write-Host "Sample User Accounts:" -ForegroundColor Cyan
Write-Host "  Sellers:" -ForegroundColor Yellow
Write-Host "    john.seller@example.com / Password123!" -ForegroundColor White
Write-Host "    sarah.merchant@example.com / Password123!" -ForegroundColor White
Write-Host "  Buyers:" -ForegroundColor Yellow  
Write-Host "    mike.buyer@example.com / Password123!" -ForegroundColor White
Write-Host "    lisa.collector@example.com / Password123!" -ForegroundColor White
Write-Host "    david.bidder@example.com / Password123!" -ForegroundColor White
Write-Host ""
Write-Host "Key Features:" -ForegroundColor Cyan
Write-Host "• Only buyers can place bids on auctions" -ForegroundColor White
Write-Host "• Only sellers can create auctions" -ForegroundColor White
Write-Host "• Real-time bid tracking and validation" -ForegroundColor White
Write-Host "• Comprehensive bidding history" -ForegroundColor White
Write-Host "• Secure JWT authentication" -ForegroundColor White
Write-Host "• Complete SQL Server integration" -ForegroundColor White
Write-Host ""
Write-Host "Ready to start building your auction system! ??" -ForegroundColor Green