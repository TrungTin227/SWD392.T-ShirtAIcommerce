using System.ComponentModel.DataAnnotations;
using BusinessObjects.Common;

namespace DTOs.CustomDesigns
{
    public class CreateCustomDesignRequest
    {
        [Required]
        [MaxLength(255)]
        public string DesignName { get; set; } = string.Empty;

        [Required]
        [MaxLength(1000)]
        public string PromptText { get; set; } = string.Empty;

        [Required]
        public GarmentType ShirtType { get; set; }

        [Required]
        public ProductColor BaseColor { get; set; }

        [Required]
        public TShirtSize Size { get; set; }

        [MaxLength(1000)]
        public string? SpecialRequirements { get; set; }

        [Range(1, 1000, ErrorMessage = "Số lượng phải từ 1 đến 1000")]
        public int Quantity { get; set; } = 1;

        
    }
}
