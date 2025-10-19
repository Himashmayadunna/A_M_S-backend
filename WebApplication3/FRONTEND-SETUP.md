# Frontend Configuration Guide

## ?? **URGENT: Update Your Frontend API URL**

Your backend is running on **NEW PORTS**:
- **HTTP**: `http://localhost:5000`
- **HTTPS**: `https://localhost:7000`

## **Step 1: Update Environment Variables**

In your **frontend project root**, create or update one of these files:

### **Option A: .env.local (Recommended for Next.js)**
```bash
# Use HTTP (recommended if HTTPS has certificate issues)
NEXT_PUBLIC_API_URL=http://localhost:5000/api

# Or use HTTPS (if certificates work)
# NEXT_PUBLIC_API_URL=https://localhost:7000/api
```

### **Option B: .env**
```bash
# For React apps
REACT_APP_API_URL=http://localhost:5000/api

# For Next.js apps
NEXT_PUBLIC_API_URL=http://localhost:5000/api
```

## **Step 2: Restart Frontend**

After updating the environment file:
```bash
# Stop your frontend (Ctrl+C)
# Then restart:
npm run dev
# or
yarn dev
```

## **Step 3: Test the Connection**

1. **Open browser dev tools** (F12)
2. **Go to Network tab**
3. **Try to register/login**
4. **Check the request URL** - it should now show `localhost:5000` instead of `localhost:7188`

## **Common Frontend API Usage**

### **Next.js Example:**
```javascript
// In your frontend code
const apiUrl = process.env.NEXT_PUBLIC_API_URL; // http://localhost:5000/api

const registerUser = async (userData) => {
  const response = await fetch(`${apiUrl}/auth/register`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(userData),
  });
  return response.json();
};
```

### **React Example:**
```javascript
// In your frontend code
const apiUrl = process.env.REACT_APP_API_URL; // http://localhost:5000/api

const registerUser = async (userData) => {
  const response = await fetch(`${apiUrl}/auth/register`, {
    method: 'POST',
    headers: {
      'Content-Type': 'application/json',
    },
    body: JSON.stringify(userData),
  });
  return response.json();
};
```

## **Troubleshooting**

### **Still Getting Port 7188 Error?**
1. Check if you have multiple environment files
2. Clear browser cache (Ctrl+Shift+R)
3. Restart both backend and frontend
4. Check browser dev tools for the actual request URL

### **CORS Errors?**
The backend is now configured to allow all origins in development mode.

### **Certificate Errors?**
Use HTTP version: `http://localhost:5000/api`

## **Test Endpoints**

You can test these directly in browser:
- Health: http://localhost:5000/api/health
- Auth Health: http://localhost:5000/api/auth/health
- CORS Test: http://localhost:5000/api/cors-test