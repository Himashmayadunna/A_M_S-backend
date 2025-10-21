using Microsoft.EntityFrameworkCore;
using AuctionHouse.API.Data;
using WebApplication3.DTOs;
using AuctionHouse.API.Models;

namespace WebApplication3.Services
{
    public class ImageService : IImageService
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ImageService> _logger;
        private readonly string[] _allowedExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
        private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

        public ImageService(
            ApplicationDbContext context, 
            IWebHostEnvironment environment,
            ILogger<ImageService> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        public async Task<ImageResponseDto> UploadImageAsync(int auctionId, UploadImageRequestDto request)
        {
            try
            {
                _logger.LogInformation("=== IMAGE UPLOAD STARTED === AuctionId: {AuctionId}, FileName: {FileName}", 
                    auctionId, request.ImageFile?.FileName);

                // Verify auction exists
                var auction = await _context.Auctions.FindAsync(auctionId);
                if (auction == null)
                {
                    _logger.LogError("Auction not found: {AuctionId}", auctionId);
                    throw new ArgumentException($"Auction with ID {auctionId} not found");
                }
                _logger.LogInformation("Auction found: {AuctionId}, SellerId: {SellerId}", auctionId, auction.SellerId);

                // Validate file
                ValidateImageFile(request.ImageFile);
                _logger.LogInformation("File validation passed");

                // Generate unique filename with Seller ID and Item ID (Auction ID)
                // Format: Seller{SellerId}_Item{AuctionId}_{Timestamp}_{Index}.ext
                // Example: Seller5_Item12_20251021120530_1.jpg
                var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmss");
                var existingImageCount = await _context.AuctionImages
                    .Where(ai => ai.AuctionId == auctionId)
                    .CountAsync();
                var imageIndex = existingImageCount + 1;
                var extension = Path.GetExtension(request.ImageFile.FileName).ToLowerInvariant();
                var fileName = $"Seller{auction.SellerId}_Item{auctionId}_{timestamp}_{imageIndex}{extension}";
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "auctions");
                
                _logger.LogInformation("Generated filename: {FileName}", fileName);
                _logger.LogInformation("Upload folder: {UploadFolder}", uploadsFolder);

                // Create directory if it doesn't exist
                Directory.CreateDirectory(uploadsFolder);

                var filePath = Path.Combine(uploadsFolder, fileName);

                // Save file to disk
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await request.ImageFile.CopyToAsync(stream);
                }
                _logger.LogInformation("Physical file saved: {FilePath}", filePath);

                // If this is set as primary, unset other primary images
                if (request.IsPrimary)
                {
                    _logger.LogInformation("Unsetting other primary images for auction {AuctionId}", auctionId);
                    await UnsetPrimaryImagesAsync(auctionId);
                }

                // Create database record
                var auctionImage = new AuctionImage
                {
                    AuctionId = auctionId,
                    ImageUrl = $"/uploads/auctions/{fileName}",
                    AltText = request.AltText,
                    IsPrimary = request.IsPrimary,
                    DisplayOrder = request.DisplayOrder,
                    CreatedAt = DateTime.UtcNow
                };

                _logger.LogInformation("Creating database record: AuctionId={AuctionId}, ImageUrl={ImageUrl}, IsPrimary={IsPrimary}", 
                    auctionImage.AuctionId, auctionImage.ImageUrl, auctionImage.IsPrimary);

                _context.AuctionImages.Add(auctionImage);
                _logger.LogInformation("AuctionImage added to context, calling SaveChangesAsync...");
                
                try
                {
                    var saveResult = await _context.SaveChangesAsync();
                    _logger.LogInformation("SaveChangesAsync completed! Rows affected: {RowsAffected}", saveResult);

                    if (saveResult > 0)
                    {
                        _logger.LogInformation("✅ IMAGE SAVED TO DATABASE! ImageId: {ImageId}", auctionImage.ImageId);
                        
                        // Verify the save by querying back
                        var savedImage = await _context.AuctionImages.FindAsync(auctionImage.ImageId);
                        if (savedImage != null)
                        {
                            _logger.LogInformation("✅ VERIFIED: Image exists in database with ImageId: {ImageId}", savedImage.ImageId);
                        }
                        else
                        {
                            _logger.LogError("❌ ERROR: Image NOT found in database after SaveChanges!");
                        }
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ SaveChangesAsync returned 0 rows affected!");
                        throw new Exception("Failed to save image to database - no rows affected");
                    }
                }
                catch (DbUpdateException dbEx)
                {
                    _logger.LogError(dbEx, "❌ DATABASE UPDATE ERROR: {Message}", dbEx.Message);
                    _logger.LogError("Inner Exception: {InnerException}", dbEx.InnerException?.Message);
                    throw new Exception($"Database error: {dbEx.InnerException?.Message ?? dbEx.Message}", dbEx);
                }
                catch (Exception saveEx)
                {
                    _logger.LogError(saveEx, "❌ SAVE CHANGES ERROR: {Message}", saveEx.Message);
                    throw new Exception($"Failed to save image: {saveEx.Message}", saveEx);
                }

                _logger.LogInformation("=== IMAGE UPLOAD COMPLETE === ImageId: {ImageId}, ImageUrl: {ImageUrl}", 
                    auctionImage.ImageId, auctionImage.ImageUrl);

                return MapToDto(auctionImage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image for auction {AuctionId}", auctionId);
                throw;
            }
        }

        public async Task<bool> DeleteImageAsync(int imageId, int auctionId)
        {
            try
            {
                var image = await _context.AuctionImages
                    .FirstOrDefaultAsync(i => i.ImageId == imageId && i.AuctionId == auctionId);

                if (image == null)
                {
                    return false;
                }

                // Delete physical file
                DeletePhysicalFile(image.ImageUrl);

                // Delete database record
                _context.AuctionImages.Remove(image);
                await _context.SaveChangesAsync();

                _logger.LogInformation("Image {ImageId} deleted successfully for auction {AuctionId}", 
                    imageId, auctionId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image {ImageId} for auction {AuctionId}", 
                    imageId, auctionId);
                throw;
            }
        }

        public async Task<List<ImageResponseDto>> GetAuctionImagesAsync(int auctionId)
        {
            try
            {
                var images = await _context.AuctionImages
                    .Where(i => i.AuctionId == auctionId)
                    .OrderByDescending(i => i.IsPrimary)
                    .ThenBy(i => i.DisplayOrder)
                    .ThenBy(i => i.CreatedAt)
                    .ToListAsync();

                return images.Select(MapToDto).ToList();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving images for auction {AuctionId}", auctionId);
                throw;
            }
        }

        public async Task<bool> SetPrimaryImageAsync(int imageId, int auctionId)
        {
            try
            {
                var image = await _context.AuctionImages
                    .FirstOrDefaultAsync(i => i.ImageId == imageId && i.AuctionId == auctionId);

                if (image == null)
                {
                    return false;
                }

                // Unset all other primary images for this auction
                await UnsetPrimaryImagesAsync(auctionId);

                // Set this image as primary
                image.IsPrimary = true;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Image {ImageId} set as primary for auction {AuctionId}", 
                    imageId, auctionId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting primary image {ImageId} for auction {AuctionId}", 
                    imageId, auctionId);
                throw;
            }
        }

        public async Task<ImageResponseDto?> UpdateImageAsync(int imageId, int auctionId, UpdateImageRequestDto request)
        {
            try
            {
                var image = await _context.AuctionImages
                    .FirstOrDefaultAsync(i => i.ImageId == imageId && i.AuctionId == auctionId);

                if (image == null)
                {
                    return null;
                }

                image.AltText = request.AltText;
                image.DisplayOrder = request.DisplayOrder;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Image {ImageId} updated successfully for auction {AuctionId}", 
                    imageId, auctionId);

                return MapToDto(image);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating image {ImageId} for auction {AuctionId}", 
                    imageId, auctionId);
                throw;
            }
        }

        public async Task<bool> DeleteAuctionImagesAsync(int auctionId)
        {
            try
            {
                var images = await _context.AuctionImages
                    .Where(i => i.AuctionId == auctionId)
                    .ToListAsync();

                foreach (var image in images)
                {
                    DeletePhysicalFile(image.ImageUrl);
                }

                _context.AuctionImages.RemoveRange(images);
                await _context.SaveChangesAsync();

                _logger.LogInformation("All images deleted for auction {AuctionId}", auctionId);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting all images for auction {AuctionId}", auctionId);
                throw;
            }
        }

        #region Private Helper Methods

        private void ValidateImageFile(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentException("File is empty or null");
            }

            if (file.Length > MaxFileSize)
            {
                throw new ArgumentException($"File size exceeds the maximum allowed size of {MaxFileSize / 1024 / 1024}MB");
            }

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!_allowedExtensions.Contains(extension))
            {
                throw new ArgumentException($"File type '{extension}' is not allowed. Allowed types: {string.Join(", ", _allowedExtensions)}");
            }
        }

        private async Task UnsetPrimaryImagesAsync(int auctionId)
        {
            var primaryImages = await _context.AuctionImages
                .Where(i => i.AuctionId == auctionId && i.IsPrimary)
                .ToListAsync();

            foreach (var img in primaryImages)
            {
                img.IsPrimary = false;
            }

            if (primaryImages.Any())
            {
                await _context.SaveChangesAsync();
            }
        }

        private void DeletePhysicalFile(string imageUrl)
        {
            try
            {
                if (string.IsNullOrEmpty(imageUrl))
                    return;

                var fileName = Path.GetFileName(imageUrl);
                var filePath = Path.Combine(_environment.WebRootPath, "uploads", "auctions", fileName);

                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                    _logger.LogInformation("Physical file deleted: {FilePath}", filePath);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to delete physical file: {ImageUrl}", imageUrl);
                // Don't throw - we still want to delete the DB record even if file deletion fails
            }
        }

        private ImageResponseDto MapToDto(AuctionImage image)
        {
            return new ImageResponseDto
            {
                ImageId = image.ImageId,
                AuctionId = image.AuctionId,
                ImageUrl = image.ImageUrl,
                AltText = image.AltText,
                IsPrimary = image.IsPrimary,
                DisplayOrder = image.DisplayOrder,
                CreatedAt = image.CreatedAt
            };
        }

        public async Task<int> GetTotalImageCountAsync()
        {
            return await _context.AuctionImages.CountAsync();
        }

        #endregion
    }
}
