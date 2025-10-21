using WebApplication3.DTOs;

namespace WebApplication3.Services
{
    public interface IImageService
    {
        /// <summary>
        /// Uploads an image for a specific auction
        /// </summary>
        Task<ImageResponseDto> UploadImageAsync(int auctionId, UploadImageRequestDto request);

        /// <summary>
        /// Deletes an image by ID (with ownership verification)
        /// </summary>
        Task<bool> DeleteImageAsync(int imageId, int auctionId);

        /// <summary>
        /// Gets all images for a specific auction
        /// </summary>
        Task<List<ImageResponseDto>> GetAuctionImagesAsync(int auctionId);

        /// <summary>
        /// Sets an image as the primary image for an auction
        /// </summary>
        Task<bool> SetPrimaryImageAsync(int imageId, int auctionId);

        /// <summary>
        /// Updates image metadata (alt text, display order)
        /// </summary>
        Task<ImageResponseDto?> UpdateImageAsync(int imageId, int auctionId, UpdateImageRequestDto request);

        /// <summary>
        /// Deletes all images for a specific auction (used when auction is deleted)
        /// </summary>
        Task<bool> DeleteAuctionImagesAsync(int auctionId);

        /// <summary>
        /// Gets the total count of images in the system
        /// </summary>
        Task<int> GetTotalImageCountAsync();
    }
}
