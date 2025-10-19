# Comprehensive Bidding Error Diagnosis Script
# This script tests each step of the bidding process to identify where it fails

$baseUrl = "http://localhost:5277"
$headers = @{
    "Content-Type" = "application/json"
    "Accept" = "application/json"
}

Write-Host "=== Comprehensive Bidding Error Diagnosis ===" -ForegroundColor Green
Write-Host "Base URL: $baseUrl" -ForegroundColor Cyan
Write-Host ""

# Function to make API calls with detailed error reporting
function Invoke-DetailedApiCall {
    param(
        [string]$Method,
        [string]$Uri,
        [hashtable]$Headers,
        [string]$Body = $null,
        [string]$Description = ""
    )
    
    Write-Host "  Testing: $Description" -ForegroundColor White
    Write-Host "    URL: $Method $Uri" -ForegroundColor Gray
    if ($Body) {
        Write-Host "    Body: $Body" -ForegroundColor Gray
    }
    
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
        Write-Host "    ? SUCCESS" -ForegroundColor Green
        return @{ Success = $true; Data = $response; Error = $null }
    }
    catch {
        Write-Host "    ? FAILED" -ForegroundColor Red
        Write-Host "    Error: $($_.Exception.Message)" -ForegroundColor Red
        
        $errorDetails = @{
            Success = $false
            Data = $null
            Error = $_.Exception.Message
            StatusCode = $null
            ResponseBody = $null
        }
        
        if ($_.Exception.Response) {
            $errorDetails.StatusCode = $_.Exception.Response.StatusCode
            try {
                $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
                $responseBody = $reader.ReadToEnd()
                $errorDetails.ResponseBody = $responseBody
                Write-Host "    Status Code: $($_.Exception.Response.StatusCode)" -ForegroundColor Yellow
                Write-Host "    Response: $responseBody" -ForegroundColor Yellow
            } catch {
                Write-Host "    Could not read response body" -ForegroundColor Yellow
            }
        }
        
        return $errorDetails
    }
}

# Step 1: Health Check
Write-Host "STEP 1: API Health Check" -ForegroundColor Yellow
$healthResult = Invoke-DetailedApiCall -Method "GET" -Uri "$baseUrl/api/health" -Headers $headers -Description "API Health Check"

if (-not $healthResult.Success) {
    Write-Host "? API is not responding. Please start the server with 'dotnet run'" -ForegroundColor Red
    exit 1
}

Write-Host ""

# Step 2: User Login
Write-Host "STEP 2: User Authentication" -ForegroundColor Yellow

$loginData = @{
    email = "mike.buyer@example.com"
    password = "password123"
} | ConvertTo-Json

$loginResult = Invoke-DetailedApiCall -Method "POST" -Uri "$baseUrl/api/auth/login" -Headers $headers -Body $loginData -Description "Buyer Login"

if (-not $loginResult.Success) {
    Write-Host "? Login failed. Trying to register a new user..." -ForegroundColor Red
    
    $registerData = @{
        firstName = "Debug"
        lastName = "Buyer"
        email = "debug.buyer.test@example.com"
        password = "password123"
        accountType = "Buyer"
        agreeToTerms = $true
        receiveUpdates = $false
    } | ConvertTo-Json
    
    $registerResult = Invoke-DetailedApiCall -Method "POST" -Uri "$baseUrl/api/auth/register" -Headers $headers -Body $registerData -Description "Register New Buyer"
    
    if ($registerResult.Success) {
        $token = $registerResult.Data.data.token
        $userId = $registerResult.Data.data.userId
        Write-Host "    New user registered - ID: $userId" -ForegroundColor Green
    } else {
        Write-Host "? Both login and registration failed. Cannot continue." -ForegroundColor Red
        exit 1
    }
} else {
    $token = $loginResult.Data.data.token
    $userId = $loginResult.Data.data.userId
    Write-Host "    User logged in - ID: $userId" -ForegroundColor Green
}

$authHeaders = $headers.Clone()
$authHeaders["Authorization"] = "Bearer $token"

Write-Host ""

# Step 3: Authentication Test
Write-Host "STEP 3: Authentication Validation" -ForegroundColor Yellow

$authTestResult = Invoke-DetailedApiCall -Method "GET" -Uri "$baseUrl/api/bidding/debug/auth-test" -Headers $authHeaders -Description "Authentication Test"

if ($authTestResult.Success) {
    $authData = $authTestResult.Data.data
    Write-Host "    Authentication Status: $($authData.isAuthenticated)" -ForegroundColor White
    Write-Host "    User Extraction: $($authData.extractionSuccessful)" -ForegroundColor White
    
    if ($authData.extractionSuccessful) {
        Write-Host "    Extracted User ID: $($authData.extractedUserId)" -ForegroundColor White
        Write-Host "    Extracted Account Type: $($authData.extractedAccountType)" -ForegroundColor White
    } else {
        Write-Host "    Extraction Error: $($authData.extractionError)" -ForegroundColor Red
    }
    
    Write-Host "    JWT Claims:" -ForegroundColor White
    foreach ($claim in $authData.claims) {
        Write-Host "      $($claim.Type): $($claim.Value)" -ForegroundColor Gray
    }
} else {
    Write-Host "? Authentication test failed. Token may be invalid." -ForegroundColor Red
}

Write-Host ""

# Step 4: Get Available Auctions
Write-Host "STEP 4: Auction Availability Check" -ForegroundColor Yellow

$auctionsResult = Invoke-DetailedApiCall -Method "GET" -Uri "$baseUrl/api/auctions" -Headers $headers -Description "Get Available Auctions"

$auctionId = $null
if ($auctionsResult.Success -and $auctionsResult.Data.data.Count -gt 0) {
    $auction = $auctionsResult.Data.data[0]
    $auctionId = $auction.auctionId
    Write-Host "    Found auction: $($auction.title)" -ForegroundColor Green
    Write-Host "    Auction ID: $auctionId" -ForegroundColor White
    Write-Host "    Current Price: $($auction.currentPrice)" -ForegroundColor White
    Write-Host "    Status: $($auction.status)" -ForegroundColor White
} else {
    Write-Host "? No auctions available. Creating a test auction..." -ForegroundColor Red
    
    # Try to create a test auction
    $sellerLoginData = @{
        email = "john.seller@example.com"
        password = "password123"
    } | ConvertTo-Json
    
    $sellerLoginResult = Invoke-DetailedApiCall -Method "POST" -Uri "$baseUrl/api/auth/login" -Headers $headers -Body $sellerLoginData -Description "Seller Login"
    
    if ($sellerLoginResult.Success) {
        $sellerToken = $sellerLoginResult.Data.data.token
        $sellerAuthHeaders = $headers.Clone()
        $sellerAuthHeaders["Authorization"] = "Bearer $sellerToken"
        
        $auctionData = @{
            title = "Test Bidding Item - Debug"
            description = "This is a test item created for debugging bidding issues"
            startingPrice = 10.00
            startTime = (Get-Date).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
            endTime = (Get-Date).AddDays(7).ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ")
            category = "Electronics"
            condition = "New"
            location = "Test Location"
            shippingInfo = "Free shipping"
        } | ConvertTo-Json
        
        $createAuctionResult = Invoke-DetailedApiCall -Method "POST" -Uri "$baseUrl/api/auctions" -Headers $sellerAuthHeaders -Body $auctionData -Description "Create Test Auction"
        
        if ($createAuctionResult.Success) {
            $auctionId = $createAuctionResult.Data.data.auctionId
            Write-Host "    Test auction created with ID: $auctionId" -ForegroundColor Green
        }
    }
}

Write-Host ""

# Step 5: Bid Placement Test
if ($auctionId) {
    Write-Host "STEP 5: Bid Placement Test" -ForegroundColor Yellow
    
    $bidAmount = 25.00
    $bidData = @{
        amount = $bidAmount
    } | ConvertTo-Json
    
    Write-Host "    Attempting to place bid of $($bidAmount) on auction $auctionId" -ForegroundColor White
    
    $bidResult = Invoke-DetailedApiCall -Method "POST" -Uri "$baseUrl/api/bidding/auctions/$auctionId/bid" -Headers $authHeaders -Body $bidData -Description "Place Bid"
    
    if ($bidResult.Success) {
        Write-Host "    ?? BID PLACED SUCCESSFULLY!" -ForegroundColor Green
        Write-Host "    Bid ID: $($bidResult.Data.data.bidId)" -ForegroundColor White
        Write-Host "    Amount: $($bidResult.Data.data.amount)" -ForegroundColor White
        Write-Host "    Bidder: $($bidResult.Data.data.bidderName)" -ForegroundColor White
    } else {
        Write-Host "    ? BID PLACEMENT FAILED" -ForegroundColor Red
        Write-Host ""
        Write-Host "    DETAILED ERROR ANALYSIS:" -ForegroundColor Yellow
        Write-Host "    Status Code: $($bidResult.StatusCode)" -ForegroundColor White
        Write-Host "    Error Message: $($bidResult.Error)" -ForegroundColor White
        
        if ($bidResult.ResponseBody) {
            try {
                $errorObj = $bidResult.ResponseBody | ConvertFrom-Json
                Write-Host "    Server Response:" -ForegroundColor White
                Write-Host "      Message: $($errorObj.message)" -ForegroundColor Gray
                if ($errorObj.errors) {
                    Write-Host "      Errors:" -ForegroundColor Gray
                    $errorObj.errors | ForEach-Object { Write-Host "        - $_" -ForegroundColor Gray }
                }
            } catch {
                Write-Host "    Raw Response Body: $($bidResult.ResponseBody)" -ForegroundColor Gray
            }
        }
    }
} else {
    Write-Host "STEP 5: SKIPPED - No auction available for testing" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== DIAGNOSIS COMPLETE ===" -ForegroundColor Green
Write-Host ""
Write-Host "SUMMARY:" -ForegroundColor Cyan
Write-Host "• API Health: $($healthResult.Success)" -ForegroundColor White
Write-Host "• User Login: $($loginResult.Success)" -ForegroundColor White
Write-Host "• Authentication: $($authTestResult.Success)" -ForegroundColor White
Write-Host "• Auction Available: $($auctionId -ne $null)" -ForegroundColor White

if ($auctionId) {
    Write-Host "• Bid Placement: $($bidResult.Success)" -ForegroundColor White
    
    if (-not $bidResult.Success) {
        Write-Host "" -ForegroundColor White
        Write-Host "NEXT STEPS TO FIX THE ISSUE:" -ForegroundColor Yellow
        Write-Host "1. Check the server console logs for detailed error messages" -ForegroundColor White
        Write-Host "2. Verify the database connection is working" -ForegroundColor White
        Write-Host "3. Check if the auction exists and is active" -ForegroundColor White
        Write-Host "4. Verify JWT token contains correct claims" -ForegroundColor White
        Write-Host "5. Run the server with detailed logging enabled" -ForegroundColor White
    }
}

Write-Host ""
Write-Host "Check your server console for detailed logs during this test." -ForegroundColor Cyan