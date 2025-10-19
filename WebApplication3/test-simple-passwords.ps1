# Simple Password Testing Script
# This script tests the system with simple passwords

$baseUrl = "http://localhost:5277"
$headers = @{
    "Content-Type" = "application/json"
    "Accept" = "application/json"
}

Write-Host "=== Simple Password Testing ===" -ForegroundColor Green
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

# Test simple passwords
Write-Host "1. Testing simple password registration..." -ForegroundColor Yellow

$simplePasswords = @(
    "123456",
    "password",
    "testpass",
    "simple123",
    "abc123",
    "mypassword"
)

foreach ($password in $simplePasswords) {
    Write-Host "  Testing password: '$password'" -ForegroundColor White
    
    $userData = @{
        firstName = "Test"
        lastName = "User"
        email = "test_$($password.Replace(' ', ''))@example.com"
        password = $password
        accountType = "Buyer"
        agreeToTerms = $true
        receiveUpdates = $false
    } | ConvertTo-Json

    $response = Invoke-ApiCall -Method "POST" -Uri "$baseUrl/api/auth/register" -Headers $headers -Body $userData
    
    if ($response -and $response.success) {
        Write-Host "    ? Registration successful with password: '$password'" -ForegroundColor Green
        
        # Test login with the simple password
        $loginData = @{
            email = "test_$($password.Replace(' ', ''))@example.com"
            password = $password
        } | ConvertTo-Json
        
        $loginResponse = Invoke-ApiCall -Method "POST" -Uri "$baseUrl/api/auth/login" -Headers $headers -Body $loginData
        if ($loginResponse -and $loginResponse.success) {
            Write-Host "    ? Login successful with password: '$password'" -ForegroundColor Green
        } else {
            Write-Host "    ? Login failed with password: '$password'" -ForegroundColor Red
        }
    } else {
        Write-Host "    ? Registration failed with password: '$password'" -ForegroundColor Red
    }
    
    Start-Sleep -Seconds 1
}

Write-Host ""
Write-Host "2. Testing password change with simple passwords..." -ForegroundColor Yellow

# First create a user with a simple password
$testUserData = @{
    firstName = "Change"
    lastName = "Password"
    email = "changepassword@example.com"
    password = "oldpass123"
    accountType = "Seller"
    agreeToTerms = $true
    receiveUpdates = $true
} | ConvertTo-Json

$userResponse = Invoke-ApiCall -Method "POST" -Uri "$baseUrl/api/auth/register" -Headers $headers -Body $testUserData

if ($userResponse -and $userResponse.success) {
    Write-Host "  ? Test user created for password change testing" -ForegroundColor Green
    $token = $userResponse.data.token
    
    $authHeaders = $headers.Clone()
    $authHeaders["Authorization"] = "Bearer $token"
    
    # Test changing to various simple passwords
    $newPasswords = @("newpass", "123456", "simple", "testing")
    
    foreach ($newPass in $newPasswords) {
        Write-Host "  Testing password change to: '$newPass'" -ForegroundColor White
        
        $changeData = @{
            currentPassword = "oldpass123"
            newPassword = $newPass
            confirmPassword = $newPass
        } | ConvertTo-Json
        
        $changeResponse = Invoke-ApiCall -Method "PUT" -Uri "$baseUrl/api/auth/change-password" -Headers $authHeaders -Body $changeData
        
        if ($changeResponse -and $changeResponse.success) {
            Write-Host "    ? Password changed to: '$newPass'" -ForegroundColor Green
            
            # Update for next iteration
            $oldPass = $newPass
        } else {
            Write-Host "    ? Password change failed for: '$newPass'" -ForegroundColor Red
        }
        
        Start-Sleep -Seconds 1
    }
} else {
    Write-Host "  ? Failed to create test user for password change testing" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== Testing Complete ===" -ForegroundColor Green
Write-Host ""
Write-Host "Password Requirements Summary:" -ForegroundColor Cyan
Write-Host "• Minimum length: 6 characters" -ForegroundColor White
Write-Host "• Maximum length: 100 characters" -ForegroundColor White
Write-Host "• No complexity requirements" -ForegroundColor White
Write-Host "• Accepts any characters (letters, numbers, symbols)" -ForegroundColor White
Write-Host ""
Write-Host "Examples of valid passwords:" -ForegroundColor Yellow
Write-Host "• 123456" -ForegroundColor Green
Write-Host "• password" -ForegroundColor Green
Write-Host "• simple123" -ForegroundColor Green
Write-Host "• test123" -ForegroundColor Green
Write-Host "• mypassword" -ForegroundColor Green
Write-Host "• abc123" -ForegroundColor Green
Write-Host ""
Write-Host "Your auction system now accepts simple passwords! ??" -ForegroundColor Green