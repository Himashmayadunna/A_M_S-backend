using System.ComponentModel.DataAnnotations;

namespace WebApplication3.DTOs
{
    public class UploadImageRequestDto
    {
        [Required(ErrorMessage = "Image file is required")]
        public IFormFile ImageFile { get; set; } = null!;

        [StringLength(200, ErrorMessage = "Alt text cannot exceed 200 characters")]
        public string? AltText { get; set; }

        public bool IsPrimary { get; set; } = false;

        [Range(0, int.MaxValue, ErrorMessage = "Display order must be non-negative")]
        public int DisplayOrder { get; set; } = 0;
    }

    public class ImageResponseDto
    {
        public int ImageId { get; set; }
        public int AuctionId { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public string? AltText { get; set; }
        public bool IsPrimary { get; set; }
        public int DisplayOrder { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class SetPrimaryImageRequestDto
    {
        [Required]
        [Range(1, int.MaxValue, ErrorMessage = "Invalid image ID")]
        public int ImageId { get; set; }
    }

    public class UpdateImageRequestDto
    {
        [StringLength(200, ErrorMessage = "Alt text cannot exceed 200 characters")]
        public string? AltText { get; set; }

        [Range(0, int.MaxValue, ErrorMessage = "Display order must be non-negative")]
        public int DisplayOrder { get; set; }
    }
}
