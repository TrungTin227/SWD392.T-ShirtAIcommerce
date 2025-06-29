using System.ComponentModel.DataAnnotations;

namespace DTOs.Category
{
    public class UpdateCategoryDto
    {
        [StringLength(100, ErrorMessage = "Name cannot exceed 100 characters")]
        public string? Name { get; set; }

        [StringLength(500, ErrorMessage = "Description cannot exceed 500 characters")]
        public string? Description { get; set; }

        public bool? IsActive { get; set; }
    }
}