# Test script for API endpoints
# Run this after starting your API server

$httpsUrl = "https://localhost:7000"  # Updated HTTPS port
$httpUrl = "http://localhost:5000"    # Updated HTTP port

Write-Host "Testing API endpoints..." -ForegroundColor Green
Write-Host "HTTPS URL: $httpsUrl" -ForegroundColor Cyan
Write-Host "HTTP URL: $httpUrl" -ForegroundColor Cyan

# Test 1: Health check endpoint
Write-Host "`n1. Testing health endpoint..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$httpsUrl/api/health" -Method Get -SkipCertificateCheck
    Write-Host "? HTTPS Health check passed: $($response | ConvertTo-Json)" -ForegroundColor Green
}
catch {
    Write-Host "? HTTPS Health check failed: $($_.Exception.Message)" -ForegroundColor Red
    try {
        $response = Invoke-RestMethod -Uri "$httpUrl/api/health" -Method Get
        Write-Host "? HTTP Health check passed: $($response | ConvertTo-Json)" -ForegroundColor Green
    }
    catch {
        Write-Host "? HTTP Health check also failed: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Test 2: Auth health endpoint
Write-Host "`n2. Testing auth health endpoint..." -ForegroundColor Yellow
try {
    $response = Invoke-RestMethod -Uri "$httpsUrl/api/auth/health" -Method Get -SkipCertificateCheck
    Write-Host "? HTTPS Auth health check passed: $($response | ConvertTo-Json)" -ForegroundColor Green
}
catch {
    Write-Host "? HTTPS Auth health check failed: $($_.Exception.Message)" -ForegroundColor Red
    try {
        $response = Invoke-RestMethod -Uri "$httpUrl/api/auth/health" -Method Get
        Write-Host "? HTTP Auth health check passed: $($response | ConvertTo-Json)" -ForegroundColor Green
    }
    catch {
        Write-Host "? HTTP Auth health check also failed: $($_.Exception.Message)" -ForegroundColor Red
    }
}

# Test 3: CORS preflight
Write-Host "`n3. Testing CORS preflight..." -ForegroundColor Yellow
try {
    $headers = @{
        'Origin' = 'http://localhost:3000'
        'Access-Control-Request-Method' = 'POST'
        'Access-Control-Request-Headers' = 'Content-Type'
    }
    $response = Invoke-WebRequest -Uri "$httpsUrl/api/auth/register" -Method Options -Headers $headers -SkipCertificateCheck
    Write-Host "? CORS preflight passed: Status $($response.StatusCode)" -ForegroundColor Green
    Write-Host "Access-Control-Allow-Origin: $($response.Headers['Access-Control-Allow-Origin'])"
}
catch {
    Write-Host "? HTTPS CORS preflight failed: $($_.Exception.Message)" -ForegroundColor Red
    try {
        $headers = @{
            'Origin' = 'http://localhost:3000'
            'Access-Control-Request-Method' = 'POST'
            'Access-Control-Request-Headers' = 'Content-Type'
        }
        $response = Invoke-WebRequest -Uri "$httpUrl/api/auth/register" -Method Options -Headers $headers
        Write-Host "? HTTP CORS preflight passed: Status $($response.StatusCode)" -ForegroundColor Green
        Write-Host "Access-Control-Allow-Origin: $($response.Headers['Access-Control-Allow-Origin'])"
    }
    catch {
        Write-Host "? HTTP CORS preflight also failed: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host "`n?? Frontend Configuration:" -ForegroundColor Cyan
Write-Host "   For your React/Next.js app, use these API URLs:" -ForegroundColor White
Write-Host "   HTTPS: NEXT_PUBLIC_API_URL=https://localhost:7000/api" -ForegroundColor Green
Write-Host "   HTTP:  NEXT_PUBLIC_API_URL=http://localhost:5000/api" -ForegroundColor Green
Write-Host "" -ForegroundColor White
Write-Host "?? If tests fail, make sure:" -ForegroundColor Cyan
Write-Host "   - Your API server is running (dotnet run or F5 in Visual Studio)" -ForegroundColor White
Write-Host "   - Try both HTTP and HTTPS URLs" -ForegroundColor White
Write-Host "   - Check Windows Defender/Firewall settings" -ForegroundColor White
Write-Host "   - If HTTPS fails, use HTTP URL in your frontend" -ForegroundColor White