using DTOs.Common;

namespace DTOs.Category
{
    public class CategoryFilterDto : PaginationDto
    {
        public string? Name { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? CreatedFrom { get; set; }
        public DateTime? CreatedTo { get; set; }
    }
}
