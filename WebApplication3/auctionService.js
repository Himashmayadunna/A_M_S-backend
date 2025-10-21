// ✅ COPY THIS TO: src/services/auctionService.js

const API_BASE = 'http://localhost:5000';

export const auctionService = {
  /**
   * Get all auctions with pagination and filtering
   */
  async getAllAuctions(page = 1, pageSize = 20, category = '', search = '') {
    try {
      const params = new URLSearchParams({
        page: page.toString(),
        pageSize: pageSize.toString(),
      });
      
      if (category) params.append('category', category);
      if (search) params.append('search', search);
      
      const response = await fetch(`${API_BASE}/api/Auctions?${params}`);
      
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }
      
      const data = await response.json();
      
      if (!data.success) {
        throw new Error(data.message || 'Failed to fetch auctions');
      }
      
      return data;
    } catch (error) {
      console.error('Error fetching auctions:', error);
      throw error;
    }
  },

  /**
   * Get single auction by ID
   */
  async getAuctionById(id) {
    try {
      const response = await fetch(`${API_BASE}/api/Auctions/${id}`);
      
      if (!response.ok) {
        throw new Error(`HTTP error! status: ${response.status}`);
      }
      
      const data = await response.json();
      
      if (!data.success) {
        throw new Error(data.message || 'Failed to fetch auction');
      }
      
      return data.auction;
    } catch (error) {
      console.error(`Error fetching auction ${id}:`, error);
      throw error;
    }
  },

  /**
   * Get full image URL from backend path
   * @param {string} imagePath - Path like '/uploaded/rolex.jpg'
   * @returns {string} Full URL like 'http://localhost:5000/uploaded/rolex.jpg'
   */
  getImageUrl(imagePath) {
    if (!imagePath) {
      return '/placeholder.png'; // Your placeholder image
    }
    
    // If already full URL, return as is
    if (imagePath.startsWith('http://') || imagePath.startsWith('https://')) {
      return imagePath;
    }
    
    // Combine with API base
    return `${API_BASE}${imagePath}`;
  },

  /**
   * Check if backend is healthy
   */
  async checkBackendHealth() {
    try {
      const response = await fetch(`${API_BASE}/api/health`);
      if (!response.ok) return false;
      
      const data = await response.json();
      return data.status === 'healthy';
    } catch (error) {
      console.error('Backend health check failed:', error);
      return false;
    }
  }
};

export default auctionService;
