﻿using System.ComponentModel;


namespace BusinessObjects.Common
{
    public enum Gender { Male, Female, Other }
    public enum PaymentPhase
    {
        Deposit = 1,
        Final = 2
    }

    // Enum cho ProductStatus (đã có sẵn)
    public enum ProductStatus
    {
        [Description("Đang bán")]
        Active,

        [Description("Ngừng bán")]
        Inactive,

        [Description("Hết hàng")]
        OutOfStock,

        [Description("Ngừng sản xuất")]
        Discontinued
    }
    public enum CustomDesignStatus
    {
        Draft = 0,
        Liked = 1,
        Accepted = 2,
        Resquest = 3,
        Order = 4, // gửi mail về
        Shipping = 5,
        Delivered = 6, // gửi mail về Customer  
        Done = 7,
        Rejected = 8,

    }
    // Enum cho ProductSize (đã có sẵn)
    public enum ProductSize
    {
        XS,
        S,
        M,
        L,
        XL,
        XXL
    }

    // Enum cho Material (thay thế string Material)
    public enum ProductMaterial
    {
        [Description("Cotton 100%")]
        Cotton100,

        [Description("Cotton Polyester")]
        CottonPolyester,

        [Description("Polyester")]
        Polyester,

        [Description("Cotton Organic")]
        OrganicCotton,

        [Description("Modal")]
        Modal,

        [Description("Bamboo")]
        Bamboo,

        [Description("Cotton Spandex")]
        CottonSpandex,

        [Description("Jersey")]
        Jersey,

        [Description("Canvas")]
        Canvas
    }

    // Enum cho Season (thay thế string Season)
    public enum ProductSeason
    {
        [Description("Xuân")]
        Spring,

        [Description("Hè")]
        Summer,

        [Description("Thu")]
        Autumn,

        [Description("Đông")]
        Winter,

        [Description("Tất cả mùa")]
        AllSeason
    }

    // Enum cho Colors (để parse JSON AvailableColors)
    public enum ProductColor
    {
        [Description("Đen")]
        Black,

        [Description("Trắng")]
        White,

        [Description("Xám")]
        Gray,

        [Description("Đỏ")]
        Red,

        [Description("Xanh dương")]
        Blue,

        [Description("Xanh navy")]
        Navy,

        [Description("Xanh lá")]
        Green,

        [Description("Vàng")]
        Yellow,

        [Description("Cam")]
        Orange,

        [Description("Tím")]
        Purple,

        [Description("Hồng")]
        Pink,

        [Description("Nâu")]
        Brown,

        [Description("Be")]
        Beige
    }
    public enum ShippingCategory
    {
        Standard,
        Express,
        SameDay,
        Overnight
    }
    public enum CouponType
    {
        Percentage,
        FixedAmount,
        FreeShipping
    }

    public enum CouponStatus
    {
        Active,
        Inactive,
        Expired,
        Used
    }
    public enum DesignStatus
    {
        Draft,
        Submitted,
        UnderReview,
        Approved,
        Rejected,
        InProduction,
        Completed,
        Cancelled
    }

    public enum TShirtSize
    {
        XS,
        S,
        M,
        L,
        XL,
        XXL,
        XXXL
    }

    public enum LogoPosition
    {
        Front,
        Back,
        LeftChest,
        RightChest,
        Sleeve,
        Custom
    }
    public enum GarmentType
    {
        TShirt,
        Hoodie,
        Sweatshirt,
        TankTop,
        LongSleeve,
        Jacket
    }
    public enum OrderStatus
    {
        Pending,      // 0. Mới tạo, chưa thanh toán
        Paid,         // 1. Thanh toán thành công (chờ staff xác nhận)
        Completed,    // 2. Staff đã xác nhận, đơn chính thức hoàn thành
        Processing,   // 3. Đang xử lý – đóng gói, chuẩn bị giao
        Shipping,     // 4. Đang vận chuyển
        Delivered,    // 5. Đã giao thành công
        Cancelled,    // 6.Đã hủy (trước khi giao)
        Returned,      // 7.Đã trả/hoàn trả
        CancellationRequested,  //Đã yêu cầu hủy/trả hàng, chờ duyệt
    }

    public enum PaymentStatus
    {
        [Description("Chưa thanh toán")]
        Unpaid,
        [Description("Đang xử lý")]
        Processing,
        [Description("Đã thanh toán")]
        Completed,
        [Description("Thanh toán một phần")]
        PartiallyPaid,
        [Description("Hoàn tiền")]
        Refunded,
        [Description("Hoàn tiền một phần")]
        PartiallyRefunded,
        [Description("Thất bại")]
        Failed,
        Paid
    }
    public enum PaymentMethod
    {
        VNPAY,
        COD
    }
    // Enum cho trạng thái yêu cầu hủy đơn
    public enum CancellationRequestStatus
    {
        None,           //0 Không có yêu cầu hủy
        Pending,        //1 Đang chờ duyệt
        Approved,       //2 Đã duyệt (cho phép hủy)
        Rejected        //3 Từ chối hủy
    }
}
