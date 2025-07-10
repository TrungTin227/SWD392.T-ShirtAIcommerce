using BusinessObjects.Products;
using DTOs.Products;
using Repositories.Commons;
using Repositories.Helpers;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Interfaces;
using Services.Commons;
using Services.Helpers.Mappers;
using Services.Interfaces;

namespace Services.Implementations
{
    public class ProductVariantService : BaseService<ProductVariant, Guid>, IProductVariantService
    {
        private readonly IProductVariantRepository _productVariantRepository;

        public ProductVariantService(
            IProductVariantRepository productVariantRepository,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            ICurrentTime currentTime)
            : base(productVariantRepository, currentUserService, unitOfWork, currentTime)
        {
            _productVariantRepository = productVariantRepository;
        }

        public async Task<ApiResult<ProductVariantDto>> CreateAsync(ProductVariantCreateDto dto)
        {
            // Bắt đầu transaction
            await using var tx = await _unitOfWork.Context.Database.BeginTransactionAsync();
            try
            {
                // 1. Kiểm tra duplicate
                var existingVariant = await _productVariantRepository.FirstOrDefaultAsync(
                    x => x.ProductId == dto.ProductId &&
                         x.Color     == dto.Color &&
                         x.Size      == dto.Size);
                if (existingVariant != null)
                    return ApiResult<ProductVariantDto>.Failure("Product variant with same color and size already exists");

                // 2. Map DTO và lưu variant
                var entity = ProductVariantMapper.ToEntity(dto);
                // (nếu cần set audit fields)
                SetAuditFieldsForCreate(entity);
                var savedVariant = await _productVariantRepository.AddAsync(entity);

                // 3. Thêm image (nếu có)
                if (!string.IsNullOrWhiteSpace(dto.ImageUrl))
                {
                    var image = new ProductImage
                    {
                        Id        = Guid.NewGuid(),
                        ProductId = dto.ProductId,
                        Url       = dto.ImageUrl,
                        IsPrimary = false
                    };
                    await _unitOfWork.Context.ProductImages.AddAsync(image);
                }

                // 4. Commit cả hai thao tác
                await _unitOfWork.SaveChangesAsync();
                await tx.CommitAsync();

                // 5. Map kết quả và trả về
                var mappedDto = ProductVariantMapper.ToDto(savedVariant);
                return ApiResult<ProductVariantDto>.Success(mappedDto, "Product variant created successfully");
            }
            catch (Exception ex)
            {
                // Rollback khi có bất kỳ lỗi nào
                await tx.RollbackAsync();
                return ApiResult<ProductVariantDto>.Failure($"Error creating product variant: {ex.Message}");
            }
        }
        public async Task<ApiResult<ProductVariantDto>> UpdateAsync(ProductVariantUpdateDto dto)
        {
            // Khởi transaction trên cùng DbContext
            await using var tx = await _unitOfWork.Context.Database.BeginTransactionAsync();
            try
            {
                // 1. Lấy entity hiện tại
                var existingEntity = await _productVariantRepository.GetByIdAsync(dto.Id);
                if (existingEntity == null)
                    return ApiResult<ProductVariantDto>.Failure("Product variant not found");

                // 2. Validate giá trị color/size
                if (!ProductVariantMapper.IsValidColor(dto.Color))
                    return ApiResult<ProductVariantDto>.Failure($"Invalid color value: {dto.Color}");
                if (!ProductVariantMapper.IsValidSize(dto.Size))
                    return ApiResult<ProductVariantDto>.Failure($"Invalid size value: {dto.Size}");

                var color = ProductVariantMapper.ParseColor(dto.Color);
                var size = ProductVariantMapper.ParseSize(dto.Size);

                // 3. Kiểm tra duplicate (ngoại trừ chính nó)
                var duplicate = await _productVariantRepository.FirstOrDefaultAsync(
                    x => x.ProductId == dto.ProductId &&
                         x.Color     == color &&
                         x.Size      == size &&
                         x.Id        != dto.Id);
                if (duplicate != null)
                    return ApiResult<ProductVariantDto>.Failure("Product variant with same color and size already exists");

                // 4. Map DTO lên entity và set lại audit fields
                ProductVariantMapper.MapToEntity(dto, existingEntity);
                SetAuditFieldsForUpdate(existingEntity);

                // 5. Cập nhật repository
                await _productVariantRepository.UpdateAsync(existingEntity);

                // 6. Lưu và commit transaction
                await _unitOfWork.SaveChangesAsync();
                await tx.CommitAsync();

                // 7. Trả về kết quả
                var mappedResult = ProductVariantMapper.ToDto(existingEntity);
                return ApiResult<ProductVariantDto>.Success(mappedResult, "Product variant updated successfully");
            }
            catch (ArgumentException ex)
            {
                await tx.RollbackAsync();
                return ApiResult<ProductVariantDto>.Failure(ex.Message);
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return ApiResult<ProductVariantDto>.Failure($"Error updating product variant: {ex.Message}");
            }
        }

        public async Task<ApiResult<bool>> DeleteAsync(Guid id)
        {
            try
            {
                // 1. Gọi đúng repository (không đệ quy)
                var deleteResult = await _productVariantRepository.DeleteAsync(id);

                // 2. Kiểm tra deleteResult thay vì IsSuccess
                if (deleteResult)
                {
                    return ApiResult<bool>.Success(true, "Product variant deleted successfully");
                }

                return ApiResult<bool>.Failure("Product variant not found");
            }
            catch (Exception ex)
            {
                return ApiResult<bool>.Failure($"Error deleting product variant: {ex.Message}");
            }
        }

        public async Task<ApiResult<ProductVariantDto?>> GetByIdAsync(Guid id)
        {
            try
            {
                var entity = await _productVariantRepository.GetByIdAsync(id, x => x.Product);
                if (entity == null)
                {
                    return ApiResult<ProductVariantDto?>.Failure("Product variant not found");
                }

                var mappedResult = ProductVariantMapper.ToDto(entity);
                return ApiResult<ProductVariantDto?>.Success(mappedResult, "Product variant retrieved successfully");
            }
            catch (Exception ex)
            {
                return ApiResult<ProductVariantDto?>.Failure($"Error retrieving product variant: {ex.Message}");
            }
        }

        public async Task<ApiResult<IReadOnlyList<ProductVariantDto>>> GetVariantsByProductIdAsync(Guid productId)
        {
            try
            {
                var entities = await _productVariantRepository.GetVariantsByProductIdAsync(productId);
                var mappedResults = ProductVariantMapper.ToReadOnlyDto(entities);

                return ApiResult<IReadOnlyList<ProductVariantDto>>.Success(
                    mappedResults,
                    $"Retrieved {entities.Count} product variants successfully");
            }
            catch (Exception ex)
            {
                return ApiResult<IReadOnlyList<ProductVariantDto>>.Failure($"Error retrieving product variants: {ex.Message}");
            }
        }

        public async Task<ApiResult<PagedList<ProductVariantDto>>> GetPagedVariantsByProductIdAsync(Guid productId, int pageNumber, int pageSize)
        {
            try
            {
                var pagedEntities = await _productVariantRepository.GetPagedVariantsByProductIdAsync(productId, pageNumber, pageSize);
                var pagedResult = ProductVariantMapper.ToPagedDto(pagedEntities);

                return ApiResult<PagedList<ProductVariantDto>>.Success(pagedResult, "Product variants retrieved successfully");
            }
            catch (Exception ex)
            {
                return ApiResult<PagedList<ProductVariantDto>>.Failure($"Error retrieving paged product variants: {ex.Message}");
            }
        }

        public async Task<ApiResult<PagedList<ProductVariantDto>>> GetPagedAsync(int pageNumber, int pageSize)
        {
            try
            {
                var pagedEntities = await _productVariantRepository.GetPagedAsync(
                    pageNumber,
                    pageSize,
                    orderBy: q => q.OrderBy(x => x.ProductId).ThenBy(x => x.Color).ThenBy(x => x.Size),
                    includes: x => x.Product);

                var pagedResult = ProductVariantMapper.ToPagedDto(pagedEntities);

                return ApiResult<PagedList<ProductVariantDto>>.Success(pagedResult, "Product variants retrieved successfully");
            }
            catch (Exception ex)
            {
                return ApiResult<PagedList<ProductVariantDto>>.Failure($"Error retrieving paged product variants: {ex.Message}");
            }
        }

        public async Task<ApiResult<bool>> BulkCreateAsync(IEnumerable<ProductVariantCreateDto> dtos)
        {
            try
            {
                var entities = ProductVariantMapper.ToEntity(dtos);

                // Set audit fields for each entity
                foreach (var entity in entities)
                {
                    SetAuditFieldsForCreate(entity);
                }

                await _productVariantRepository.AddRangeAsync(entities);
                await _unitOfWork.SaveChangesAsync();

                return ApiResult<bool>.Success(true, $"Created {entities.Count()} product variants successfully");
            }
            catch (Exception ex)
            {
                return ApiResult<bool>.Failure($"Error bulk creating product variants: {ex.Message}");
            }
        }

        public async Task<ApiResult<bool>> BulkUpdateAsync(IEnumerable<ProductVariantUpdateDto> dtos)
        {
            try
            {
                var entities = ProductVariantMapper.ToEntity(dtos);

                // Set audit fields for each entity
                foreach (var entity in entities)
                {
                    SetAuditFieldsForUpdate(entity);
                }

                await _productVariantRepository.UpdateRangeAsync(entities);
                await _unitOfWork.SaveChangesAsync();

                return ApiResult<bool>.Success(true, $"Updated {entities.Count()} product variants successfully");
            }
            catch (Exception ex)
            {
                return ApiResult<bool>.Failure($"Error bulk updating product variants: {ex.Message}");
            }
        }

        public async Task<ApiResult<bool>> BulkDeleteAsync(IEnumerable<Guid> ids)
        {
            await using var tx = await _unitOfWork.Context.Database.BeginTransactionAsync();
            try
            {
                await _productVariantRepository.DeleteRangeAsync(ids);
                await _unitOfWork.SaveChangesAsync();
                await tx.CommitAsync();

                return ApiResult<bool>.Success(true, $"Deleted {ids.Count()} variants successfully");
            }
            catch (Exception ex)
            {
                await tx.RollbackAsync();
                return ApiResult<bool>.Failure($"Error deleting variants: {ex.Message}");
            }
        }

        private void SetAuditFieldsForCreate(ProductVariant entity)
        {
            var currentUserId = _currentUserService.GetUserId() ?? Guid.Empty;
            var now = _currentTime.GetVietnamTime();

            entity.CreatedAt = now;
            entity.UpdatedAt = now;
            entity.CreatedBy = currentUserId;
            entity.UpdatedBy = currentUserId;

            if (entity.Id == Guid.Empty)
            {
                entity.Id = Guid.NewGuid();
            }
        }

        private void SetAuditFieldsForUpdate(ProductVariant entity)
        {
            var currentUserId = _currentUserService.GetUserId() ?? Guid.Empty;
            var now = _currentTime.GetVietnamTime();

            entity.UpdatedAt = now;
            entity.UpdatedBy = currentUserId;
        }
    }
}