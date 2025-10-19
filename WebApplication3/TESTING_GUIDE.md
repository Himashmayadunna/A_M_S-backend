# Auction House API Testing Guide

## Issue Resolution Steps

Your auction items weren't being saved to the database due to several potential issues. Here's what I've fixed:

### 1. **Database Transaction Handling**
- Added proper database transactions to ensure data integrity
- Enhanced error handling and rollback capabilities

### 2. **Validation & Error Handling**  
- Improved DTO validation with better error messages
- Added comprehensive logging for debugging
- Enhanced user account type validation

### 3. **Authentication Issues**
- Fixed token parsing and user ID extraction
- Added debug endpoints to test authentication

### 4. **CORS Configuration**
- Your CORS is configured correctly for localhost development

## Testing Steps

### Step 1: Test Database Connection
```
GET http://localhost:5000/api/debug/db-test
```

### Step 2: Test Authentication (Login first)
```
POST http://localhost:5000/api/auth/login
Content-Type: application/json

{
  "email": "your-seller@email.com",
  "password": "your-password"
}
```

### Step 3: Test User Info with Token
```
GET http://localhost:5000/api/debug/user-info
Authorization: Bearer YOUR_JWT_TOKEN
```

### Step 4: Test Auction Creation
```
POST http://localhost:5000/api/auctions
Authorization: Bearer YOUR_JWT_TOKEN
Content-Type: application/json

{
  "title": "Test Auction Item",
  "description": "This is a test auction item for debugging",
  "startingPrice": 10.00,
  "startTime": "2024-10-07T10:00:00Z",
  "endTime": "2024-10-14T10:00:00Z",
  "category": "Electronics",
  "condition": "New",
  "location": "Test Location",
  "shippingInfo": "Free shipping",
  "isFeatured": false,
  "images": []
}
```

### Step 5: Create Test Auction (Simplified)
```
POST http://localhost:5000/api/debug/test-auction
Authorization: Bearer YOUR_JWT_TOKEN
```

## Common Issues & Solutions

### Issue 1: "Only sellers can create auctions"
**Solution**: Ensure your user account has `AccountType = "Seller"`

### Issue 2: "Invalid token: User ID not found"
**Solutions**:
- Make sure you're including the Bearer token in the Authorization header
- Verify the token is valid and not expired
- Check that the user still exists in the database

### Issue 3: "Start time cannot be in the past"
**Solution**: Use future dates for StartTime and EndTime

### Issue 4: Validation errors
**Solutions**:
- Title: 3-200 characters
- Description: 10-2000 characters  
- Starting Price: $0.01 - $999,999.99
- Category: 2-50 characters

## Frontend Integration Tips

### JavaScript Fetch Example:
```javascript
// Create auction
const createAuction = async (auctionData, token) => {
  try {
    const response = await fetch('http://localhost:5000/api/auctions', {
      method: 'POST',
      headers: {
        'Content-Type': 'application/json',
        'Authorization': `Bearer ${token}`
      },
      body: JSON.stringify(auctionData)
    });
    
    const result = await response.json();
    
    if (!response.ok) {
      console.error('Auction creation failed:', result);
      return { success: false, error: result };
    }
    
    console.log('Auction created:', result);
    return { success: true, data: result };
  } catch (error) {
    console.error('Network error:', error);
    return { success: false, error: error.message };
  }
};
```

### React Example:
```jsx
const handleSubmit = async (formData) => {
  const token = localStorage.getItem('authToken');
  
  const auctionData = {
    title: formData.title,
    description: formData.description,
    startingPrice: parseFloat(formData.startingPrice),
    startTime: new Date(formData.startTime).toISOString(),
    endTime: new Date(formData.endTime).toISOString(),
    category: formData.category,
    condition: formData.condition || 'New',
    location: formData.location || '',
    shippingInfo: formData.shippingInfo || '',
    isFeatured: formData.isFeatured || false,
    images: formData.images || []
  };
  
  const result = await createAuction(auctionData, token);
  
  if (result.success) {
    // Success - redirect or show success message
    alert('Auction created successfully!');
  } else {
    // Handle errors
    console.error('Error:', result.error);
    alert('Failed to create auction: ' + (result.error.message || 'Unknown error'));
  }
};
```

## Debugging Checklist

1. ? **Check Database Connection**: Use `/api/debug/db-test`
2. ? **Verify User Authentication**: Use `/api/debug/user-info`  
3. ? **Check Account Type**: Must be "Seller" to create auctions
4. ? **Validate Input Data**: All required fields must be present
5. ? **Check Network Requests**: Use browser dev tools
6. ? **Review Server Logs**: Check console output for errors

## What's Been Fixed

1. **Enhanced Validation**: Better input validation with clear error messages
2. **Transaction Safety**: Database transactions ensure data integrity  
3. **Improved Logging**: Detailed logs help identify issues
4. **Authentication Debugging**: New endpoints to test auth flow
5. **Error Handling**: Comprehensive error responses
6. **CORS Issues**: Properly configured for local development

Your auction creation should now work properly. If you still encounter issues, use the debug endpoints to identify exactly where the problem occurs.