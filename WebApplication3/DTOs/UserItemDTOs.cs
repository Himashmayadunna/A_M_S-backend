using System.ComponentModel.DataAnnotations;

namespace AuctionHouse.API.DTOs
{
    /// <summary>
    /// DTO for removing/deleting an auction
    /// </summary>
    public class RemoveAuctionDto
    {
        [Required(ErrorMessage = "Confirmation is required to remove auction")]
        [Range(typeof(bool), "true", "true", ErrorMessage = "You must confirm auction removal")]
        public bool ConfirmRemoval { get; set; }

        [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
        public string? Reason { get; set; }
    }

    /// <summary>
    /// DTO for deactivating an auction (soft delete)
    /// </summary>
    public class DeactivateAuctionDto
    {
        [Required(ErrorMessage = "Confirmation is required to deactivate auction")]
        [Range(typeof(bool), "true", "true", ErrorMessage = "You must confirm auction deactivation")]
        public bool ConfirmDeactivation { get; set; }

        [StringLength(500, ErrorMessage = "Reason cannot exceed 500 characters")]
        public string? Reason { get; set; }
    }
}