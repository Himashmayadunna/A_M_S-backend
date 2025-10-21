using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApplication3.DTOs;
using WebApplication3.Services;
using AuctionHouse.API.DTOs;

namespace WebApplication3.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ImagesController : ControllerBase
    {
        private readonly IImageService _imageService;
        private readonly ILogger<ImagesController> _logger;

        public ImagesController(IImageService imageService, ILogger<ImagesController> logger)
        {
            _imageService = imageService;
            _logger = logger;
        }

        /// <summary>
        /// Upload an image for a specific auction
        /// </summary>
        [HttpPost("upload/{auctionId}")]
        [Authorize]
        public async Task<ActionResult<ImageResponseDto>> UploadImage(
            int auctionId,
            [FromForm] UploadImageRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ErrorResponseDto 
                    { 
                        Message = "Invalid request data",
                        Errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList()
                    });
                }

                var result = await _imageService.UploadImageAsync(auctionId, request);
                return Ok(result);
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid image upload request for auction {AuctionId}", auctionId);
                return BadRequest(new ErrorResponseDto { Message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading image for auction {AuctionId}", auctionId);
                return StatusCode(500, new ErrorResponseDto 
                { 
                    Message = "An error occurred while uploading the image" 
                });
            }
        }

        /// <summary>
        /// Get all images for a specific auction
        /// </summary>
        [HttpGet("auction/{auctionId}")]
        public async Task<ActionResult<List<ImageResponseDto>>> GetAuctionImages(int auctionId)
        {
            try
            {
                var images = await _imageService.GetAuctionImagesAsync(auctionId);
                return Ok(images);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving images for auction {AuctionId}", auctionId);
                return StatusCode(500, new ErrorResponseDto 
                { 
                    Message = "An error occurred while retrieving images" 
                });
            }
        }

        /// <summary>
        /// Delete an image
        /// </summary>
        [HttpDelete("{imageId}/auction/{auctionId}")]
        [Authorize]
        public async Task<ActionResult> DeleteImage(int imageId, int auctionId)
        {
            try
            {
                var result = await _imageService.DeleteImageAsync(imageId, auctionId);
                
                if (!result)
                {
                    return NotFound(new ErrorResponseDto 
                    { 
                        Message = "Image not found" 
                    });
                }

                return Ok(new { message = "Image deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting image {ImageId} for auction {AuctionId}", 
                    imageId, auctionId);
                return StatusCode(500, new ErrorResponseDto 
                { 
                    Message = "An error occurred while deleting the image" 
                });
            }
        }

        /// <summary>
        /// Set an image as primary for an auction
        /// </summary>
        [HttpPut("{imageId}/auction/{auctionId}/set-primary")]
        [Authorize]
        public async Task<ActionResult> SetPrimaryImage(int imageId, int auctionId)
        {
            try
            {
                var result = await _imageService.SetPrimaryImageAsync(imageId, auctionId);
                
                if (!result)
                {
                    return NotFound(new ErrorResponseDto 
                    { 
                        Message = "Image not found" 
                    });
                }

                return Ok(new { message = "Primary image set successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error setting primary image {ImageId} for auction {AuctionId}", 
                    imageId, auctionId);
                return StatusCode(500, new ErrorResponseDto 
                { 
                    Message = "An error occurred while setting the primary image" 
                });
            }
        }

        /// <summary>
        /// Update image metadata (alt text, display order)
        /// </summary>
        [HttpPut("{imageId}/auction/{auctionId}")]
        [Authorize]
        public async Task<ActionResult<ImageResponseDto>> UpdateImage(
            int imageId,
            int auctionId,
            [FromBody] UpdateImageRequestDto request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(new ErrorResponseDto 
                    { 
                        Message = "Invalid request data",
                        Errors = ModelState.Values
                            .SelectMany(v => v.Errors)
                            .Select(e => e.ErrorMessage)
                            .ToList()
                    });
                }

                var result = await _imageService.UpdateImageAsync(imageId, auctionId, request);
                
                if (result == null)
                {
                    return NotFound(new ErrorResponseDto 
                    { 
                        Message = "Image not found" 
                    });
                }

                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating image {ImageId} for auction {AuctionId}", 
                    imageId, auctionId);
                return StatusCode(500, new ErrorResponseDto 
                { 
                    Message = "An error occurred while updating the image" 
                });
            }
        }

        /// <summary>
        /// Delete all images for an auction (admin/owner only)
        /// </summary>
        [HttpDelete("auction/{auctionId}")]
        [Authorize]
        public async Task<ActionResult> DeleteAuctionImages(int auctionId)
        {
            try
            {
                var result = await _imageService.DeleteAuctionImagesAsync(auctionId);
                
                if (!result)
                {
                    return NotFound(new ErrorResponseDto 
                    { 
                        Message = "No images found for this auction" 
                    });
                }

                return Ok(new { message = "All auction images deleted successfully" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting all images for auction {AuctionId}", auctionId);
                return StatusCode(500, new ErrorResponseDto 
                { 
                    Message = "An error occurred while deleting auction images" 
                });
            }
        }

        /// <summary>
        /// Test endpoint to check database connection and folders (NO AUTH REQUIRED)
        /// </summary>
        [HttpGet("test/status")]
        public async Task<ActionResult> TestStatus()
        {
            try
            {
                _logger.LogInformation("=== IMAGE SYSTEM STATUS CHECK ===");
                
                var wwwrootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
                var uploadsPath = Path.Combine(wwwrootPath, "uploads", "auctions");
                
                var status = new
                {
                    timestamp = DateTime.UtcNow,
                    folders = new
                    {
                        wwwroot_exists = Directory.Exists(wwwrootPath),
                        wwwroot_path = wwwrootPath,
                        uploads_exists = Directory.Exists(uploadsPath),
                        uploads_path = uploadsPath,
                        files_in_uploads = Directory.Exists(uploadsPath) 
                            ? Directory.GetFiles(uploadsPath).Length 
                            : 0
                    },
                    database = new
                    {
                        total_images = await _imageService.GetTotalImageCountAsync(),
                        connection_ok = true
                    },
                    message = "✅ Image upload system is ready!"
                };

                _logger.LogInformation("Status check complete: {Status}", status);
                return Ok(status);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking image system status");
                return StatusCode(500, new 
                { 
                    error = ex.Message,
                    message = "❌ Image upload system has issues"
                });
            }
        }
    }
}
