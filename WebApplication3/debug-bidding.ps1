# Bidding Debug Script
# This script helps diagnose bidding issues step by step

$baseUrl = "http://localhost:5277"
$headers = @{
    "Content-Type" = "application/json"
    "Accept" = "application/json"
}

Write-Host "=== Bidding Debug Script ===" -ForegroundColor Green
Write-Host "Base URL: $baseUrl" -ForegroundColor Cyan
Write-Host ""

# Helper function to make API calls with better error reporting
function Invoke-ApiCall {
    param(
        [string]$Method,
        [string]$Uri,
        [hashtable]$Headers,
        [string]$Body = $null,
        [string]$Description = ""
    )
    
    Write-Host "  $Description..." -ForegroundColor White
    
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
        Write-Host "    ? Success" -ForegroundColor Green
        return $response
    }
    catch {
        Write-Host "    ? Failed: $($_.Exception.Message)" -ForegroundColor Red
        
        if ($_.Exception.Response) {
            $statusCode = $_.Exception.Response.StatusCode
            Write-Host "    Status Code: $statusCode" -ForegroundColor Yellow
            
            try {
                $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
                $errorContent = $reader.ReadToEnd()
                Write-Host "    Response Body: $errorContent" -ForegroundColor Yellow
            } catch {
                Write-Host "    Could not read response body" -ForegroundColor Yellow
            }
        }
        return $null
    }
}

# Step 1: Health Check
Write-Host "1. Testing API Health..." -ForegroundColor Yellow
$healthResponse = Invoke-ApiCall -Method "GET" -Uri "$baseUrl/api/health" -Headers $headers -Description "Health check"

if (-not $healthResponse) {
    Write-Host "? API is not responding. Please check if the server is running." -ForegroundColor Red
    exit 1
}

# Step 2: Login as a buyer
Write-Host ""
Write-Host "2. Logging in as buyer..." -ForegroundColor Yellow

$loginData = @{
    email = "mike.buyer@example.com"
    password = "password123"
} | ConvertTo-Json

$loginResponse = Invoke-ApiCall -Method "POST" -Uri "$baseUrl/api/auth/login" -Headers $headers -Body $loginData -Description "Buyer login"

if (-not $loginResponse -or -not $loginResponse.success) {
    Write-Host "? Failed to login as buyer. Trying to register..." -ForegroundColor Red
    
    # Try registering a new buyer
    $registerData = @{
        firstName = "Debug"
        lastName = "Buyer"
        email = "debug.buyer@example.com"
        password = "password123"
        accountType = "Buyer"
        agreeToTerms = $true
        receiveUpdates = $false
    } | ConvertTo-Json
    
    $registerResponse = Invoke-ApiCall -Method "POST" -Uri "$baseUrl/api/auth/register" -Headers $headers -Body $registerData -Description "Register debug buyer"
    
    if ($registerResponse -and $registerResponse.success) {
        $buyerToken = $registerResponse.data.token
        Write-Host "    ? New buyer registered and logged in" -ForegroundColor Green
    } else {
        Write-Host "? Failed to register buyer. Cannot continue." -ForegroundColor Red
        exit 1
    }
} else {
    $buyerToken = $loginResponse.data.token
    Write-Host "    ? Buyer logged in successfully" -ForegroundColor Green
    Write-Host "    User: $($loginResponse.data.firstName) $($loginResponse.data.lastName)" -ForegroundColor White
    Write-Host "    Account Type: $($loginResponse.data.accountType)" -ForegroundColor White
}

# Step 3: Check JWT token claims
Write-Host ""
Write-Host "3. Checking JWT token claims..." -ForegroundColor Yellow

$authHeaders = $headers.Clone()
$authHeaders["Authorization"] = "Bearer $buyerToken"

$claimsResponse = Invoke-ApiCall -Method "GET" -Uri "$baseUrl/api/bidding/debug/token-claims" -Headers $authHeaders -Description "Check JWT token claims"

if ($claimsResponse -and $claimsResponse.success) {
    Write-Host "    Token Claims:" -ForegroundColor Cyan
    foreach ($claim in $claimsResponse.data.claims) {
        Write-Host "      $($claim.type): $($claim.value)" -ForegroundColor White
    }
    Write-Host "    UserID Methods:" -ForegroundColor Cyan
    Write-Host "      NameIdentifier: $($claimsResponse.data.userIdFromNameIdentifier)" -ForegroundColor White
    Write-Host "      Sub: $($claimsResponse.data.userIdFromSub)" -ForegroundColor White
    Write-Host "      AccountType: $($claimsResponse.data.accountType)" -ForegroundColor White
}

# Step 4: Get available auctions
Write-Host ""
Write-Host "4. Getting available auctions..." -ForegroundColor Yellow

$auctionsResponse = Invoke-ApiCall -Method "GET" -Uri "$baseUrl/api/auctions" -Headers $headers -Description "Get auctions"

if (-not $auctionsResponse -or -not $auctionsResponse.success -or $auctionsResponse.data.Count -eq 0) {
    Write-Host "? No auctions available. Creating a test auction..." -ForegroundColor Red
    
    # Login as seller first
    $sellerLoginData = @{
        email = "john.seller@example.com"
        password = "password123"
    } | ConvertTo-Json
    
    $sellerLoginResponse = Invoke-ApiCall -Method "POST" -Uri "$baseUrl/api/auth/login" -Headers $headers -Body $sellerLoginData -Description "Seller login"
    
    if ($sellerLoginResponse -and $sellerLoginResponse.success) {
        $sellerToken = $sellerLoginResponse.data.token
        $sellerAuthHeaders = $headers.Clone()
        $sellerAuthHeaders["Authorization"] = "Bearer $sellerToken"
        
        # Create test auction
        $auctionData = @{
            title = "Debug Test Item"
            description = "This is a test item for debugging bidding issues"
            startingPrice = 10.00
            startTime = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
            endTime = (Get-Date).AddDays(7).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
            category = "Electronics"
            condition = "New"
            location = "Test Location"
            shippingInfo = "Free shipping"
        } | ConvertTo-Json
        
        $createAuctionResponse = Invoke-ApiCall -Method "POST" -Uri "$baseUrl/api/auctions" -Headers $sellerAuthHeaders -Body $auctionData -Description "Create test auction"
        
        if ($createAuctionResponse -and $createAuctionResponse.success) {
            $testAuctionId = $createAuctionResponse.data.auctionId
            Write-Host "    ? Test auction created with ID: $testAuctionId" -ForegroundColor Green
        }
    }
} else {
    $testAuctionId = $auctionsResponse.data[0].auctionId
    Write-Host "    ? Found auction to test with ID: $testAuctionId" -ForegroundColor Green
    Write-Host "    Title: $($auctionsResponse.data[0].title)" -ForegroundColor White
    Write-Host "    Current Price: $($auctionsResponse.data[0].currentPrice)" -ForegroundColor White
}

# Step 5: Attempt to place a bid
if ($testAuctionId) {
    Write-Host ""
    Write-Host "5. Attempting to place a bid..." -ForegroundColor Yellow
    
    $bidAmount = 25.00
    $bidData = @{
        amount = $bidAmount
    } | ConvertTo-Json
    
    Write-Host "    Placing bid of $($bidAmount) on auction $testAuctionId" -ForegroundColor White
    
    $bidResponse = Invoke-ApiCall -Method "POST" -Uri "$baseUrl/api/bidding/auctions/$testAuctionId/bid" -Headers $authHeaders -Body $bidData -Description "Place bid"
    
    if ($bidResponse -and $bidResponse.success) {
        Write-Host "    ? Bid placed successfully!" -ForegroundColor Green
        Write-Host "    Bid ID: $($bidResponse.data.bidId)" -ForegroundColor White
        Write-Host "    Amount: $($bidResponse.data.amount)" -ForegroundColor White
        Write-Host "    Bidder: $($bidResponse.data.bidderName)" -ForegroundColor White
    } else {
        Write-Host "    ? Bid placement failed" -ForegroundColor Red
    }
} else {
    Write-Host ""
    Write-Host "? No auction available for testing" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== Debug Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "If bidding still fails after this test, check:" -ForegroundColor Yellow
Write-Host "• Database connection is working" -ForegroundColor White
Write-Host "• JWT token contains correct claims" -ForegroundColor White
Write-Host "• User is properly authenticated as a Buyer" -ForegroundColor White
Write-Host "• Auction exists and is active" -ForegroundColor White
Write-Host "• Server logs in Visual Studio or console for detailed errors" -ForegroundColor White