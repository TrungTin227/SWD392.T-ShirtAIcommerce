using BusinessObjects.Common;
using BusinessObjects.Orders;
using BusinessObjects.Products;

namespace Services.Helpers
{
    public static class InventoryBusinessLogic
    {
        /// <summary>
        /// Validates if inventory can be reserved for order
        /// </summary>
        public static (bool CanReserve, List<string> Errors) ValidateInventoryReservation(
            List<(Guid? ProductId, Guid? ProductVariantId, int Quantity)> items,
            Dictionary<Guid, Product> products,
            Dictionary<Guid, ProductVariant> variants)
        {
            var errors = new List<string>();

            foreach (var item in items)
            {
                if (item.ProductId.HasValue && products.TryGetValue(item.ProductId.Value, out var product))
                {
                    //if (product.Quantity < item.Quantity)
                    //{
                    //    errors.Add($"Sản phẩm '{product.Name}' không đủ số lượng trong kho. Còn lại: {product.Quantity}, yêu cầu: {item.Quantity}");
                    //}
                }

                if (item.ProductVariantId.HasValue && variants.TryGetValue(item.ProductVariantId.Value, out var variant))
                {
                    if (variant.Quantity < item.Quantity)
                    {
                        errors.Add($"Biến thể sản phẩm (Size: {variant.Size}, Color: {variant.Color}) không đủ số lượng trong kho. Còn lại: {variant.Quantity}, yêu cầu: {item.Quantity}");
                    }
                }
            }

            return (!errors.Any(), errors);
        }

        /// <summary>
        /// Calculates inventory changes for order placement
        /// </summary>
        public static (List<InventoryChange> ProductChanges, List<InventoryChange> VariantChanges) 
            CalculateInventoryChanges(List<OrderItem> orderItems, InventoryOperation operation)
        {
            var productChanges = new List<InventoryChange>();
            var variantChanges = new List<InventoryChange>();

            foreach (var item in orderItems)
            {
                var quantityChange = operation == InventoryOperation.Reserve ? -item.Quantity : item.Quantity;

                if (item.ProductId.HasValue)
                {
                    productChanges.Add(new InventoryChange
                    {
                        ItemId = item.ProductId.Value,
                        QuantityChange = quantityChange,
                        Operation = operation,
                        OrderId = item.OrderId,
                        OrderItemId = item.Id,
                        Timestamp = DateTime.UtcNow
                    });
                }

                if (item.ProductVariantId.HasValue)
                {
                    variantChanges.Add(new InventoryChange
                    {
                        ItemId = item.ProductVariantId.Value,
                        QuantityChange = quantityChange,
                        Operation = operation,
                        OrderId = item.OrderId,
                        OrderItemId = item.Id,
                        Timestamp = DateTime.UtcNow
                    });
                }
            }

            return (productChanges, variantChanges);
        }

        /// <summary>
        /// Validates inventory restoration for order cancellation
        /// </summary>
        public static (bool CanRestore, List<string> Warnings) ValidateInventoryRestoration(
    List<OrderItem> orderItems,
    OrderStatus currentStatus)
        {
            var warnings = new List<string>();

            // Chỉ cho phép restore khi đơn đã hủy hoặc đã trả/hoàn trả
            var allowedStatuses = new[]
            {
        OrderStatus.Cancelled,
        OrderStatus.Returned
    };

            if (!allowedStatuses.Contains(currentStatus))
            {
                warnings.Add($"Không thể hoàn trả tồn kho cho đơn hàng có trạng thái: {currentStatus}");
                return (false, warnings);
            }

            // Cảnh báo nếu số lượng bất thường
            foreach (var item in orderItems)
            {
                if (item.Quantity > 1000)
                {
                    warnings.Add(
                        $"Số lượng lớn bất thường cho sản phẩm {item.ItemName}: {item.Quantity}. Kiểm tra dữ liệu.");
                }
            }

            return (true, warnings);
        }


        /// <summary>
        /// Calculates low stock alerts
        /// </summary>
        public static List<LowStockAlert> CalculateLowStockAlerts(
            List<Product> products,
            List<ProductVariant> variants,
            int lowStockThreshold = 10)
        {
            var alerts = new List<LowStockAlert>();

            // Check products
            foreach (var product in products.Where(p => !p.IsDeleted && p.Status == ProductStatus.Active))
            {
                //if (product.Quantity <= lowStockThreshold)
                //{
                //    alerts.Add(new LowStockAlert
                //    {
                //        Type = "Product",
                //        ItemId = product.Id,
                //        ItemName = product.Name,
                //        //CurrentStock = product.Quantity,
                //        Threshold = lowStockThreshold,
                //        //Severity = product.Quantity == 0 ? "Critical" : "Warning",
                //        //SuggestedReorderQuantity = CalculateReorderQuantity(product.Quantity, product.SoldCount)
                //    });
                //}
            }

            // Check variants
            foreach (var variant in variants.Where(v => v.IsActive))
            {
                if (variant.Quantity <= lowStockThreshold)
                {
                    alerts.Add(new LowStockAlert
                    {
                        Type = "ProductVariant",
                        ItemId = variant.Id,
                        ItemName = $"{variant.Product?.Name} - {variant.Color} - {variant.Size}",
                        CurrentStock = variant.Quantity,
                        Threshold = lowStockThreshold,
                        Severity = variant.Quantity == 0 ? "Critical" : "Warning",
                        SuggestedReorderQuantity = CalculateReorderQuantity(variant.Quantity, 0) // Variants don't have direct sold count
                    });
                }
            }

            return alerts.OrderBy(a => a.CurrentStock).ToList();
        }

        /// <summary>
        /// Validates concurrent inventory operations
        /// </summary>
        public static (bool IsValid, string ErrorMessage) ValidateConcurrentOperation(
            Guid itemId,
            int requestedQuantity,
            int currentStock,
            DateTime lastUpdated,
            TimeSpan maxOperationAge = default)
        {
            if (maxOperationAge == default)
                maxOperationAge = TimeSpan.FromMinutes(5);

            // Check if the stock data is too old (potential concurrency issue)
            if (DateTime.UtcNow - lastUpdated > maxOperationAge)
            {
                return (false, "Dữ liệu tồn kho có thể đã lỗi thời. Vui lòng làm mới và thử lại.");
            }

            if (currentStock < requestedQuantity)
            {
                return (false, $"Không đủ hàng trong kho. Còn lại: {currentStock}, yêu cầu: {requestedQuantity}");
            }

            return (true, string.Empty);
        }

        /// <summary>
        /// Calculates suggested reorder quantity
        /// </summary>
        public static int CalculateReorderQuantity(int currentStock, int recentSales, int leadTimeDays = 14)
        {
            // Simple reorder calculation based on recent sales velocity
            var dailySalesVelocity = recentSales / Math.Max(1, 30); // Assume sales are for last 30 days
            var leadTimeBuffer = dailySalesVelocity * leadTimeDays;
            var safetyStock = Math.Max(10, (int)(leadTimeBuffer * 0.5)); // 50% safety buffer
            
            var suggestedReorder = Math.Max(50, leadTimeBuffer + safetyStock - currentStock);
            
            return Math.Min(suggestedReorder, 1000); // Cap at 1000 units
        }

        /// <summary>
        /// Validates bulk inventory operations
        /// </summary>
        public static (bool IsValid, List<string> Errors) ValidateBulkInventoryOperation(
            List<InventoryChange> changes,
            int maxBatchSize = 100)
        {
            var errors = new List<string>();

            if (changes.Count > maxBatchSize)
            {
                errors.Add($"Số lượng thao tác vượt quá giới hạn cho phép: {maxBatchSize}");
            }

            // Check for duplicate operations on same item
            var duplicateItems = changes.GroupBy(c => new { c.ItemId, c.Operation })
                                       .Where(g => g.Count() > 1)
                                       .Select(g => g.Key);

            if (duplicateItems.Any())
            {
                errors.Add("Có thao tác trùng lặp trên cùng một sản phẩm");
            }

            // Validate individual changes
            foreach (var change in changes)
            {
                if (change.QuantityChange == 0)
                {
                    errors.Add($"Thay đổi số lượng không thể bằng 0 cho sản phẩm {change.ItemId}");
                }

                if (Math.Abs(change.QuantityChange) > 10000)
                {
                    errors.Add($"Thay đổi số lượng quá lớn cho sản phẩm {change.ItemId}: {change.QuantityChange}");
                }
            }

            return (!errors.Any(), errors);
        }

        /// <summary>
        /// Creates inventory audit trail entry
        /// </summary>
        public static InventoryAuditEntry CreateAuditEntry(
            InventoryChange change,
            Guid? userId,
            string reason)
        {
            return new InventoryAuditEntry
            {
                Id = Guid.NewGuid(),
                ItemId = change.ItemId,
                Operation = change.Operation,
                QuantityChange = change.QuantityChange,
                OrderId = change.OrderId,
                OrderItemId = change.OrderItemId,
                UserId = userId,
                Reason = reason,
                Timestamp = change.Timestamp,
                CreatedAt = DateTime.UtcNow
            };
        }
    }

    // Supporting classes
    public enum InventoryOperation
    {
        Reserve,
        Release,
        Adjust,
        Restock
    }

    public class InventoryChange
    {
        public Guid ItemId { get; set; }
        public int QuantityChange { get; set; }
        public InventoryOperation Operation { get; set; }
        public Guid OrderId { get; set; }
        public Guid OrderItemId { get; set; }
        public DateTime Timestamp { get; set; }
    }

    public class LowStockAlert
    {
        public string Type { get; set; } = string.Empty; // "Product" or "ProductVariant"
        public Guid ItemId { get; set; }
        public string ItemName { get; set; } = string.Empty;
        public int CurrentStock { get; set; }
        public int Threshold { get; set; }
        public string Severity { get; set; } = string.Empty; // "Warning" or "Critical"
        public int SuggestedReorderQuantity { get; set; }
    }

    public class InventoryAuditEntry
    {
        public Guid Id { get; set; }
        public Guid ItemId { get; set; }
        public InventoryOperation Operation { get; set; }
        public int QuantityChange { get; set; }
        public Guid? OrderId { get; set; }
        public Guid? OrderItemId { get; set; }
        public Guid? UserId { get; set; }
        public string Reason { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}