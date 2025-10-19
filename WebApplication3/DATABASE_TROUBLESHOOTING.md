# Database Connection Troubleshooting Guide

## ?? **Common SQL Server Connection Issues & Solutions**

### **Issue:** Cannot connect to SQL Server localhost

---

## **Step 1: Check SQL Server Installation & Status**

### **Option A: Check if SQL Server is installed**
1. Open **SQL Server Configuration Manager**
   - Press `Win + R`, type `SQLServerManager15.msc` (for SQL Server 2019)
   - Or search "SQL Server Configuration Manager" in Start menu

2. Check **SQL Server Services**:
   - Look for "SQL Server (MSSQLSERVER)" or "SQL Server (SQLEXPRESS)"
   - Status should be "Running"
   - If stopped, right-click ? Start

### **Option B: Alternative SQL Server installations**
Try these connection strings in `appsettings.json`:

**For SQL Server Express:**
```json
"DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=AuctionHouseDB;Integrated Security=true;TrustServerCertificate=true;"
```

**For SQL Server LocalDB:**
```json
"DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=AuctionHouseDB;Trusted_Connection=True;TrustServerCertificate=True;"
```

**For Named Instance:**
```json
"DefaultConnection": "Server=localhost\\YourInstanceName;Database=AuctionHouseDB;Integrated Security=true;TrustServerCertificate=true;"
```

---

## **Step 2: Test Database Connection**

### **Quick Test Script:**
```powershell
# Run this in PowerShell to test SQL Server connectivity
try {
    $connectionString = "Server=localhost;Database=master;Integrated Security=true;TrustServerCertificate=true;"
    $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
    $connection.Open()
    Write-Host "? SQL Server connection successful!" -ForegroundColor Green
    $connection.Close()
} catch {
    Write-Host "? SQL Server connection failed: $_" -ForegroundColor Red
    Write-Host "Trying SQL Express..." -ForegroundColor Yellow
    
    try {
        $connectionString = "Server=localhost\\SQLEXPRESS;Database=master;Integrated Security=true;TrustServerCertificate=true;"
        $connection = New-Object System.Data.SqlClient.SqlConnection($connectionString)
        $connection.Open()
        Write-Host "? SQL Express connection successful!" -ForegroundColor Green
        Write-Host "Update your appsettings.json to use: localhost\\SQLEXPRESS" -ForegroundColor Cyan
        $connection.Close()
    } catch {
        Write-Host "? SQL Express also failed: $_" -ForegroundColor Red
    }
}
```

---

## **Step 3: Install SQL Server (if not installed)**

### **Download & Install SQL Server Developer Edition (Free):**
1. Go to: https://www.microsoft.com/en-us/sql-server/sql-server-downloads
2. Download "Developer" edition (free)
3. Run installer
4. Choose "Basic" installation
5. Accept defaults and install

### **Alternative: Install SQL Server Express:**
1. Download SQL Server Express from Microsoft
2. During installation, choose "Mixed Mode" authentication
3. Set a password for 'sa' user
4. Enable TCP/IP protocol

---

## **Step 4: Fix Common Connection Issues**

### **Enable TCP/IP Protocol:**
1. Open **SQL Server Configuration Manager**
2. Go to **SQL Server Network Configuration** ? **Protocols for MSSQLSERVER**
3. Right-click **TCP/IP** ? **Enable**
4. Restart SQL Server service

### **Check Windows Authentication:**
1. Open **SQL Server Management Studio** (SSMS)
2. Connect to your SQL Server instance
3. Right-click server ? **Properties** ? **Security**
4. Ensure "Windows Authentication mode" is selected

### **Firewall Issues:**
1. Open **Windows Defender Firewall**
2. Click **Allow an app through firewall**
3. Add **SQL Server** if not present

---

## **Step 5: Database Migration Issues**

### **Clean Migration Setup:**
```powershell
# Run from WebApplication3 directory
# Remove old migrations
Remove-Item -Recurse -Force "Migrations" -ErrorAction SilentlyContinue

# Add fresh migration
dotnet ef migrations add InitialCreate

# Update database
dotnet ef database update
```

### **Manual Database Creation:**
If Entity Framework fails, create database manually:

1. Open **SQL Server Management Studio**
2. Connect to your SQL Server
3. Right-click **Databases** ? **New Database**
4. Name: `AuctionHouseDB`
5. Click **OK**

---

## **Step 6: Alternative Solutions**

### **Option A: Use SQL Server LocalDB (Simplest)**
Update `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\MSSQLLocalDB;Database=AuctionHouseDB;Trusted_Connection=True;TrustServerCertificate=True;"
  }
}
```

### **Option B: Use SQL Server Express**
Update `appsettings.json`:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost\\SQLEXPRESS;Database=AuctionHouseDB;Integrated Security=true;TrustServerCertificate=true;"
  }
}
```

### **Option C: Use Docker SQL Server**
```bash
# Run SQL Server in Docker
docker run -e "ACCEPT_EULA=Y" -e "SA_PASSWORD=YourStrong@Passw0rd" -p 1433:1433 --name sqlserver -d mcr.microsoft.com/mssql/server:2019-latest
```

Then use:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost,1433;Database=AuctionHouseDB;User Id=sa;Password=YourStrong@Passw0rd;TrustServerCertificate=true;"
  }
}
```

---

## **Step 7: Verify Setup**

### **Test Your API:**
1. Start your application: `dotnet run`
2. Visit: `http://localhost:5277/api/health`
3. Should return: `{"status":"healthy",...}`

### **Check Database:**
1. Open SQL Server Management Studio
2. Connect to your server
3. Expand **Databases**
4. Look for **AuctionHouseDB**
5. Expand **Tables** to see: Users, Auctions, Bids, etc.

---

## **Step 8: Common Error Messages & Solutions**

### **Error: "A network-related or instance-specific error occurred"**
- **Solution**: Check if SQL Server service is running
- Try different connection strings (SQLEXPRESS, LocalDB)

### **Error: "Login failed for user"**
- **Solution**: Check Windows Authentication settings
- Ensure your Windows user has access to SQL Server

### **Error: "Cannot open database requested by the login"**
- **Solution**: Database doesn't exist, run `dotnet ef database update`

### **Error: "Invalid object name 'Users'"**
- **Solution**: Tables not created, run database migrations

---

## **Quick Diagnostic Commands**

```powershell
# Check .NET version
dotnet --version

# Check EF Core tools
dotnet ef --version

# List SQL Server instances
sqlcmd -L

# Test connection with sqlcmd
sqlcmd -S localhost -E -Q "SELECT @@VERSION"
```

---

## **?? Still Having Issues?**

1. **Check Windows Services:**
   - Press `Win + R`, type `services.msc`
   - Look for "SQL Server (MSSQLSERVER)" service
   - Ensure it's running

2. **Try Different Port:**
   ```json
   "DefaultConnection": "Server=localhost,1433;Database=AuctionHouseDB;Integrated Security=true;TrustServerCertificate=true;"
   ```

3. **Use SQL Authentication:**
   ```json
   "DefaultConnection": "Server=localhost;Database=AuctionHouseDB;User Id=sa;Password=YourPassword;TrustServerCertificate=true;"
   ```

4. **Check SQL Server Browser Service:**
   - Must be running for named instances

---

**Remember:** After changing connection strings, restart your application!