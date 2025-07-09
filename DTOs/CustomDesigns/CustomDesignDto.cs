using BusinessObjects.Common;

namespace DTOs.CustomDesigns
{
    public class CustomDesignDto
    {
        public Guid Id { get; set; }
        public Guid UserId { get; set; }
        public string UserName { get; set; } = string.Empty;
        public string DesignName { get; set; } = string.Empty;
        public GarmentType ShirtType { get; set; }
        public ProductColor BaseColor { get; set; }
        public TShirtSize Size { get; set; }
        public string? DesignImageUrl { get; set; }
        public string? LogoText { get; set; }
        public LogoPosition? LogoPosition { get; set; }
        public string? SpecialRequirements { get; set; }
        public decimal TotalPrice { get; set; }
        public int Quantity { get; set; }
        public int EstimatedDays { get; set; }
        public DesignStatus Status { get; set; }
        public Guid? StaffId { get; set; }
        public string? StaffName { get; set; }
        public string? StaffNotes { get; set; }
        public DateTime? ApprovedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}