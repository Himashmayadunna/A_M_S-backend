# Quick Fix and Test Script
# This script helps identify and potentially fix the bidding error

Write-Host "=== Quick Bidding Fix and Test ===" -ForegroundColor Green
Write-Host ""

# Step 1: Check if server is running
Write-Host "1. Checking if server is running..." -ForegroundColor Yellow

try {
    $healthResponse = Invoke-RestMethod -Uri "http://localhost:5277/api/health" -Method GET -TimeoutSec 5
    if ($healthResponse.status -eq "healthy") {
        Write-Host "   ? Server is running" -ForegroundColor Green
    } else {
        Write-Host "   ? Server responded but status is not healthy" -ForegroundColor Red
    }
} catch {
    Write-Host "   ? Server is not running" -ForegroundColor Red
    Write-Host ""
    Write-Host "Please start the server first:" -ForegroundColor Yellow
    Write-Host "   cd WebApplication3" -ForegroundColor White
    Write-Host "   dotnet run" -ForegroundColor White
    Write-Host ""
    Write-Host "Then run this script again." -ForegroundColor Yellow
    Write-Host ""
    Write-Host "Press any key to exit..." -ForegroundColor Gray
    $null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")
    exit 1
}

Write-Host ""
Write-Host "2. Running comprehensive diagnosis..." -ForegroundColor Yellow
Write-Host ""

# Run the comprehensive diagnosis
& ".\comprehensive-bidding-diagnosis.ps1"

Write-Host ""
Write-Host "=== QUICK SOLUTIONS ===" -ForegroundColor Cyan
Write-Host ""

Write-Host "If bidding is still failing, try these solutions:" -ForegroundColor Yellow
Write-Host ""

Write-Host "SOLUTION 1: Check JWT Token Format" -ForegroundColor White
Write-Host "• Test: GET http://localhost:5277/api/bidding/debug/auth-test" -ForegroundColor Gray
Write-Host "• This will show you all JWT claims and user extraction status" -ForegroundColor Gray
Write-Host ""

Write-Host "SOLUTION 2: Database Issues" -ForegroundColor White
Write-Host "• Run: .\fix-database-connection.ps1" -ForegroundColor Gray
Write-Host "• This will fix common database connection problems" -ForegroundColor Gray
Write-Host ""

Write-Host "SOLUTION 3: Check Server Logs" -ForegroundColor White
Write-Host "• Look at the console where 'dotnet run' is running" -ForegroundColor Gray
Write-Host "• Enhanced logging is now enabled for detailed error information" -ForegroundColor Gray
Write-Host ""

Write-Host "SOLUTION 4: Manual Test Commands" -ForegroundColor White
Write-Host "# Login as buyer" -ForegroundColor Gray
Write-Host 'curl -X POST http://localhost:5277/api/auth/login -H "Content-Type: application/json" -d "{\"email\":\"mike.buyer@example.com\",\"password\":\"password123\"}"' -ForegroundColor Gray
Write-Host ""
Write-Host "# Place bid (replace TOKEN and AUCTION_ID)" -ForegroundColor Gray
Write-Host 'curl -X POST http://localhost:5277/api/bidding/auctions/1/bid -H "Authorization: Bearer TOKEN" -H "Content-Type: application/json" -d "{\"amount\":25.00}"' -ForegroundColor Gray
Write-Host ""

Write-Host "SOLUTION 5: Reset and Restart" -ForegroundColor White
Write-Host "• Stop the server (Ctrl+C)" -ForegroundColor Gray
Write-Host "• Run: dotnet ef database drop --force" -ForegroundColor Gray
Write-Host "• Run: dotnet ef database update" -ForegroundColor Gray
Write-Host "• Run: dotnet run" -ForegroundColor Gray
Write-Host ""

Write-Host "=== MOST COMMON ISSUES ===" -ForegroundColor Yellow
Write-Host ""
Write-Host "? JWT Token Claims Missing" -ForegroundColor Red
Write-Host "   Solution: The JWT service now includes multiple claim types" -ForegroundColor White
Write-Host ""
Write-Host "? Database Connection Issues" -ForegroundColor Red
Write-Host "   Solution: Run .\fix-database-connection.ps1" -ForegroundColor White
Write-Host ""
Write-Host "? Account Type Validation" -ForegroundColor Red
Write-Host "   Solution: Ensure you're logged in as a 'Buyer' account" -ForegroundColor White
Write-Host ""
Write-Host "? Auction Not Found" -ForegroundColor Red
Write-Host "   Solution: Ensure the auction exists and is active" -ForegroundColor White
Write-Host ""

Write-Host "Press any key to exit..." -ForegroundColor Gray
$null = $Host.UI.RawUI.ReadKey("NoEcho,IncludeKeyDown")