# PowerShell Script to Test Auction Creation API
# Run this script to test your auction creation functionality

# Configuration
$baseUrl = "http://localhost:5000"

Write-Host "=== Auction House API Testing Script ===" -ForegroundColor Green
Write-Host ""

# Test 1: Health Check
Write-Host "1. Testing API Health..." -ForegroundColor Yellow
try {
    $healthResponse = Invoke-RestMethod -Uri "$baseUrl/api/health" -Method GET
    Write-Host "? API Health: $($healthResponse.status)" -ForegroundColor Green
    Write-Host "   Environment: $($healthResponse.environment)" -ForegroundColor Cyan
} catch {
    Write-Host "? API Health Check Failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   Make sure your API is running on $baseUrl" -ForegroundColor Yellow
    exit 1
}

Write-Host ""

# Test 2: Database Connection
Write-Host "2. Testing Database Connection..." -ForegroundColor Yellow
try {
    $dbResponse = Invoke-RestMethod -Uri "$baseUrl/api/debug/db-test" -Method GET
    Write-Host "? Database Connected: $($dbResponse.canConnect)" -ForegroundColor Green
    Write-Host "   Users: $($dbResponse.userCount), Auctions: $($dbResponse.auctionCount)" -ForegroundColor Cyan
} catch {
    Write-Host "? Database Connection Failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Prompt for login credentials
Write-Host "3. Testing Authentication..." -ForegroundColor Yellow
$email = Read-Host "Enter your seller email address"
$password = Read-Host "Enter your password" -AsSecureString
$passwordText = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($password))

# Test 3: Login
try {
    $loginData = @{
        email = $email
        password = $passwordText
    }
    
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method POST -Body ($loginData | ConvertTo-Json) -ContentType "application/json"
    
    Write-Host "? Login Successful" -ForegroundColor Green
    Write-Host "   User: $($loginResponse.data.firstName) $($loginResponse.data.lastName)" -ForegroundColor Cyan
    Write-Host "   Account Type: $($loginResponse.data.accountType)" -ForegroundColor Cyan
    
    $token = $loginResponse.data.token
    
    if ($loginResponse.data.accountType -ne "Seller") {
        Write-Host "??  Warning: Account type is not 'Seller'. You may not be able to create auctions." -ForegroundColor Yellow
    }
    
} catch {
    Write-Host "? Login Failed: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host "   Please check your credentials and try again." -ForegroundColor Yellow
    exit 1
}

Write-Host ""

# Test 4: User Info Verification
Write-Host "4. Verifying User Token..." -ForegroundColor Yellow
try {
    $headers = @{
        "Authorization" = "Bearer $token"
        "Content-Type" = "application/json"
    }
    
    $userInfoResponse = Invoke-RestMethod -Uri "$baseUrl/api/debug/user-info" -Method GET -Headers $headers
    Write-Host "? Token Valid" -ForegroundColor Green
    Write-Host "   User ID: $($userInfoResponse.user.userId)" -ForegroundColor Cyan
    Write-Host "   Account Type: $($userInfoResponse.user.accountType)" -ForegroundColor Cyan
} catch {
    Write-Host "? Token Verification Failed: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""

# Test 5: Create Test Auction
Write-Host "5. Creating Test Auction..." -ForegroundColor Yellow
try {
    $testAuctionResponse = Invoke-RestMethod -Uri "$baseUrl/api/debug/test-auction" -Method POST -Headers $headers
    Write-Host "? Test Auction Created Successfully" -ForegroundColor Green
    Write-Host "   Auction ID: $($testAuctionResponse.auctionId)" -ForegroundColor Cyan
} catch {
    Write-Host "? Test Auction Creation Failed: $($_.Exception.Message)" -ForegroundColor Red
    if ($_.Exception.Response) {
        $errorResponse = $_.Exception.Response.GetResponseStream()
        $reader = New-Object System.IO.StreamReader($errorResponse)
        $errorBody = $reader.ReadToEnd()
        Write-Host "   Error Details: $errorBody" -ForegroundColor Red
    }
}

Write-Host ""

# Test 6: Create Real Auction
Write-Host "6. Creating Real Auction..." -ForegroundColor Yellow
$createRealAuction = Read-Host "Do you want to create a real auction? (y/n)"

if ($createRealAuction -eq "y" -or $createRealAuction -eq "Y") {
    $auctionTitle = Read-Host "Enter auction title"
    $auctionDescription = Read-Host "Enter auction description"
    $startingPrice = Read-Host "Enter starting price (e.g., 10.99)"
    $category = Read-Host "Enter category (e.g., Electronics)"
    
    $auctionData = @{
        title = $auctionTitle
        description = $auctionDescription
        startingPrice = [decimal]$startingPrice
        startTime = (Get-Date).AddHours(1).ToString("yyyy-MM-ddTHH:mm:ssZ")
        endTime = (Get-Date).AddDays(7).ToString("yyyy-MM-ddTHH:mm:ssZ")
        category = $category
        condition = "New"
        location = "Test Location"
        shippingInfo = "Standard shipping available"
        isFeatured = $false
        images = @()
    }
    
    try {
        $auctionResponse = Invoke-RestMethod -Uri "$baseUrl/api/auctions" -Method POST -Body ($auctionData | ConvertTo-Json) -Headers $headers
        Write-Host "? Real Auction Created Successfully!" -ForegroundColor Green
        Write-Host "   Auction ID: $($auctionResponse.data.auctionId)" -ForegroundColor Cyan
        Write-Host "   Title: $($auctionResponse.data.title)" -ForegroundColor Cyan
        Write-Host "   Starting Price: `$$($auctionResponse.data.startingPrice)" -ForegroundColor Cyan
    } catch {
        Write-Host "? Real Auction Creation Failed: $($_.Exception.Message)" -ForegroundColor Red
        if ($_.Exception.Response) {
            $errorResponse = $_.Exception.Response.GetResponseStream()
            $reader = New-Object System.IO.StreamReader($errorResponse)
            $errorBody = $reader.ReadToEnd()
            Write-Host "   Error Details: $errorBody" -ForegroundColor Red
        }
    }
}

Write-Host ""

# Test 7: List Recent Auctions
Write-Host "7. Fetching Recent Auctions..." -ForegroundColor Yellow
try {
    $recentResponse = Invoke-RestMethod -Uri "$baseUrl/api/debug/recent-auctions" -Method GET
    Write-Host "? Recent Auctions Retrieved" -ForegroundColor Green
    
    if ($recentResponse.data.Count -gt 0) {
        Write-Host "   Found $($recentResponse.data.Count) auction(s):" -ForegroundColor Cyan
        foreach ($auction in $recentResponse.data) {
            Write-Host "   - ID: $($auction.auctionId), Title: $($auction.title), Created: $($auction.createdAt)" -ForegroundColor White
        }
    } else {
        Write-Host "   No auctions found in database" -ForegroundColor Yellow
    }
} catch {
    Write-Host "? Failed to retrieve recent auctions: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== Testing Complete ===" -ForegroundColor Green
Write-Host "If all tests passed, your auction creation functionality should be working!" -ForegroundColor Green
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Test from your frontend application" -ForegroundColor White
Write-Host "2. Check the TESTING_GUIDE.md file for more detailed examples" -ForegroundColor White
Write-Host "3. Use browser developer tools to debug any frontend issues" -ForegroundColor White

Read-Host "Press Enter to exit"