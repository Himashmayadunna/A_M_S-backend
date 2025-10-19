# Quick Bidding Fix Test
# This script tests if the bidding issue is resolved

$baseUrl = "http://localhost:5277"

Write-Host "=== Quick Bidding Fix Test ===" -ForegroundColor Green
Write-Host ""

try {
    # 1. Login as buyer
    Write-Host "1. Logging in as buyer..." -ForegroundColor Yellow
    
    $loginData = @{
        email = "mike.buyer@example.com"
        password = "password123"
    } | ConvertTo-Json
    
    $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method POST -Body $loginData -ContentType "application/json"
    
    if ($loginResponse.success) {
        Write-Host "   ? Login successful!" -ForegroundColor Green
        $token = $loginResponse.data.token
        $headers = @{
            "Authorization" = "Bearer $token"
            "Content-Type" = "application/json"
        }
    } else {
        throw "Login failed"
    }

    # 2. Check token claims
    Write-Host ""
    Write-Host "2. Checking JWT token claims..." -ForegroundColor Yellow
    
    $claimsResponse = Invoke-RestMethod -Uri "$baseUrl/api/bidding/debug/token-claims" -Method GET -Headers $headers
    
    if ($claimsResponse.success) {
        Write-Host "   ? Token claims retrieved successfully!" -ForegroundColor Green
        Write-Host "   Account Type: $($claimsResponse.data.accountType)" -ForegroundColor White
        
        # Show user ID extraction methods
        $userIdFound = $false
        if ($claimsResponse.data.userIdFromNameIdentifier) {
            Write-Host "   User ID (NameIdentifier): $($claimsResponse.data.userIdFromNameIdentifier)" -ForegroundColor White
            $userIdFound = $true
        }
        if ($claimsResponse.data.userIdFromSub) {
            Write-Host "   User ID (Sub): $($claimsResponse.data.userIdFromSub)" -ForegroundColor White
            $userIdFound = $true
        }
        
        if (-not $userIdFound) {
            Write-Host "   ?? No User ID found in token!" -ForegroundColor Yellow
        }
    }

    # 3. Get auctions
    Write-Host ""
    Write-Host "3. Getting available auctions..." -ForegroundColor Yellow
    
    $auctionsResponse = Invoke-RestMethod -Uri "$baseUrl/api/auctions" -Method GET -ContentType "application/json"
    
    if ($auctionsResponse.success -and $auctionsResponse.data.Count -gt 0) {
        $auction = $auctionsResponse.data[0]
        Write-Host "   ? Found auction: $($auction.title)" -ForegroundColor Green
        Write-Host "   Current Price: $($auction.currentPrice)" -ForegroundColor White
        $auctionId = $auction.auctionId
    } else {
        throw "No auctions available"
    }

    # 4. Place a bid
    Write-Host ""
    Write-Host "4. Placing a bid..." -ForegroundColor Yellow
    
    $bidAmount = [math]::Round($auction.currentPrice + 5.00, 2)
    $bidData = @{
        amount = $bidAmount
    } | ConvertTo-Json
    
    Write-Host "   Bidding $($bidAmount) on auction $auctionId" -ForegroundColor White
    
    $bidResponse = Invoke-RestMethod -Uri "$baseUrl/api/bidding/auctions/$auctionId/bid" -Method POST -Body $bidData -Headers $headers
    
    if ($bidResponse.success) {
        Write-Host "   ? BID PLACED SUCCESSFULLY!" -ForegroundColor Green
        Write-Host "   Bid ID: $($bidResponse.data.bidId)" -ForegroundColor White
        Write-Host "   Amount: $($bidResponse.data.amount)" -ForegroundColor White
        Write-Host "   Bidder: $($bidResponse.data.bidderName)" -ForegroundColor White
        Write-Host "   Is Winning: $($bidResponse.data.isWinningBid)" -ForegroundColor White
    } else {
        Write-Host "   ? Bid failed: $($bidResponse.message)" -ForegroundColor Red
    }

} catch {
    Write-Host "? Error: $($_.Exception.Message)" -ForegroundColor Red
    
    if ($_.Exception.Response) {
        $statusCode = $_.Exception.Response.StatusCode
        Write-Host "Status Code: $statusCode" -ForegroundColor Yellow
        
        try {
            $reader = New-Object System.IO.StreamReader($_.Exception.Response.GetResponseStream())
            $errorContent = $reader.ReadToEnd()
            $errorObj = $errorContent | ConvertFrom-Json
            Write-Host "Error Details: $($errorObj.message)" -ForegroundColor Yellow
            if ($errorObj.errors) {
                $errorObj.errors | ForEach-Object { Write-Host "  - $_" -ForegroundColor Yellow }
            }
        } catch {
            Write-Host "Raw Error: $errorContent" -ForegroundColor Yellow
        }
    }
}

Write-Host ""
Write-Host "=== Test Complete ===" -ForegroundColor Green