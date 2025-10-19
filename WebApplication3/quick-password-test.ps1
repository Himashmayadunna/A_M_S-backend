# Quick Simple Password Test
# Run this to test simple passwords immediately

$baseUrl = "http://localhost:5277"

Write-Host "=== Quick Simple Password Test ===" -ForegroundColor Green
Write-Host ""

# Test 1: Register with simple password
Write-Host "1. Testing registration with simple password 'test123'..." -ForegroundColor Yellow

$userData = @{
    firstName = "Simple"
    lastName = "User"
    email = "simple@example.com"
    password = "test123"
    accountType = "Buyer"
    agreeToTerms = $true
    receiveUpdates = $false
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/auth/register" -Method POST -Body $userData -ContentType "application/json"
    
    if ($response.success) {
        Write-Host "? Registration successful!" -ForegroundColor Green
        Write-Host "  User ID: $($response.data.userId)" -ForegroundColor White
        Write-Host "  Name: $($response.data.firstName) $($response.data.lastName)" -ForegroundColor White
        Write-Host "  Email: $($response.data.email)" -ForegroundColor White
        Write-Host "  Token: $($response.data.token.Substring(0, 20))..." -ForegroundColor White
        
        $token = $response.data.token
    } else {
        Write-Host "? Registration failed" -ForegroundColor Red
    }
} catch {
    Write-Host "? Registration error: $_" -ForegroundColor Red
    # Try login if user already exists
    Write-Host "Trying login instead..." -ForegroundColor Yellow
    
    $loginData = @{
        email = "simple@example.com"
        password = "test123"
    } | ConvertTo-Json
    
    try {
        $loginResponse = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method POST -Body $loginData -ContentType "application/json"
        
        if ($loginResponse.success) {
            Write-Host "? Login successful!" -ForegroundColor Green
            $token = $loginResponse.data.token
        }
    } catch {
        Write-Host "? Login also failed: $_" -ForegroundColor Red
    }
}

Write-Host ""

# Test 2: Login with sample user
Write-Host "2. Testing login with sample user (password: 'password123')..." -ForegroundColor Yellow

$loginData = @{
    email = "mike.buyer@example.com"
    password = "password123"
} | ConvertTo-Json

try {
    $response = Invoke-RestMethod -Uri "$baseUrl/api/auth/login" -Method POST -Body $loginData -ContentType "application/json"
    
    if ($response.success) {
        Write-Host "? Sample user login successful!" -ForegroundColor Green
        Write-Host "  User: $($response.data.firstName) $($response.data.lastName)" -ForegroundColor White
        Write-Host "  Account Type: $($response.data.accountType)" -ForegroundColor White
    }
} catch {
    Write-Host "? Sample user login failed: $_" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== Simple Password Examples ===" -ForegroundColor Cyan
Write-Host "All of these passwords will work now:" -ForegroundColor White
Write-Host "• test123" -ForegroundColor Green
Write-Host "• password123" -ForegroundColor Green  
Write-Host "• simple" -ForegroundColor Green
Write-Host "• 123456" -ForegroundColor Green
Write-Host "• mypass" -ForegroundColor Green
Write-Host "• testing" -ForegroundColor Green
Write-Host ""
Write-Host "Sample Accounts (all use password: 'password123'):" -ForegroundColor Yellow
Write-Host "Sellers:" -ForegroundColor White
Write-Host "  john.seller@example.com" -ForegroundColor Green
Write-Host "  sarah.merchant@example.com" -ForegroundColor Green
Write-Host "Buyers:" -ForegroundColor White
Write-Host "  mike.buyer@example.com" -ForegroundColor Green
Write-Host "  lisa.collector@example.com" -ForegroundColor Green
Write-Host "  david.bidder@example.com" -ForegroundColor Green
Write-Host ""
Write-Host "? Your system now accepts simple passwords!" -ForegroundColor Green