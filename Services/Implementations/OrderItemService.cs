using BusinessObjects.CustomDesigns;
using BusinessObjects.Orders;
using BusinessObjects.Products;
using DTOs.OrderItem;
using Repositories.Commons;
using Repositories.Helpers;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Interfaces;
using Services.Commons;
using Services.Extensions;
using Services.Helpers;
using Services.Helpers.Mappers;
using Services.Interfaces;
using System.Data;

namespace Services.Implementations
{
    public class OrderItemService : BaseService<OrderItem, Guid>, IOrderItemService
    {
        private readonly IOrderItemRepository _orderItemRepository;
        private readonly IRepositoryFactory _repositoryFactory;

        public OrderItemService(
            IOrderItemRepository orderItemRepository,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            ICurrentTime currentTime,
            IRepositoryFactory repositoryFactory)
            : base(orderItemRepository, currentUserService, unitOfWork, currentTime)
        {
            _orderItemRepository = orderItemRepository;
            _repositoryFactory = repositoryFactory;
        }

        public async Task<ApiResult<OrderItemDto>> CreateAsync(CreateOrderItemDto dto)
        {
            return await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                try
                {
                    // Validate input using business logic
                    if (!OrderItemBusinessLogic.ValidateOrderItemInput(dto))
                    {
                        return ApiResult<OrderItemDto>.Failure("Invalid order item input");
                    }

                    // Validate order exists
                    if (!await _orderItemRepository.ValidateOrderExistsAsync(dto.OrderId))
                    {
                        return ApiResult<OrderItemDto>.Failure("Order not found");
                    }

                    // Get repositories using factory pattern
                    var productRepo = _repositoryFactory.GetRepository<Product, Guid>();
                    var customDesignRepo = _repositoryFactory.GetRepository<CustomDesign, Guid>();
                    var productVariantRepo = _repositoryFactory.GetRepository<ProductVariant, Guid>();

                    // Get source entities and validate
                    var product = dto.ProductId.HasValue ? await productRepo.GetByIdAsync(dto.ProductId.Value) : null;
                    var customDesign = dto.CustomDesignId.HasValue ? await customDesignRepo.GetByIdAsync(dto.CustomDesignId.Value) : null;
                    var productVariant = dto.ProductVariantId.HasValue ? await productVariantRepo.GetByIdAsync(dto.ProductVariantId.Value) : null;

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

                    // Create order item using BaseService which handles audit fields
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

                    // Use BaseService UpdateAsync which handles audit fields
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
                // 1. Kiểm tra tồn tại
                var entity = await _orderItemRepository.GetByIdAsync(id);
                if (entity == null)
                    return ApiResult<bool>.Failure("Order item not found");

                // 2. Gọi BaseService.DeleteAsync (soft‐delete + audit + SaveChanges)
                var wasDeleted = await base.DeleteAsync(id);

                // 3. Trả về kết quả
                if (wasDeleted)
                    return ApiResult<bool>.Success(true, "Order item deleted successfully");
                else
                    return ApiResult<bool>.Failure("Failed to delete order item");
            },
            IsolationLevel.ReadCommitted);
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