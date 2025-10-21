using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AuctionHouse.API.Data;
using AuctionHouse.API.Models;

namespace WebApplication3.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TestImageController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly ILogger<TestImageController> _logger;

        public TestImageController(ApplicationDbContext context, ILogger<TestImageController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Simple test to insert an image record directly into database
        /// This bypasses file upload to test database insertion only
        /// </summary>
        [HttpPost("test-insert/{auctionId}")]
        public async Task<IActionResult> TestInsert(int auctionId)
        {
            try
            {
                _logger.LogInformation("=== TEST INSERT STARTED ===");

                // Check if auction exists
                var auction = await _context.Auctions.FindAsync(auctionId);
                if (auction == null)
                {
                    _logger.LogError("Auction {AuctionId} not found", auctionId);
                    return NotFound($"Auction {auctionId} not found");
                }
                _logger.LogInformation("Auction found: {AuctionId}, SellerId: {SellerId}", auctionId, auction.SellerId);

                // Check current image count
                var currentCount = await _context.AuctionImages.CountAsync();
                _logger.LogInformation("Current image count in database: {Count}", currentCount);

                // Create a test image record
                var testImage = new AuctionImage
                {
                    AuctionId = auctionId,
                    ImageUrl = $"/test/Seller{auction.SellerId}_Item{auctionId}_TEST.jpg",
                    AltText = "Test image from test endpoint",
                    IsPrimary = false,
                    DisplayOrder = 999,
                    CreatedAt = DateTime.UtcNow
                };

                _logger.LogInformation("Creating test image: AuctionId={AuctionId}, ImageUrl={ImageUrl}", 
                    testImage.AuctionId, testImage.ImageUrl);

                // Add to context
                _context.AuctionImages.Add(testImage);
                _logger.LogInformation("Image added to context, calling SaveChangesAsync...");

                // Save changes
                var result = await _context.SaveChangesAsync();
                _logger.LogInformation("SaveChangesAsync result: {Result} rows affected", result);

                if (result > 0)
                {
                    _logger.LogInformation("✅ SUCCESS! ImageId assigned: {ImageId}", testImage.ImageId);
                    
                    // Verify it's actually in the database
                    var savedImage = await _context.AuctionImages
                        .FirstOrDefaultAsync(i => i.ImageId == testImage.ImageId);
                    
                    if (savedImage != null)
                    {
                        _logger.LogInformation("✅ VERIFIED! Image retrieved from database: ImageId={ImageId}", savedImage.ImageId);
                        
                        return Ok(new
                        {
                            success = true,
                            message = "Image successfully saved to database!",
                            imageId = testImage.ImageId,
                            imageUrl = testImage.ImageUrl,
                            auctionId = testImage.AuctionId,
                            sellerId = auction.SellerId,
                            rowsAffected = result,
                            verifiedInDb = true
                        });
                    }
                    else
                    {
                        _logger.LogError("❌ Image was assigned ID but not found in database!");
                        return StatusCode(500, new { success = false, message = "Image saved but verification failed" });
                    }
                }
                else
                {
                    _logger.LogError("❌ SaveChangesAsync returned 0 rows affected!");
                    return StatusCode(500, new { success = false, message = "SaveChangesAsync returned 0 rows" });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ ERROR during test insert: {Message}", ex.Message);
                return StatusCode(500, new { 
                    success = false, 
                    message = $"Error: {ex.Message}",
                    innerException = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        /// <summary>
        /// Get count of images in database
        /// </summary>
        [HttpGet("count")]
        public async Task<IActionResult> GetImageCount()
        {
            try
            {
                var count = await _context.AuctionImages.CountAsync();
                var images = await _context.AuctionImages
                    .Select(i => new { i.ImageId, i.AuctionId, i.ImageUrl })
                    .ToListAsync();

                return Ok(new
                {
                    totalImages = count,
                    images = images
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting image count");
                return StatusCode(500, new { error = ex.Message });
            }
        }

        /// <summary>
        /// Delete all test images (cleanup)
        /// </summary>
        [HttpDelete("cleanup")]
        public async Task<IActionResult> Cleanup()
        {
            try
            {
                var testImages = await _context.AuctionImages
                    .Where(i => i.ImageUrl.Contains("/test/"))
                    .ToListAsync();

                _context.AuctionImages.RemoveRange(testImages);
                var deleted = await _context.SaveChangesAsync();

                return Ok(new { message = $"Deleted {deleted} test images" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during cleanup");
                return StatusCode(500, new { error = ex.Message });
            }
        }
    }
}
