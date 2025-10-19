# Auction House API - Complete Backend System

A complete auction house backend system built with .NET 8, Entity Framework Core, and SQL Server. This system allows sellers to create auctions and buyers to place bids, with all data stored in SQL Server Management Studio.

## ?? Features

### User Management
- **User Registration & Authentication** - JWT-based auth system
- **Role-based Access Control** - Separate roles for Buyers and Sellers
- **Account Types**: 
  - **Sellers**: Can create, update, and manage auctions
  - **Buyers**: Can browse auctions, place bids, and manage watchlists

### Auction System
- **Create Auctions** - Sellers can list items with detailed information
- **Browse Auctions** - Search and filter by category, price, etc.
- **Auction Images** - Support for multiple images per auction
- **Auction Categories** - Predefined categories for organization
- **Time-based Auctions** - Start time, end time, and duration management

### Bidding System
- **Secure Bidding** - Only buyers can place bids
- **Real-time Bid Tracking** - Track highest bids and bid history
- **Bid Validation** - Prevents invalid bids (too low, seller bidding on own auction)
- **Bid Statistics** - Comprehensive statistics for each auction
- **User Bid History** - Track user's bidding activity
- **Winning Bids** - Track and identify auction winners

### Additional Features
- **Watchlist System** - Users can save favorite auctions
- **Database Seeding** - Automatic sample data generation
- **API Documentation** - Swagger/OpenAPI documentation
- **CORS Support** - Ready for frontend integration

## ??? Technology Stack

- **.NET 8** - Latest .NET framework
- **ASP.NET Core Web API** - RESTful API framework
- **Entity Framework Core** - ORM for database operations
- **SQL Server** - Database management system
- **JWT Authentication** - Secure token-based authentication
- **BCrypt** - Password hashing
- **Swagger/OpenAPI** - API documentation

## ?? Prerequisites

Before running this application, make sure you have:

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [SQL Server](https://www.microsoft.com/en-us/sql-server/sql-server-downloads) (Express or Developer Edition)
- [SQL Server Management Studio](https://docs.microsoft.com/en-us/sql/ssms/download-sql-server-management-studio-ssms) (Optional but recommended)
- [Visual Studio 2022](https://visualstudio.microsoft.com/) or [VS Code](https://code.visualstudio.com/)

## ?? Quick Start

### 1. Clone and Setup
```bash
# Navigate to the project directory
cd WebApplication3

# Restore NuGet packages
dotnet restore
```

### 2. Database Setup

#### Option A: Automatic Setup (Recommended)
Run the PowerShell setup script:
```powershell
# Run from the WebApplication3 directory
.\setup-database.ps1
```

#### Option B: Manual Setup
```bash
# Add initial migration
dotnet ef migrations add InitialMigration

# Update database
dotnet ef database update

# Build the application
dotnet build
```

### 3. Run the Application
```bash
dotnet run
```

The API will be available at:
- **HTTP**: `http://localhost:5277`
- **Swagger UI**: `http://localhost:5277/swagger`

### 4. Test the System
Run the comprehensive test script:
```powershell
.\test-bidding-system.ps1
```

## ??? Database Schema

The system creates the following tables in SQL Server:

### Users Table
- **UserId** (Primary Key)
- **FirstName, LastName** - User name
- **Email** - Unique email address
- **PasswordHash** - Encrypted password
- **AccountType** - "Buyer" or "Seller"
- **AgreeToTerms, ReceiveUpdates** - User preferences
- **CreatedAt, UpdatedAt** - Timestamps
- **IsActive** - Account status

### Auctions Table
- **AuctionId** (Primary Key)
- **Title, Description** - Auction details
- **StartingPrice, CurrentPrice, ReservePrice** - Pricing
- **StartTime, EndTime** - Auction timing
- **Category, Condition, Location** - Item details
- **SellerId** (Foreign Key) - Links to Users table
- **IsActive, IsFeatured** - Status flags
- **Tags, ShippingInfo** - Additional details

### Bids Table
- **BidId** (Primary Key)
- **AuctionId** (Foreign Key) - Links to Auctions
- **BidderId** (Foreign Key) - Links to Users
- **Amount** - Bid amount
- **BidTime** - When bid was placed
- **IsWinningBid** - Current highest bid flag

### AuctionImages Table
- **ImageId** (Primary Key)
- **AuctionId** (Foreign Key)
- **ImageUrl** - Image location
- **IsPrimary** - Main image flag
- **DisplayOrder** - Image ordering

### WatchlistItems Table
- **WatchlistId** (Primary Key)
- **UserId, AuctionId** (Foreign Keys)
- **CreatedAt** - When added to watchlist

## ?? API Endpoints

### Authentication
- `POST /api/auth/register` - Register new user
- `POST /api/auth/login` - User login
- `GET /api/auth/profile` - Get user profile

### Auctions
- `GET /api/auctions` - Get all auctions (with filtering)
- `GET /api/auctions/{id}` - Get specific auction
- `POST /api/auctions` - Create auction (Sellers only)
- `PUT /api/auctions/{id}` - Update auction (Sellers only)
- `DELETE /api/auctions/{id}` - Delete auction (Sellers only)
- `GET /api/auctions/seller` - Get seller's auctions
- `GET /api/auctions/categories` - Get available categories

### Bidding System
- `POST /api/bidding/auctions/{id}/bid` - Place bid (Buyers only)
- `GET /api/bidding/auctions/{id}/bids` - Get auction bids
- `GET /api/bidding/auctions/{id}/stats` - Get bid statistics
- `GET /api/bidding/auctions/{id}/highest-bid` - Get highest bid
- `GET /api/bidding/my-bids` - Get user's bid history
- `GET /api/bidding/my-wins` - Get user's winning bids

### Watchlist
- `POST /api/auctions/{id}/watchlist` - Add to watchlist
- `DELETE /api/auctions/{id}/watchlist` - Remove from watchlist
- `GET /api/auctions/watchlist` - Get user's watchlist

## ?? Security Features

- **JWT Authentication** - Secure token-based authentication
- **Role-based Authorization** - Separate permissions for buyers/sellers
- **Password Hashing** - BCrypt encryption for passwords
- **Input Validation** - Comprehensive data validation
- **CORS Protection** - Configurable cross-origin policies
- **Business Logic Security**:
  - Sellers cannot bid on their own auctions
  - Users cannot place bids lower than current highest bid
  - Only buyers can place bids
  - Only sellers can create auctions

## ?? Sample Data

The system automatically seeds the database with sample data including:
- **5 Sample Users** (2 Sellers, 3 Buyers)
- **5 Sample Auctions** with various categories
- **Sample Bids** demonstrating the bidding system
- **Sample Images** for visual representation

### Sample Accounts
**Sellers:**
- john.seller@example.com / password123
- sarah.merchant@example.com / password123

**Buyers:**
- mike.buyer@example.com / password123
- lisa.collector@example.com / password123
- david.bidder@example.com / password123

## ?? Configuration

### Password Requirements
The system now uses simple password validation:
- **Minimum length**: 6 characters
- **No complexity requirements**: Any combination of letters, numbers, and symbols
- **Examples of valid passwords**: `test123`, `password123`, `simple`, `123456`

### Database Connection
Update `appsettings.json` to match your SQL Server configuration:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=AuctionHouseDB;Integrated Security=true;TrustServerCertificate=true;"
  }
}
```

### JWT Settings
Configure JWT authentication in `appsettings.json`:
```json
{
  "JwtSettings": {
    "Secret": "YourSuperSecretKeyForJWTTokenGeneration123456789",
    "Issuer": "AuctionHouseAPI",
    "Audience": "AuctionHouseClient",
    "ExpiryInHours": 24
  }
}
```

## ?? Sample Usage Examples

### Register a New Buyer
```bash
curl -X POST "http://localhost:5277/api/auth/register" \
  -H "Content-Type: application/json" \
  -d '{
    "firstName": "John",
    "lastName": "Doe",
    "email": "john.doe@example.com",
    "password": "Password123!",
    "accountType": "Buyer",
    "agreeToTerms": true,
    "receiveUpdates": true
  }'
```

### Login User
```bash
curl -X POST "http://localhost:5277/api/auth/login" \
  -H "Content-Type: application/json" \
  -d '{
    "email": "john.doe@example.com",
    "password": "Password123!"
  }'
```

### Place a Bid
```bash
curl -X POST "http://localhost:5277/api/bidding/auctions/1/bid" \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer YOUR_JWT_TOKEN" \
  -d '{
    "amount": 150.00
  }'
```

**Happy Bidding! ??**