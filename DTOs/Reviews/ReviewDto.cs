using BusinessObjects.Reviews;

namespace DTOs.Reviews
{
    public class ReviewDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        
        public Guid ProductVariantId { get; set; } // << THÊM MỚI
        public string VariantInfo { get; set; } = string.Empty; // << THÊM MỚI (e.g., "Màu: Đen, Size: L")
        public Guid ProductId { get; set; } // << Giữ lại để tham khảo
        public string ProductName { get; set; } = string.Empty; // << Giữ lại để tham khảo
        
        public Guid OrderId { get; set; }
        public int Rating { get; set; }
        public string Content { get; set; } = string.Empty;
        public List<string>? Images { get; set; }
        public ReviewStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}