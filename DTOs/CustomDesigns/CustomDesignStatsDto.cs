using BusinessObjects.Products;

namespace DTOs.CustomDesigns
{
    public class CustomDesignStatsDto
    {
        public int TotalDesigns { get; set; }
        public int PendingDesigns { get; set; }
        public int ApprovedDesigns { get; set; }
        public int InProductionDesigns { get; set; }
        public int CompletedDesigns { get; set; }
        public decimal TotalRevenue { get; set; }
        public double AverageProcessingDays { get; set; }
        public Dictionary<DesignStatus, int> StatusDistribution { get; set; } = new();
    }

    public class DesignPricingDto
    {
        public GarmentType ShirtType { get; set; }
        public ProductColor BaseColor { get; set; }
        public TShirtSize Size { get; set; }
        public decimal BasePrice { get; set; }
        public decimal CustomizationFee { get; set; }
        public decimal TotalPrice { get; set; }
        public bool HasLogo { get; set; }
        public decimal LogoFee { get; set; }
    }
}