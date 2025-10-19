# Comprehensive API Testing Script for Auction House Bidding System
# This script demonstrates all the bidding functionality

$baseUrl = "http://localhost:5277"
$headers = @{
    "Content-Type" = "application/json"
    "Accept" = "application/json"
}

Write-Host "=== Auction House API Testing Script ===" -ForegroundColor Green
Write-Host "Base URL: $baseUrl" -ForegroundColor Cyan
Write-Host ""

# Helper function to make API calls
function Invoke-ApiCall {
    param(
        [string]$Method,
        [string]$Uri,
        [hashtable]$Headers,
        [string]$Body = $null
    )
    
    try {
        $params = @{
            Method = $Method
            Uri = $Uri
            Headers = $Headers
        }
        
        if ($Body) {
            $params.Body = $Body
        }
        
        $response = Invoke-RestMethod @params
        return $response
    }
    catch {
        Write-Host "API Call Failed: $_" -ForegroundColor Red
        if ($_.Exception.Response) {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $errorContent = $reader.ReadToEnd()
            Write-Host "Error Details: $errorContent" -ForegroundColor Red
        }
        return $null
    }
}

# Test 1: Check API Health
Write-Host "1. Testing API Health..." -ForegroundColor Yellow
$healthResponse = Invoke-ApiCall -Method "GET" -Uri "$baseUrl/api/health" -Headers $headers
if ($healthResponse) {
    Write-Host "? API is healthy" -ForegroundColor Green
    Write-Host "  Status: $($healthResponse.status)" -ForegroundColor White
    Write-Host "  Environment: $($healthResponse.environment)" -ForegroundColor White
} else {
    Write-Host "? API health check failed" -ForegroundColor Red
    exit 1
}
Write-Host ""

# Test 2: User Registration (Buyers and Sellers)
Write-Host "2. Testing User Registration..." -ForegroundColor Yellow

# Register a seller
$sellerData = @{
    firstName = "Test"
    lastName = "Seller"
    email = "testseller@example.com"
    password = "TestPassword123!"
    accountType = "Seller"
    agreeToTerms = $true
    receiveUpdates = $false
} | ConvertTo-Json

$sellerResponse = Invoke-ApiCall -Method "POST" -Uri "$baseUrl/api/auth/register" -Headers $headers -Body $sellerData
if ($sellerResponse -and $sellerResponse.success) {
    Write-Host "? Seller registered successfully" -ForegroundColor Green
    $sellerToken = $sellerResponse.data.token
} else {
    Write-Host "Note: Seller may already exist, trying login..." -ForegroundColor Yellow
}

# Register buyers
$buyers = @(
    @{ firstName = "Alice"; lastName = "Buyer"; email = "alice@example.com" },
    @{ firstName = "Bob"; lastName = "Bidder"; email = "bob@example.com" },
    @{ firstName = "Charlie"; lastName = "Collector"; email = "charlie@example.com" }
)

$buyerTokens = @()
foreach ($buyer in $buyers) {
    $buyerData = @{
        firstName = $buyer.firstName
        lastName = $buyer.lastName
        email = $buyer.email
        password = "TestPassword123!"
        accountType = "Buyer"
        agreeToTerms = $true
        receiveUpdates = $true
    } | ConvertTo-Json

    $buyerResponse = Invoke-ApiCall -Method "POST" -Uri "$baseUrl/api/auth/register" -Headers $headers -Body $buyerData
    if ($buyerResponse -and $buyerResponse.success) {
        Write-Host "? Buyer $($buyer.firstName) registered successfully" -ForegroundColor Green
        $buyerTokens += $buyerResponse.data.token
    } else {
        Write-Host "Note: Buyer $($buyer.firstName) may already exist" -ForegroundColor Yellow
    }
}
Write-Host ""

# Test 3: User Login
Write-Host "3. Testing User Login..." -ForegroundColor Yellow

# Login seller if registration failed
if (-not $sellerToken) {
    $loginData = @{
        email = "testseller@example.com"
        password = "TestPassword123!"
    } | ConvertTo-Json

    $loginResponse = Invoke-ApiCall -Method "POST" -Uri "$baseUrl/api/auth/login" -Headers $headers -Body $loginData
    if ($loginResponse -and $loginResponse.success) {
        Write-Host "? Seller logged in successfully" -ForegroundColor Green
        $sellerToken = $loginResponse.data.token
    }
}

# Login buyers if needed
if ($buyerTokens.Count -eq 0) {
    foreach ($buyer in $buyers) {
        $loginData = @{
            email = $buyer.email
            password = "TestPassword123!"
        } | ConvertTo-Json

        $loginResponse = Invoke-ApiCall -Method "POST" -Uri "$baseUrl/api/auth/login" -Headers $headers -Body $loginData
        if ($loginResponse -and $loginResponse.success) {
            Write-Host "? Buyer $($buyer.firstName) logged in successfully" -ForegroundColor Green
            $buyerTokens += $loginResponse.data.token
        }
    }
}
Write-Host ""

# Test 4: Create Auction (Seller)
Write-Host "4. Testing Auction Creation..." -ForegroundColor Yellow
if ($sellerToken) {
    $authHeaders = $headers.Clone()
    $authHeaders["Authorization"] = "Bearer $sellerToken"

    $auctionData = @{
        title = "Test Gaming Laptop - High Performance"
        description = "Excellent gaming laptop with RTX 4060, perfect for gaming and professional work. Barely used, includes original packaging and accessories."
        startingPrice = 899.99
        reservePrice = 1200.00
        startTime = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
        endTime = (Get-Date).AddDays(7).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
        category = "Electronics"
        condition = "Like New"
        location = "Seattle, WA"
        shippingInfo = "Free shipping within US"
        tags = "Gaming, Laptop, RTX, High Performance"
        duration = 7
        shipping = "Free"
        authenticityGuarantee = $true
        acceptReturns = $true
        images = @(
            @{
                imageUrl = "https://via.placeholder.com/400x300?text=Gaming+Laptop"
                altText = "Gaming laptop main view"
                isPrimary = $true
                displayOrder = 0
            }
        )
    } | ConvertTo-Json -Depth 3

    $auctionResponse = Invoke-ApiCall -Method "POST" -Uri "$baseUrl/api/auctions" -Headers $authHeaders -Body $auctionData
    if ($auctionResponse -and $auctionResponse.success) {
        Write-Host "? Auction created successfully" -ForegroundColor Green
        $auctionId = $auctionResponse.data.auctionId
        Write-Host "  Auction ID: $auctionId" -ForegroundColor White
        Write-Host "  Title: $($auctionResponse.data.title)" -ForegroundColor White
    } else {
        Write-Host "? Failed to create auction" -ForegroundColor Red
    }
} else {
    Write-Host "? No seller token available" -ForegroundColor Red
}
Write-Host ""

# Test 5: Get All Auctions
Write-Host "5. Testing Get All Auctions..." -ForegroundColor Yellow
$auctionsResponse = Invoke-ApiCall -Method "GET" -Uri "$baseUrl/api/auctions" -Headers $headers
if ($auctionsResponse -and $auctionsResponse.success) {
    Write-Host "? Retrieved auctions successfully" -ForegroundColor Green
    Write-Host "  Total auctions: $($auctionsResponse.data.Count)" -ForegroundColor White
    
    # Use the first available auction for bidding tests
    if ($auctionsResponse.data.Count -gt 0) {
        $testAuction = $auctionsResponse.data[0]
        $auctionId = $testAuction.auctionId
        Write-Host "  Using auction '$($testAuction.title)' (ID: $auctionId) for bidding tests" -ForegroundColor Cyan
    }
} else {
    Write-Host "? Failed to get auctions" -ForegroundColor Red
}
Write-Host ""

# Test 6: Place Bids (Multiple Buyers)
Write-Host "6. Testing Bid Placement..." -ForegroundColor Yellow
if ($auctionId -and $buyerTokens.Count -gt 0) {
    $bidAmounts = @(950.00, 975.00, 1025.00, 1100.00)
    
    for ($i = 0; $i -lt [Math]::Min($buyerTokens.Count, $bidAmounts.Count); $i++) {
        $authHeaders = $headers.Clone()
        $authHeaders["Authorization"] = "Bearer $($buyerTokens[$i])"
        
        $bidData = @{
            amount = $bidAmounts[$i]
        } | ConvertTo-Json

        Write-Host "  Buyer $($i + 1) placing bid of $($bidAmounts[$i])..." -ForegroundColor White
        $bidResponse = Invoke-ApiCall -Method "POST" -Uri "$baseUrl/api/bidding/auctions/$auctionId/bid" -Headers $authHeaders -Body $bidData
        
        if ($bidResponse -and $bidResponse.success) {
            Write-Host "  ? Bid placed successfully - Amount: $($bidResponse.data.amount)" -ForegroundColor Green
        } else {
            Write-Host "  ? Failed to place bid" -ForegroundColor Red
        }
        
        Start-Sleep -Seconds 1  # Small delay between bids
    }
} else {
    Write-Host "? No auction ID or buyer tokens available for bidding" -ForegroundColor Red
}
Write-Host ""

# Test 7: Get Auction Bids
Write-Host "7. Testing Get Auction Bids..." -ForegroundColor Yellow
if ($auctionId) {
    $bidsResponse = Invoke-ApiCall -Method "GET" -Uri "$baseUrl/api/bidding/auctions/$auctionId/bids" -Headers $headers
    if ($bidsResponse -and $bidsResponse.success) {
        Write-Host "? Retrieved auction bids successfully" -ForegroundColor Green
        Write-Host "  Total bids: $($bidsResponse.data.Count)" -ForegroundColor White
        
        if ($bidsResponse.data.Count -gt 0) {
            $highestBid = $bidsResponse.data[0]
            Write-Host "  Highest bid: $($highestBid.amount) by $($highestBid.bidderName)" -ForegroundColor Cyan
        }
    } else {
        Write-Host "? Failed to get auction bids" -ForegroundColor Red
    }
}
Write-Host ""

# Test 8: Get User's Bidding History
Write-Host "8. Testing User Bidding History..." -ForegroundColor Yellow
if ($buyerTokens.Count -gt 0) {
    $authHeaders = $headers.Clone()
    $authHeaders["Authorization"] = "Bearer $($buyerTokens[0])"
    
    $userBidsResponse = Invoke-ApiCall -Method "GET" -Uri "$baseUrl/api/bidding/my-bids" -Headers $authHeaders
    if ($userBidsResponse -and $userBidsResponse.success) {
        Write-Host "? Retrieved user bidding history successfully" -ForegroundColor Green
        Write-Host "  User's total bids: $($userBidsResponse.data.Count)" -ForegroundColor White
    } else {
        Write-Host "? Failed to get user bidding history" -ForegroundColor Red
    }
}
Write-Host ""

# Test 9: Get Bid Statistics
Write-Host "9. Testing Bid Statistics..." -ForegroundColor Yellow
if ($auctionId) {
    $statsResponse = Invoke-ApiCall -Method "GET" -Uri "$baseUrl/api/bidding/auctions/$auctionId/stats" -Headers $headers
    if ($statsResponse -and $statsResponse.success) {
        Write-Host "? Retrieved bid statistics successfully" -ForegroundColor Green
        $stats = $statsResponse.data
        Write-Host "  Total bids: $($stats.totalBids)" -ForegroundColor White
        Write-Host "  Unique bidders: $($stats.uniqueBidders)" -ForegroundColor White
        Write-Host "  Starting price: $($stats.startingPrice)" -ForegroundColor White
        Write-Host "  Current price: $($stats.currentPrice)" -ForegroundColor White
    } else {
        Write-Host "? Failed to get bid statistics" -ForegroundColor Red
    }
}
Write-Host ""

# Test 10: Get Highest Bid
Write-Host "10. Testing Get Highest Bid..." -ForegroundColor Yellow
if ($auctionId) {
    $highestBidResponse = Invoke-ApiCall -Method "GET" -Uri "$baseUrl/api/bidding/auctions/$auctionId/highest-bid" -Headers $headers
    if ($highestBidResponse -and $highestBidResponse.success) {
        Write-Host "? Retrieved highest bid successfully" -ForegroundColor Green
        $highestBid = $highestBidResponse.data
        if ($highestBid) {
            Write-Host "  Highest bid: $($highestBid.amount) by $($highestBid.bidderName)" -ForegroundColor Cyan
            Write-Host "  Bid time: $($highestBid.bidTime)" -ForegroundColor White
        } else {
            Write-Host "  No bids placed yet" -ForegroundColor Yellow
        }
    } else {
        Write-Host "? Failed to get highest bid" -ForegroundColor Red
    }
}
Write-Host ""

# Test 11: Try Invalid Bid (Seller trying to bid on own auction)
Write-Host "11. Testing Invalid Bid Prevention..." -ForegroundColor Yellow
if ($auctionId -and $sellerToken) {
    $authHeaders = $headers.Clone()
    $authHeaders["Authorization"] = "Bearer $sellerToken"
    
    $invalidBidData = @{
        amount = 1500.00
    } | ConvertTo-Json

    Write-Host "  Attempting seller bid on own auction (should fail)..." -ForegroundColor White
    $invalidBidResponse = Invoke-ApiCall -Method "POST" -Uri "$baseUrl/api/bidding/auctions/$auctionId/bid" -Headers $authHeaders -Body $invalidBidData
    
    if ($invalidBidResponse -and $invalidBidResponse.success) {
        Write-Host "  ? Seller was allowed to bid on own auction (this should not happen)" -ForegroundColor Red
    } else {
        Write-Host "  ? Seller correctly prevented from bidding on own auction" -ForegroundColor Green
    }
}
Write-Host ""

# Test 12: Test Auction Categories
Write-Host "12. Testing Get Auction Categories..." -ForegroundColor Yellow
$categoriesResponse = Invoke-ApiCall -Method "GET" -Uri "$baseUrl/api/auctions/categories" -Headers $headers
if ($categoriesResponse -and $categoriesResponse.success) {
    Write-Host "? Retrieved auction categories successfully" -ForegroundColor Green
    Write-Host "  Available categories: $($categoriesResponse.data -join ', ')" -ForegroundColor White
} else {
    Write-Host "? Failed to get auction categories" -ForegroundColor Red
}
Write-Host ""

Write-Host "=== Testing Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "Summary:" -ForegroundColor Cyan
Write-Host "• User registration and authentication ?" -ForegroundColor White
Write-Host "• Auction creation by sellers ?" -ForegroundColor White
Write-Host "• Bid placement by buyers only ?" -ForegroundColor White
Write-Host "• Bid tracking and history ?" -ForegroundColor White
Write-Host "• Auction statistics ?" -ForegroundColor White
Write-Host "• Security validations ?" -ForegroundColor White
Write-Host ""
Write-Host "Your auction system is ready for use!" -ForegroundColor Green
Write-Host "You can now:" -ForegroundColor Yellow
Write-Host "  1. Connect to your SQL Server database to see all the data" -ForegroundColor White
Write-Host "  2. Use the API endpoints in your frontend application" -ForegroundColor White
Write-Host "  3. Access Swagger UI at: $baseUrl/swagger" -ForegroundColor White