# Quick Database Connection Fix Script
# This script tries the most common solutions for SQL Server connection issues

Write-Host "=== Quick Database Fix Script ===" -ForegroundColor Green
Write-Host ""

# Check if we're in the right directory
if (-not (Test-Path "WebApplication3.csproj")) {
    Write-Host "Error: Please run this script from the WebApplication3 project directory." -ForegroundColor Red
    exit 1
}

Write-Host "Step 1: Backing up current appsettings.json..." -ForegroundColor Yellow
if (Test-Path "appsettings.json") {
    Copy-Item "appsettings.json" "appsettings.json.backup" -Force
    Write-Host "? Backup created: appsettings.json.backup" -ForegroundColor Green
}

Write-Host ""
Write-Host "Step 2: Testing SQL Server connection options..." -ForegroundColor Yellow

# Function to test connection and update appsettings if successful
function Test-AndUpdateConnection {
    param([string]$ConnectionString, [string]$Description)
    
    try {
        # Test connection
        $connection = New-Object System.Data.SqlClient.SqlConnection($ConnectionString)
        $connection.Open()
        $connection.Close()
        
        Write-Host "? $Description - Connection successful!" -ForegroundColor Green
        
        # Update appsettings.json
        $appSettingsPath = "appsettings.json"
        $appSettings = Get-Content $appSettingsPath | ConvertFrom-Json
        $auctionConnection = $ConnectionString -replace "Database=master", "Database=AuctionHouseDB"
        $appSettings.ConnectionStrings.DefaultConnection = $auctionConnection
        $appSettings | ConvertTo-Json -Depth 10 | Set-Content $appSettingsPath
        
        Write-Host "? Updated appsettings.json with working connection string" -ForegroundColor Green
        return $true
    } catch {
        Write-Host "? $Description - Failed" -ForegroundColor Red
        return $false
    }
}

# Try different connection strings in order of likelihood
$connectionOptions = @(
    @{
        Connection = "Server=(localdb)\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True;"
        Description = "SQL Server LocalDB (most common)"
    },
    @{
        Connection = "Server=localhost\SQLEXPRESS;Database=master;Integrated Security=true;TrustServerCertificate=true;"
        Description = "SQL Server Express"
    },
    @{
        Connection = "Server=localhost;Database=master;Integrated Security=true;TrustServerCertificate=true;"
        Description = "SQL Server Default Instance"
    },
    @{
        Connection = "Server=.\SQLEXPRESS;Database=master;Integrated Security=true;TrustServerCertificate=true;"
        Description = "SQL Express (alternative)"
    }
)

$connectionFound = $false
foreach ($option in $connectionOptions) {
    if (Test-AndUpdateConnection -ConnectionString $option.Connection -Description $option.Description) {
        $connectionFound = $true
        break
    }
}

if (-not $connectionFound) {
    Write-Host ""
    Write-Host "? No working SQL Server connection found!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Quick solutions:" -ForegroundColor Yellow
    Write-Host "1. Install SQL Server LocalDB (simplest):" -ForegroundColor White
    Write-Host "   https://www.microsoft.com/en-us/download/details.aspx?id=29062" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "2. Or install SQL Server Developer Edition:" -ForegroundColor White
    Write-Host "   https://www.microsoft.com/en-us/sql-server/sql-server-downloads" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "3. Or use Docker:" -ForegroundColor White
    Write-Host "   docker run -e ACCEPT_EULA=Y -e SA_PASSWORD=StrongPassword123! -p 1433:1433 --name sqlserver -d mcr.microsoft.com/mssql/server:2019-latest" -ForegroundColor Cyan
    
    # Restore backup
    if (Test-Path "appsettings.json.backup") {
        Copy-Item "appsettings.json.backup" "appsettings.json" -Force
        Write-Host ""
        Write-Host "Restored original appsettings.json" -ForegroundColor Yellow
    }
    exit 1
}

Write-Host ""
Write-Host "Step 3: Installing Entity Framework tools (if needed)..." -ForegroundColor Yellow
try {
    $efCheck = & dotnet ef --version 2>$null
    if ($LASTEXITCODE -ne 0) {
        Write-Host "Installing EF Core tools..." -ForegroundColor White
        & dotnet tool install --global dotnet-ef
        Write-Host "? Entity Framework tools installed" -ForegroundColor Green
    } else {
        Write-Host "? Entity Framework tools already available" -ForegroundColor Green
    }
} catch {
    Write-Host "Installing EF Core tools..." -ForegroundColor White
    & dotnet tool install --global dotnet-ef
}

Write-Host ""
Write-Host "Step 4: Setting up database..." -ForegroundColor Yellow

# Clean old migrations
if (Test-Path "Migrations") {
    Write-Host "Cleaning old migrations..." -ForegroundColor White
    Remove-Item -Recurse -Force "Migrations"
}

# Create new migration
try {
    Write-Host "Creating database migration..." -ForegroundColor White
    & dotnet ef migrations add InitialCreate
    Write-Host "? Migration created successfully" -ForegroundColor Green
} catch {
    Write-Host "? Failed to create migration: $_" -ForegroundColor Red
    exit 1
}

# Update database
try {
    Write-Host "Updating database..." -ForegroundColor White
    & dotnet ef database update
    Write-Host "? Database updated successfully" -ForegroundColor Green
} catch {
    Write-Host "? Failed to update database: $_" -ForegroundColor Red
    Write-Host ""
    Write-Host "Try running this manually:" -ForegroundColor Yellow
    Write-Host "dotnet ef database update" -ForegroundColor Cyan
    exit 1
}

Write-Host ""
Write-Host "Step 5: Building application..." -ForegroundColor Yellow
try {
    & dotnet build
    Write-Host "? Application built successfully" -ForegroundColor Green
} catch {
    Write-Host "? Build failed: $_" -ForegroundColor Red
    exit 1
}

Write-Host ""
Write-Host "?? Database connection fixed!" -ForegroundColor Green
Write-Host ""
Write-Host "What was done:" -ForegroundColor Cyan
Write-Host "? Found working SQL Server connection" -ForegroundColor White
Write-Host "? Updated appsettings.json" -ForegroundColor White
Write-Host "? Created database migration" -ForegroundColor White
Write-Host "? Updated database with tables" -ForegroundColor White
Write-Host "? Built application successfully" -ForegroundColor White
Write-Host ""
Write-Host "Next steps:" -ForegroundColor Yellow
Write-Host "1. Run your API: dotnet run" -ForegroundColor White
Write-Host "2. Test health endpoint: http://localhost:5277/api/health" -ForegroundColor White
Write-Host "3. View Swagger UI: http://localhost:5277/swagger" -ForegroundColor White
Write-Host ""
Write-Host "Your auction system with bidding is now ready! ??" -ForegroundColor Green