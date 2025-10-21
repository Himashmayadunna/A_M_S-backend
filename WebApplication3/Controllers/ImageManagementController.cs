using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AuctionHouse.API.Data;
using System.Text;

namespace AuctionHouse.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ImageManagementController : ControllerBase
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ImageManagementController> _logger;

        public ImageManagementController(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            ILogger<ImageManagementController> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        /// <summary>
        /// Check all images in database and verify if files exist
        /// </summary>
        [HttpGet("check-images")]
        public async Task<IActionResult> CheckImages()
        {
            var images = await _context.AuctionImages
                .Include(i => i.Auction)
                .ToListAsync();

            var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsPath);

            var report = new List<object>();

            foreach (var img in images)
            {
                var fileName = Path.GetFileName(img.ImageUrl);
                var possiblePaths = new[]
                {
                    Path.Combine(uploadsPath, fileName),
                    Path.Combine(_environment.WebRootPath, fileName),
                    Path.Combine(uploadsPath, "auctions", fileName)
                };

                var fileExists = false;
                var foundPath = "";

                foreach (var path in possiblePaths)
                {
                    if (System.IO.File.Exists(path))
                    {
                        fileExists = true;
                        foundPath = path;
                        break;
                    }
                }

                report.Add(new
                {
                    imageId = img.ImageId,
                    auctionId = img.AuctionId,
                    auctionTitle = img.Auction?.Title ?? "Unknown",
                    originalUrl = img.ImageUrl,
                    fileName = fileName,
                    fileExists = fileExists,
                    foundPath = foundPath,
                    isPrimary = img.IsPrimary
                });
            }

            var missingCount = report.Count(r => !(bool)r.GetType().GetProperty("fileExists")!.GetValue(r)!);

            return Ok(new
            {
                success = true,
                totalImages = images.Count,
                missingFiles = missingCount,
                existingFiles = images.Count - missingCount,
                images = report
            });
        }

        /// <summary>
        /// Create placeholder images for all missing files
        /// </summary>
        [HttpPost("create-placeholders")]
        public async Task<IActionResult> CreatePlaceholders()
        {
            var images = await _context.AuctionImages
                .Include(i => i.Auction)
                .ToListAsync();

            var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads");
            Directory.CreateDirectory(uploadsPath);

            var createdFiles = new List<string>();

            foreach (var img in images)
            {
                var fileName = Path.GetFileName(img.ImageUrl);
                var filePath = Path.Combine(uploadsPath, fileName);

                // Check if file already exists
                if (!System.IO.File.Exists(filePath))
                {
                    // Create a simple placeholder SVG image
                    var svgContent = CreatePlaceholderSVG(img.Auction?.Title ?? "Auction Item", 800, 600);
                    
                    // Save as PNG-like file (browsers will render SVG)
                    var svgBytes = Encoding.UTF8.GetBytes(svgContent);
                    
                    // For SVG files, save with .svg extension
                    var svgPath = Path.ChangeExtension(filePath, ".svg");
                    await System.IO.File.WriteAllBytesAsync(svgPath, svgBytes);
                    
                    // Also create a copy with original extension for compatibility
                    await System.IO.File.WriteAllBytesAsync(filePath, svgBytes);
                    
                    createdFiles.Add(fileName);
                    _logger.LogInformation("Created placeholder for: {FileName}", fileName);
                }
            }

            return Ok(new
            {
                success = true,
                message = $"Created {createdFiles.Count} placeholder images",
                createdFiles = createdFiles
            });
        }

        /// <summary>
        /// Fix image URLs in database to use correct format
        /// </summary>
        [HttpPost("fix-urls")]
        public async Task<IActionResult> FixImageUrls()
        {
            var images = await _context.AuctionImages.ToListAsync();
            var updatedCount = 0;

            foreach (var img in images)
            {
                var originalUrl = img.ImageUrl;

                // Normalize the URL
                if (!originalUrl.StartsWith("http://") && !originalUrl.StartsWith("https://"))
                {
                    // Extract just the filename
                    var fileName = Path.GetFileName(originalUrl);
                    
                    // Set it to the uploaded path
                    var newUrl = $"/uploaded/{fileName}";

                    if (newUrl != originalUrl)
                    {
                        img.ImageUrl = newUrl;
                        updatedCount++;
                        _logger.LogInformation("Updated URL: {Old} -> {New}", originalUrl, newUrl);
                    }
                }
            }

            if (updatedCount > 0)
            {
                await _context.SaveChangesAsync();
            }

            return Ok(new
            {
                success = true,
                message = $"Fixed {updatedCount} image URLs",
                updatedCount = updatedCount
            });
        }

        /// <summary>
        /// Get statistics about images
        /// </summary>
        [HttpGet("stats")]
        public async Task<IActionResult> GetImageStats()
        {
            var totalImages = await _context.AuctionImages.CountAsync();
            var auctionsWithImages = await _context.Auctions
                .Include(a => a.Images)
                .Where(a => a.Images.Any())
                .CountAsync();
            var auctionsWithoutImages = await _context.Auctions.CountAsync() - auctionsWithImages;

            var uploadsPath = Path.Combine(_environment.WebRootPath, "uploads");
            var filesOnDisk = Directory.Exists(uploadsPath)
                ? Directory.GetFiles(uploadsPath, "*.*", SearchOption.AllDirectories).Length
                : 0;

            return Ok(new
            {
                success = true,
                stats = new
                {
                    totalImagesInDatabase = totalImages,
                    auctionsWithImages = auctionsWithImages,
                    auctionsWithoutImages = auctionsWithoutImages,
                    filesOnDisk = filesOnDisk,
                    uploadsFolderPath = uploadsPath
                }
            });
        }

        private string CreatePlaceholderSVG(string text, int width, int height)
        {
            return $@"<?xml version=""1.0"" encoding=""UTF-8""?>
<svg width=""{width}"" height=""{height}"" xmlns=""http://www.w3.org/2000/svg"">
    <rect width=""100%"" height=""100%"" fill=""#f0f0f0""/>
    <rect x=""10"" y=""10"" width=""{width - 20}"" height=""{height - 20}"" fill=""none"" stroke=""#cccccc"" stroke-width=""2""/>
    <text x=""50%"" y=""45%"" font-family=""Arial, sans-serif"" font-size=""24"" fill=""#666666"" text-anchor=""middle"" dominant-baseline=""middle"">
        🖼️ {System.Security.SecurityElement.Escape(text)}
    </text>
    <text x=""50%"" y=""55%"" font-family=""Arial, sans-serif"" font-size=""16"" fill=""#999999"" text-anchor=""middle"" dominant-baseline=""middle"">
        Image Placeholder
    </text>
</svg>";
        }
    }
}
