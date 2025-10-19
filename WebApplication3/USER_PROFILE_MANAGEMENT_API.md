# User Profile and Item Management API Documentation

## Overview
This document covers the new user profile management and auction item management features added to the Auction House API.

## ?? Authentication Required
All endpoints in this documentation require JWT authentication via the `Authorization: Bearer <token>` header.

---

## ?? User Profile Management

### Get User Profile
**Endpoint:** `GET /api/auth/profile`  
**Auth Required:** Yes  
**Role:** All authenticated users

**Response:**
```json
{
  "success": true,
  "data": {
    "userId": 1,
    "firstName": "John",
    "lastName": "Doe",
    "email": "john.doe@example.com",
    "accountType": "Seller",
    "agreeToTerms": true,
    "receiveUpdates": true,
    "createdAt": "2024-01-01T00:00:00Z",
    "updatedAt": "2024-01-02T00:00:00Z",
    "isActive": true
  }
}
```

### Update User Profile
**Endpoint:** `PUT /api/auth/profile`  
**Auth Required:** Yes  
**Role:** All authenticated users

**Request Body:**
```json
{
  "firstName": "UpdatedName",     // Optional
  "lastName": "UpdatedLastName",  // Optional
  "email": "new@example.com",     // Optional
  "receiveUpdates": false         // Optional
}
```

**Response:**
```json
{
  "success": true,
  "message": "Profile updated successfully",
  "data": {
    // Updated profile object
  }
}
```

**Validation Rules:**
- First name: 2-100 characters
- Last name: 2-100 characters
- Email: Valid email format, max 255 characters, must be unique
- Email changes are validated against existing accounts

### Change Password
**Endpoint:** `PUT /api/auth/change-password`  
**Auth Required:** Yes  
**Role:** All authenticated users

**Request Body:**
```json
{
  "currentPassword": "CurrentPassword123!",
  "newPassword": "NewPassword123!",
  "confirmPassword": "NewPassword123!"
}
```

**Response:**
```json
{
  "success": true,
  "message": "Password changed successfully"
}
```

**Validation Rules:**
- Current password must match user's existing password
- New password: 6-100 characters, must contain uppercase, lowercase, number, and special character
- Confirm password must match new password

### Deactivate Account
**Endpoint:** `DELETE /api/auth/deactivate`  
**Auth Required:** Yes  
**Role:** All authenticated users

**Request Body:**
```json
{
  "password": "UserPassword123!",
  "confirmDeactivation": true
}
```

**Response:**
```json
{
  "success": true,
  "message": "Account deactivated successfully"
}
```

**Notes:**
- Requires password confirmation for security
- Sets `isActive` to false (soft delete)
- User cannot login after deactivation

---

## ?? Seller Item Management

### Get My Auctions
**Endpoint:** `GET /api/useritems/my-auctions`  
**Auth Required:** Yes  
**Role:** Sellers only

**Query Parameters:**
- `page` (optional): Page number (default: 1)
- `pageSize` (optional): Items per page (default: 20)
- `status` (optional): Filter by status ("active", "ended", "upcoming")

**Response:**
```json
{
  "success": true,
  "data": [
    {
      "auctionId": 1,
      "title": "Gaming Laptop",
      "startingPrice": 500.00,
      "currentPrice": 750.00,
      "startTime": "2024-01-01T00:00:00Z",
      "endTime": "2024-01-08T00:00:00Z",
      "category": "Electronics",
      "isActive": true,
      "isFeatured": false,
      "viewCount": 25,
      "primaryImageUrl": "https://example.com/image.jpg",
      "totalBids": 5,
      "timeRemaining": "2.05:30:15",
      "status": "Active",
      "seller": {
        "userId": 1,
        "firstName": "John",
        "lastName": "Seller",
        "email": "john@example.com"
      }
    }
  ],
  "pagination": {
    "page": 1,
    "pageSize": 20,
    "totalItems": 1
  }
}
```

### Remove Auction
**Endpoint:** `DELETE /api/useritems/auctions/{auctionId}`  
**Auth Required:** Yes  
**Role:** Sellers only (own auctions)

**Request Body:**
```json
{
  "confirmRemoval": true,
  "reason": "No longer available"  // Optional
}
```

**Response:**
```json
{
  "success": true,
  "message": "Auction removed successfully"
}
```

**Business Rules:**
- Can only remove own auctions
- Cannot remove auctions that have ended with bids
- Active auctions with bids require confirmation (`confirmRemoval: true`)
- All auction data and related records are permanently deleted

### Deactivate Auction
**Endpoint:** `PUT /api/useritems/auctions/{auctionId}/deactivate`  
**Auth Required:** Yes  
**Role:** Sellers only (own auctions)

**Request Body:**
```json
{
  "confirmDeactivation": true,
  "reason": "Temporarily unavailable"  // Optional
}
```

**Response:**
```json
{
  "success": true,
  "message": "Auction deactivated successfully"
}
```

**Business Rules:**
- Can only deactivate own auctions
- Cannot deactivate auctions that have already ended
- Sets auction as inactive (soft delete)
- Preserves all auction data and bid history

### Get Auction Statistics
**Endpoint:** `GET /api/useritems/auction-stats`  
**Auth Required:** Yes  
**Role:** Sellers only

**Response:**
```json
{
  "success": true,
  "data": {
    "totalAuctions": 15,
    "activeAuctions": 5,
    "endedAuctions": 8,
    "upcomingAuctions": 2,
    "totalRevenue": 2450.75,
    "averageSellingPrice": 306.34,
    "totalViews": 1250,
    "featuredAuctions": 3,
    "mostPopularCategory": "Electronics"
  }
}
```

---

## ?? Buyer Management Features

### Get Bidding Summary
**Endpoint:** `GET /api/useritems/bidding-summary`  
**Auth Required:** Yes  
**Role:** Buyers only

**Response:**
```json
{
  "success": true,
  "data": {
    "totalBids": 25,
    "activeBids": 8,
    "wonAuctions": 3,
    "lostAuctions": 14,
    "totalAmountBid": 1875.50,
    "totalAmountWon": 425.75,
    "recentBids": [
      {
        "bidId": 123,
        "auctionId": 45,
        "auctionTitle": "Gaming Mouse",
        "amount": 75.00,
        "bidTime": "2024-01-10T15:30:00Z",
        "isWinningBid": true,
        "auctionEndTime": "2024-01-15T20:00:00Z",
        "auctionCurrentPrice": 75.00,
        "auctionStatus": "Active",
        "primaryImageUrl": "https://example.com/mouse.jpg"
      }
    ],
    "recentWins": [
      {
        "bidId": 120,
        "auctionId": 40,
        "auctionTitle": "Wireless Headphones",
        "winningAmount": 125.00,
        "auctionEndTime": "2024-01-05T18:00:00Z",
        "sellerName": "Jane Seller",
        "sellerEmail": "jane@example.com",
        "primaryImageUrl": "https://example.com/headphones.jpg",
        "location": "New York, NY",
        "shippingInfo": "Free shipping"
      }
    ]
  }
}
```

---

## ?? Security Features

### Role-Based Access Control
- **Sellers** can only access seller-specific endpoints
- **Buyers** can only access buyer-specific endpoints
- All endpoints validate user ownership for resource access

### Input Validation
- All endpoints include comprehensive input validation
- Password requirements enforced
- Email format and uniqueness validation
- Required field validation with meaningful error messages

### Auction Removal Safety
- Prevents deletion of auctions with concluded bidding
- Requires confirmation for active auctions with bids
- Preserves data integrity and bidder protection

---

## ?? Error Responses

### Common Error Format
```json
{
  "success": false,
  "message": "Error description",
  "errors": [
    "Detailed error message 1",
    "Detailed error message 2"
  ]
}
```

### HTTP Status Codes
- `200`: Success
- `400`: Bad Request (validation errors, business rule violations)
- `401`: Unauthorized (invalid/missing token)
- `403`: Forbidden (insufficient permissions)
- `404`: Not Found (resource doesn't exist)
- `500`: Internal Server Error

### Common Error Scenarios

#### Profile Management
- **400**: Validation errors (invalid email format, password requirements)
- **400**: Email already in use
- **400**: Current password incorrect
- **401**: Token expired or invalid

#### Auction Management
- **403**: Trying to manage someone else's auction
- **400**: Cannot remove auction with concluded bids
- **400**: Cannot deactivate ended auction
- **404**: Auction not found

#### Role-Based Access
- **403**: Buyer trying to access seller endpoints
- **403**: Seller trying to access buyer endpoints

---

## ?? Testing

Use the provided test script to validate all functionality:
```bash
.\test-user-profile-management.ps1
```

The test script covers:
- User registration and authentication
- Profile management operations
- Auction creation and removal
- Statistics and summaries
- Security and validation
- Error handling scenarios

---

## ?? Usage Examples

### Frontend Integration Examples

#### React/JavaScript - Update Profile
```javascript
const updateProfile = async (profileData) => {
  const response = await fetch('/api/auth/profile', {
    method: 'PUT',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${userToken}`
    },
    body: JSON.stringify(profileData)
  });
  
  return await response.json();
};
```

#### React/JavaScript - Remove Auction
```javascript
const removeAuction = async (auctionId, confirm = false) => {
  const response = await fetch(`/api/useritems/auctions/${auctionId}`, {
    method: 'DELETE',
    headers: {
      'Content-Type': 'application/json',
      'Authorization': `Bearer ${userToken}`
    },
    body: JSON.stringify({
      confirmRemoval: confirm,
      reason: "No longer available"
    })
  });
  
  return await response.json();
};
```

#### React/JavaScript - Get Bidding Summary
```javascript
const getBiddingSummary = async () => {
  const response = await fetch('/api/useritems/bidding-summary', {
    headers: {
      'Authorization': `Bearer ${userToken}`
    }
  });
  
  return await response.json();
};
```

---

This completes the comprehensive user profile and item management system for your auction platform!