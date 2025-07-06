using BusinessObjects.Products;
using DTOs.Products;
using Repositories.Commons;
using Repositories.Helpers;

namespace Services.Helpers.Mappers
{
    public static class ProductVariantMapper
    {
        #region Entity to DTO
        public static ProductVariantDto ToDto(ProductVariant entity)
        {
            if (entity == null) return null;

            return new ProductVariantDto
            {
                Id = entity.Id,
                ProductId = entity.ProductId,
                Color = entity.Color,
                Size = entity.Size,
                VariantSku = entity.VariantSku,
                Quantity = entity.Quantity,
                PriceAdjustment = entity.PriceAdjustment,
                ImageUrl = entity.ImageUrl,
                IsActive = entity.IsActive,
                ProductName = entity.Product?.Name // Nếu có navigation property
            };
        }

        public static IList<ProductVariantDto> ToDto(IEnumerable<ProductVariant> entities)
        {
            if (entities == null) return new List<ProductVariantDto>();

            return entities.Select(ToDto).ToList();
        }

        public static IReadOnlyList<ProductVariantDto> ToReadOnlyDto(IEnumerable<ProductVariant> entities)
        {
            if (entities == null) return new List<ProductVariantDto>();

            return entities.Select(ToDto).ToList().AsReadOnly();
        }

        #endregion

        #region CreateDTO to Entity
        public static ProductVariant ToEntity(ProductVariantCreateDto dto)
        {
            if (dto == null) return null;

            return new ProductVariant
            {
                Id = Guid.NewGuid(), // Tạo ID mới
                ProductId = dto.ProductId,
                Color = dto.Color,
                Size = dto.Size,
                VariantSku = dto.VariantSku,
                Quantity = dto.Quantity,
                PriceAdjustment = dto.PriceAdjustment,
                ImageUrl = dto.ImageUrl,
                IsActive = dto.IsActive
                // Audit fields sẽ được set trong service
            };
        }

        public static IEnumerable<ProductVariant> ToEntity(IEnumerable<ProductVariantCreateDto> dtos)
        {
            if (dtos == null) return new List<ProductVariant>();

            return dtos.Select(ToEntity);
        }

        #endregion

        #region UpdateDTO to Entity
        public static ProductVariant ToEntity(ProductVariantUpdateDto dto)
        {
            if (dto == null) return null;

            // Parse enum từ string
            if (!Enum.TryParse<ProductColor>(dto.Color, true, out var color))
            {
                throw new ArgumentException($"Invalid color value: {dto.Color}");
            }

            if (!Enum.TryParse<ProductSize>(dto.Size, true, out var size))
            {
                throw new ArgumentException($"Invalid size value: {dto.Size}");
            }

            return new ProductVariant
            {
                Id = dto.Id,
                ProductId = dto.ProductId,
                Color = color,
                Size = size,
                VariantSku = dto.VariantSku,
                Quantity = dto.Quantity,
                PriceAdjustment = dto.PriceAdjustment,
                ImageUrl = dto.ImageUrl,
                IsActive = dto.IsActive
                // Audit fields sẽ được set trong service
            };
        }

        public static void MapToEntity(ProductVariantUpdateDto dto, ProductVariant entity)
        {
            if (dto == null || entity == null) return;

            // Parse enum từ string
            if (!Enum.TryParse<ProductColor>(dto.Color, true, out var color))
            {
                throw new ArgumentException($"Invalid color value: {dto.Color}");
            }

            if (!Enum.TryParse<ProductSize>(dto.Size, true, out var size))
            {
                throw new ArgumentException($"Invalid size value: {dto.Size}");
            }

            entity.ProductId = dto.ProductId;
            entity.Color = color;
            entity.Size = size;
            entity.VariantSku = dto.VariantSku;
            entity.Quantity = dto.Quantity;
            entity.PriceAdjustment = dto.PriceAdjustment;
            entity.ImageUrl = dto.ImageUrl;
            entity.IsActive = dto.IsActive;
            // Không thay đổi Id, audit fields sẽ được set trong service
        }

        public static IEnumerable<ProductVariant> ToEntity(IEnumerable<ProductVariantUpdateDto> dtos)
        {
            if (dtos == null) return new List<ProductVariant>();

            return dtos.Select(ToEntity);
        }

        #endregion

        #region PagedList Mapping
        public static ApiResult<PagedList<ProductVariantDto>> ToPagedDto(
        PagedList<ProductVariant> pagedEntities)
        {
            try
            {
                // Nếu không có dữ liệu, trả về một PagedList<ProductVariantDto> rỗng
                if (pagedEntities == null)
                {
                    var empty = new PagedList<ProductVariantDto>(
                        new List<ProductVariantDto>(),
                        count: 0,
                        pageNumber: 1,
                        pageSize: 10);

                    return ApiResult<PagedList<ProductVariantDto>>.Success(
                        empty,
                        "No product variants found");
                }

                // Map trực tiếp từng ProductVariant -> ProductVariantDto
                var dtoItems = pagedEntities
                    .Select(e => new ProductVariantDto
                    {
                        Id = e.Id,
                        Color = e.Color,
                        Size = e.Size,
                        ProductId = e.ProductId,
                        // … map thêm các trường khác nếu cần …
                    })
                    .ToList();

                // Tạo lại PagedList với DTO và giữ nguyên metadata
                var dtoPaged = new PagedList<ProductVariantDto>(
                    dtoItems,
                    pagedEntities.MetaData.TotalCount,
                    pagedEntities.MetaData.CurrentPage,
                    pagedEntities.MetaData.PageSize);

                return ApiResult<PagedList<ProductVariantDto>>.Success(
                    dtoPaged,
                    "Product variants retrieved successfully");
            }
            catch (Exception ex)
            {
                // Trả về Failure nếu có lỗi trong quá trình map
                return ApiResult<PagedList<ProductVariantDto>>.Failure(
                    "Error mapping paged product variants",
                    ex);
            }
        }

        #endregion

        #region Validation Helpers
        public static bool IsValidColor(string color)
        {
            return Enum.TryParse<ProductColor>(color, true, out _);
        }

        public static bool IsValidSize(string size)
        {
            return Enum.TryParse<ProductSize>(size, true, out _);
        }

        public static ProductColor ParseColor(string color)
        {
            if (Enum.TryParse<ProductColor>(color, true, out var result))
            {
                return result;
            }
            throw new ArgumentException($"Invalid color value: {color}");
        }

        public static ProductSize ParseSize(string size)
        {
            if (Enum.TryParse<ProductSize>(size, true, out var result))
            {
                return result;
            }
            throw new ArgumentException($"Invalid size value: {size}");
        }

        #endregion
    }
}