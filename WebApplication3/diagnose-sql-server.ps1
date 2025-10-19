# SQL Server Diagnostic Script
# This script helps identify your SQL Server installation and suggests the correct connection string

Write-Host "=== SQL Server Diagnostic Tool ===" -ForegroundColor Green
Write-Host ""

# Function to test connection
function Test-SqlConnection {
    param([string]$ConnectionString, [string]$Description)
    
    try {
        $connection = New-Object System.Data.SqlClient.SqlConnection($ConnectionString)
        $connection.Open()
        $connection.Close()
        Write-Host "? $Description - SUCCESS" -ForegroundColor Green
        return $true
    } catch {
        Write-Host "? $Description - Failed: $($_.Exception.Message)" -ForegroundColor Red
        return $false
    }
}

Write-Host "1. Checking for SQL Server instances..." -ForegroundColor Yellow

# Check for SQL Server services
$sqlServices = Get-Service -Name "*SQL*" -ErrorAction SilentlyContinue | Where-Object {$_.DisplayName -like "*SQL Server*"}

if ($sqlServices) {
    Write-Host "Found SQL Server services:" -ForegroundColor Cyan
    foreach ($service in $sqlServices) {
        $status = if ($service.Status -eq "Running") { "? Running" } else { "? Stopped" }
        $color = if ($service.Status -eq "Running") { "Green" } else { "Red" }
        Write-Host "  $($service.DisplayName): $status" -ForegroundColor $color
    }
} else {
    Write-Host "No SQL Server services found!" -ForegroundColor Red
    Write-Host "You may need to install SQL Server." -ForegroundColor Yellow
}

Write-Host ""
Write-Host "2. Testing connection strings..." -ForegroundColor Yellow

# Define connection strings to test
$connectionStrings = @(
    @{
        Connection = "Server=localhost;Database=master;Integrated Security=true;TrustServerCertificate=true;"
        Description = "SQL Server Default Instance (localhost)"
    },
    @{
        Connection = "Server=localhost\SQLEXPRESS;Database=master;Integrated Security=true;TrustServerCertificate=true;"
        Description = "SQL Server Express"
    },
    @{
        Connection = "Server=(localdb)\MSSQLLocalDB;Database=master;Trusted_Connection=True;TrustServerCertificate=True;"
        Description = "SQL Server LocalDB"
    },
    @{
        Connection = "Server=.\SQLEXPRESS;Database=master;Integrated Security=true;TrustServerCertificate=true;"
        Description = "SQL Express (dot notation)"
    },
    @{
        Connection = "Server=localhost,1433;Database=master;Integrated Security=true;TrustServerCertificate=true;"
        Description = "SQL Server with Port 1433"
    }
)

$workingConnections = @()

foreach ($connTest in $connectionStrings) {
    if (Test-SqlConnection -ConnectionString $connTest.Connection -Description $connTest.Description) {
        $workingConnections += $connTest
    }
}

Write-Host ""
if ($workingConnections.Count -gt 0) {
    Write-Host "?? Found working SQL Server connections!" -ForegroundColor Green
    Write-Host ""
    Write-Host "Recommended appsettings.json configurations:" -ForegroundColor Cyan
    
    foreach ($working in $workingConnections) {
        $appSettingsConnection = $working.Connection -replace "Database=master", "Database=AuctionHouseDB"
        Write-Host ""
        Write-Host "For $($working.Description):" -ForegroundColor Yellow
        Write-Host '"ConnectionStrings": {' -ForegroundColor White
        Write-Host '  "DefaultConnection": "' -NoNewline -ForegroundColor White
        Write-Host $appSettingsConnection -NoNewline -ForegroundColor Cyan
        Write-Host '"' -ForegroundColor White
        Write-Host '}' -ForegroundColor White
    }
    
    Write-Host ""
    Write-Host "3. Next steps:" -ForegroundColor Yellow
    Write-Host "   1. Update your appsettings.json with one of the above connection strings" -ForegroundColor White
    Write-Host "   2. Run: dotnet ef database update" -ForegroundColor White
    Write-Host "   3. Run: dotnet run" -ForegroundColor White
    
} else {
    Write-Host "? No working SQL Server connections found!" -ForegroundColor Red
    Write-Host ""
    Write-Host "Solutions:" -ForegroundColor Yellow
    Write-Host "1. Install SQL Server Developer Edition (free):" -ForegroundColor White
    Write-Host "   https://www.microsoft.com/en-us/sql-server/sql-server-downloads" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "2. Or install SQL Server Express:" -ForegroundColor White
    Write-Host "   https://www.microsoft.com/en-us/download/details.aspx?id=101064" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "3. Or use Docker:" -ForegroundColor White
    Write-Host "   docker run -e ACCEPT_EULA=Y -e SA_PASSWORD=YourStrong@Passw0rd -p 1433:1433 --name sqlserver -d mcr.microsoft.com/mssql/server:2019-latest" -ForegroundColor Cyan
    Write-Host ""
    Write-Host "4. Start SQL Server services if installed:" -ForegroundColor White
    Write-Host "   - Open Services (services.msc)" -ForegroundColor White
    Write-Host "   - Find 'SQL Server' services and start them" -ForegroundColor White
}

Write-Host ""
Write-Host "4. Additional diagnostics:" -ForegroundColor Yellow

# Check if sqlcmd is available
try {
    $sqlcmdVersion = & sqlcmd -? 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "? SQLCMD is available" -ForegroundColor Green
        
        # Try to list SQL Server instances
        try {
            Write-Host "Available SQL Server instances:" -ForegroundColor Cyan
            & sqlcmd -L
        } catch {
            Write-Host "Could not list SQL Server instances" -ForegroundColor Yellow
        }
    }
} catch {
    Write-Host "? SQLCMD not found (this is optional)" -ForegroundColor Yellow
}

# Check .NET and EF versions
Write-Host ""
Write-Host "Development environment:" -ForegroundColor Cyan
try {
    $dotnetVersion = & dotnet --version
    Write-Host "? .NET Version: $dotnetVersion" -ForegroundColor Green
} catch {
    Write-Host "? .NET not found" -ForegroundColor Red
}

try {
    $efVersion = & dotnet ef --version 2>$null
    if ($LASTEXITCODE -eq 0) {
        Write-Host "? Entity Framework tools available" -ForegroundColor Green
    } else {
        Write-Host "? Entity Framework tools not found" -ForegroundColor Red
        Write-Host "  Install with: dotnet tool install --global dotnet-ef" -ForegroundColor Yellow
    }
} catch {
    Write-Host "? Entity Framework tools not found" -ForegroundColor Red
    Write-Host "  Install with: dotnet tool install --global dotnet-ef" -ForegroundColor Yellow
}

Write-Host ""
Write-Host "=== Diagnostic Complete ===" -ForegroundColor Green