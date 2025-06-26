using BusinessObjects.Orders;
using DTOs.OrderItem;
using Repositories.Commons;
using Repositories.Helpers;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Interfaces;
using Services.Commons;
using Services.Extensions;
using Services.Helpers;
using Services.Interfaces;
using System.Data;

namespace Services.Implementations
{
    public class OrderItemService : BaseService<OrderItem, Guid>, IOrderItemService
    {
        private readonly IOrderItemRepository _orderItemRepository;
        private readonly IProductRepository _productRepository;
        private readonly ICustomDesignRepository _customDesignRepository;
        private readonly IProductVariantRepository _productVariantRepository;

        public OrderItemService(
            IOrderItemRepository orderItemRepository,
            IProductRepository productRepository,
            ICustomDesignRepository customDesignRepository,
            IProductVariantRepository productVariantRepository,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            ICurrentTime currentTime)
            : base(orderItemRepository, currentUserService, unitOfWork, currentTime)
        {
            _orderItemRepository = orderItemRepository;
            _productRepository = productRepository;
            _customDesignRepository = customDesignRepository;
            _productVariantRepository = productVariantRepository;
        }

        public async Task<ApiResult<OrderItemDto>> CreateAsync(CreateOrderItemDto dto)
        {
            return await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                try
                {
                    // Validate input
                    if (!OrderItemBusinessLogic.ValidateOrderItemInput(dto))
                    {
                        return ApiResult<OrderItemDto>.Failure("Invalid order item input");
                    }

                    // Validate order exists
                    if (!await _orderItemRepository.ValidateOrderExistsAsync(dto.OrderId))
                    {
                        return ApiResult<OrderItemDto>.Failure("Order not found");
                    }

                    // Get source entities and validate
                    var product = dto.ProductId.HasValue ? await _productRepository.GetByIdAsync(dto.ProductId.Value) : null;
                    var customDesign = dto.CustomDesignId.HasValue ? await _customDesignRepository.GetByIdAsync(dto.CustomDesignId.Value) : null;
                    var productVariant = dto.ProductVariantId.HasValue ? await _productVariantRepository.GetByIdAsync(dto.ProductVariantId.Value) : null;

                    if (dto.ProductId.HasValue && product == null)
                        return ApiResult<OrderItemDto>.Failure("Product not found");

                    if (dto.CustomDesignId.HasValue && customDesign == null)
                        return ApiResult<OrderItemDto>.Failure("Custom design not found");

                    if (dto.ProductVariantId.HasValue && productVariant == null)
                        return ApiResult<OrderItemDto>.Failure("Product variant not found");

                    // Validate color and size
                    if (!OrderItemBusinessLogic.ValidateColorAndSize(dto.SelectedColor, dto.SelectedSize, dto.ProductVariantId))
                    {
                        return ApiResult<OrderItemDto>.Failure("Invalid color or size selection");
                    }

                    // Get price from appropriate source
                    var unitPrice = OrderItemBusinessLogic.GetPriceFromSource(product, customDesign, productVariant);

                    // Create order item
                    var orderItem = OrderItemMapper.ToEntity(dto, unitPrice);

                    var createdOrderItem = await CreateAsync(orderItem);
                    var result = OrderItemMapper.ToDto(createdOrderItem);

                    return ApiResult<OrderItemDto>.Success(result, "Order item created successfully");
                }
                catch (Exception ex)
                {
                    return ApiResult<OrderItemDto>.Failure("Failed to create order item", ex);
                }
            }, IsolationLevel.ReadCommitted);
        }

        public async Task<ApiResult<OrderItemDto>> UpdateAsync(UpdateOrderItemDto dto)
        {
            return await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                try
                {
                    var existingOrderItem = await _orderItemRepository.GetWithDetailsAsync(dto.Id);
                    if (existingOrderItem == null)
                    {
                        return ApiResult<OrderItemDto>.Failure("Order item not found");
                    }

                    // Validate color and size
                    if (!OrderItemBusinessLogic.ValidateColorAndSize(dto.SelectedColor, dto.SelectedSize, existingOrderItem.ProductVariantId))
                    {
                        return ApiResult<OrderItemDto>.Failure("Invalid color or size selection");
                    }

                    // Update using mapper with business logic
                    OrderItemMapper.UpdateEntity(existingOrderItem, dto);

                    var updatedOrderItem = await UpdateAsync(existingOrderItem);
                    var result = OrderItemMapper.ToDto(updatedOrderItem);

                    return ApiResult<OrderItemDto>.Success(result, "Order item updated successfully");
                }
                catch (Exception ex)
                {
                    return ApiResult<OrderItemDto>.Failure("Failed to update order item", ex);
                }
            }, IsolationLevel.ReadCommitted);
        }

        public async Task<ApiResult<bool>> DeleteAsync(Guid id)
        {
            return await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                try
                {
                    var exists = await _orderItemRepository.GetByIdAsync(id);
                    if (exists == null)
                    {
                        return ApiResult<bool>.Failure("Order item not found");
                    }

                    var result = await DeleteAsync(id);
                    return ApiResult<bool>.Success(result, "Order item deleted successfully");
                }
                catch (Exception ex)
                {
                    return ApiResult<bool>.Failure("Failed to delete order item", ex);
                }
            }, IsolationLevel.ReadCommitted);
        }

        public async Task<ApiResult<OrderItemDto?>> GetByIdAsync(Guid id)
        {
            try
            {
                var orderItem = await _orderItemRepository.GetWithDetailsAsync(id);
                if (orderItem == null)
                {
                    return ApiResult<OrderItemDto?>.Failure("Order item not found");
                }

                var result = OrderItemMapper.ToDto(orderItem);
                return ApiResult<OrderItemDto?>.Success(result);
            }
            catch (Exception ex)
            {
                return ApiResult<OrderItemDto?>.Failure("Failed to get order item", ex);
            }
        }

        public async Task<ApiResult<PagedList<OrderItemDto>>> GetOrderItemsAsync(OrderItemQueryDto query)
        {
            try
            {
                var orderItems = await _orderItemRepository.GetOrderItemsAsync(query);
                var result = OrderItemMapper.ToPagedDto(orderItems);
                return ApiResult<PagedList<OrderItemDto>>.Success(result);
            }
            catch (Exception ex)
            {
                return ApiResult<PagedList<OrderItemDto>>.Failure("Failed to get order items", ex);
            }
        }

        public async Task<ApiResult<IEnumerable<OrderItemDto>>> GetByOrderIdAsync(Guid orderId)
        {
            try
            {
                var orderItems = await _orderItemRepository.GetByOrderIdAsync(orderId);
                var result = OrderItemMapper.ToDtoList(orderItems);
                return ApiResult<IEnumerable<OrderItemDto>>.Success(result);
            }
            catch (Exception ex)
            {
                return ApiResult<IEnumerable<OrderItemDto>>.Failure("Failed to get order items", ex);
            }
        }

        public async Task<ApiResult<decimal>> GetOrderTotalAsync(Guid orderId)
        {
            try
            {
                var total = await _orderItemRepository.GetOrderTotalAsync(orderId);
                return ApiResult<decimal>.Success(total);
            }
            catch (Exception ex)
            {
                return ApiResult<decimal>.Failure("Failed to calculate order total", ex);
            }
        }

        public async Task<ApiResult<int>> GetOrderItemCountAsync(Guid orderId)
        {
            try
            {
                var count = await _orderItemRepository.GetOrderItemCountAsync(orderId);
                return ApiResult<int>.Success(count);
            }
            catch (Exception ex)
            {
                return ApiResult<int>.Failure("Failed to get order item count", ex);
            }
        }
    }
}