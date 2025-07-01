using BusinessObjects.Products;
using System.ComponentModel.DataAnnotations;

namespace DTOs.CustomDesigns
{
    public class CreateCustomDesignDto
    {
        [Required(ErrorMessage = "Design name is required")]
        [MaxLength(255, ErrorMessage = "Design name cannot exceed 255 characters")]
        public string DesignName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Shirt type is required")]
        public GarmentType ShirtType { get; set; }

        [Required(ErrorMessage = "Base color is required")]
        public ProductColor BaseColor { get; set; }

        [Required(ErrorMessage = "Size is required")]
        public TShirtSize Size { get; set; }

        [Url(ErrorMessage = "Invalid design image URL")]
        [MaxLength(500, ErrorMessage = "Image URL cannot exceed 500 characters")]
        public string? DesignImageUrl { get; set; }

        [MaxLength(255, ErrorMessage = "Logo text cannot exceed 255 characters")]
        public string? LogoText { get; set; }

        public LogoPosition? LogoPosition { get; set; }

        [MaxLength(1000, ErrorMessage = "Special requirements cannot exceed 1000 characters")]
        public string? SpecialRequirements { get; set; }

        [Required(ErrorMessage = "Quantity is required")]
        [Range(1, 100, ErrorMessage = "Quantity must be between 1 and 100")]
        public int Quantity { get; set; } = 1;

        [Range(1, 30, ErrorMessage = "Estimated days must be between 1 and 30")]
        public int EstimatedDays { get; set; } = 7;
    }
}