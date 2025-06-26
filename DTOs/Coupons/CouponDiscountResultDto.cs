namespace DTOs.Coupons
{
    public class CouponDiscountResultDto
    {
        public bool IsValid { get; set; }
        public string Message { get; set; } = string.Empty;
        public decimal DiscountAmount { get; set; }
        public decimal FinalAmount { get; set; }
        public CouponDto? Coupon { get; set; }
    }
}