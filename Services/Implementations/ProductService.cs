using BusinessObjects.Products;
using DTOs.Common;
using DTOs.Product;
using Repositories.Commons;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Interfaces;
using Services.Commons;
using Services.Extensions;
using Services.Interfaces;

namespace Services.Implementations
{
    public class ProductService : BaseService<Product, Guid>, IProductService
    {
        private readonly IProductRepository _productRepository;

        public ProductService(
            IProductRepository productRepository,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            ICurrentTime currentTime)
            : base(productRepository, currentUserService, unitOfWork, currentTime)
        {
            _productRepository = productRepository;
        }

        public async Task<ApiResult<PagedResponse<ProductDto>>> GetPagedAsync(ProductFilterDto filter)
        {
            try
            {
                // 1. Repo đã include(p => p.Images) rồi
                var pagedProducts = await _productRepository.GetPagedAsync(filter);

                // 2. Ánh xạ, MapToDto sẽ lấy lên List<string> Images
                var productDtos = pagedProducts
                    .Select(MapToDto)
                    .ToList();

                // 3. Tạo response như trước
                var response = new PagedResponse<ProductDto>
                {
                    Data            = productDtos,
                    CurrentPage     = pagedProducts.MetaData.CurrentPage,
                    TotalPages      = pagedProducts.MetaData.TotalPages,
                    PageSize        = pagedProducts.MetaData.PageSize,
                    TotalCount      = pagedProducts.MetaData.TotalCount,
                    HasNextPage     = pagedProducts.MetaData.CurrentPage < pagedProducts.MetaData.TotalPages,
                    HasPreviousPage = pagedProducts.MetaData.CurrentPage > 1,
                    IsSuccess       = true,
                    Message         = "Products retrieved successfully"
                };

                return ApiResult<PagedResponse<ProductDto>>.Success(response);
            }
            catch (Exception ex)
            {
                return ApiResult<PagedResponse<ProductDto>>.Failure($"Error retrieving products: {ex.Message}", ex);
            }
        }

        public async Task<ApiResult<ProductDto>> GetByIdAsync(Guid id)
        {
            try
            {
                // Load cả Category và Images
                var product = await _productRepository.GetByIdAsync(
                    id,
                    p => p.Category,
                    p => p.Images      // ← include thêm collection Images
                );
                if (product == null || product.IsDeleted)
                {
                    return ApiResult<ProductDto>.Failure("Product not found");
                }

                return ApiResult<ProductDto>.Success(MapToDto(product));
            }
            catch (Exception ex)
            {
                return ApiResult<ProductDto>.Failure($"Error retrieving product: {ex.Message}", ex);
            }
        }

        public async Task<ApiResult<ProductDto>> GetBySkuAsync(string sku)
        {
            try
            {
                var product = await _productRepository.GetBySkuAsync(sku);
                if (product == null)
                {
                    return ApiResult<ProductDto>.Failure("Product not found");
                }

                return ApiResult<ProductDto>.Success(MapToDto(product));
            }
            catch (Exception ex)
            {
                return ApiResult<ProductDto>.Failure($"Error retrieving product: {ex.Message}", ex);
            }
        }

        public async Task<ApiResult<ProductDto>> CreateAsync(CreateProductDto dto)
        {
            return await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                try
                {
                    // Validate unique constraints
                    if (!string.IsNullOrWhiteSpace(dto.Sku) && await _productRepository.IsSkuExistsAsync(dto.Sku))
                        return ApiResult<ProductDto>.Failure("SKU already exists");

                    if (!string.IsNullOrWhiteSpace(dto.Slug) && await _productRepository.IsSlugExistsAsync(dto.Slug))
                        return ApiResult<ProductDto>.Failure("Slug already exists");

                    // Validate business rules
                    if (dto.SalePrice.HasValue && dto.SalePrice >= dto.Price)
                        return ApiResult<ProductDto>.Failure("Sale price must be less than regular price");

                    // Map & create product
                    var product = MapToEntity(dto);

                    // Thêm logic để lưu ProductImages nếu có
                    if (dto.Images != null && dto.Images.Any())
                    {
                        product.Images = dto.Images.Select(imageDto => new ProductImage
                        {
                            Id = Guid.NewGuid(),
                            ProductId = product.Id,
                            Url = imageDto.Url,
                            IsPrimary = imageDto.IsPrimary
                        }).ToList();
                    }

                    var result = await CreateAsync(product);
                    return ApiResult<ProductDto>.Success(MapToDto(result));
                }
                catch (Exception ex)
                {
                    return ApiResult<ProductDto>.Failure($"Error creating product: {ex.Message}", ex);
                }
            });
        }
        public async Task<ApiResult<ProductDto>> UpdateAsync(Guid id, UpdateProductDto dto)
        {
            return await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                // 1. Lấy product kèm collection ProductImages
                var product = await _productRepository.GetByIdAsync(id, p => p.Images);
                if (product == null || product.IsDeleted)
                    return ApiResult<ProductDto>.Failure("Product not found");

                // 2. Validate unique SKU/Slug
                if (!string.IsNullOrWhiteSpace(dto.Sku) && dto.Sku != product.Sku &&
                    await _productRepository.IsSkuExistsAsync(dto.Sku, id))
                {
                    return ApiResult<ProductDto>.Failure("SKU already exists");
                }

                if (!string.IsNullOrWhiteSpace(dto.Slug) && dto.Slug != product.Slug &&
                    await _productRepository.IsSlugExistsAsync(dto.Slug, id))
                {
                    return ApiResult<ProductDto>.Failure("Slug already exists");
                }

                // 3. Validate business rules về giá
                var newPrice = dto.Price     ?? product.Price;
                var newSalePrice = dto.SalePrice ?? product.SalePrice;
                if (newSalePrice.HasValue && newSalePrice >= newPrice)
                    return ApiResult<ProductDto>.Failure("Sale price must be less than regular price");

                // 4. Map các trường cơ bản từ DTO lên entity
                if (dto.Name            != null) product.Name            = dto.Name;
                if (dto.Description     != null) product.Description     = dto.Description;
                if (dto.Price           != null) product.Price           = dto.Price.Value;
                if (dto.SalePrice       != null) product.SalePrice       = dto.SalePrice;
                if (dto.Sku             != null) product.Sku             = dto.Sku;
                if (dto.CategoryId      != null) product.CategoryId      = dto.CategoryId.Value;
                if (dto.Material        != null) product.Material        = dto.Material.Value;
                if (dto.Season          != null) product.Season          = dto.Season.Value;
                if (dto.MetaTitle       != null) product.MetaTitle       = dto.MetaTitle;
                if (dto.MetaDescription != null) product.MetaDescription = dto.MetaDescription;
                if (dto.Slug            != null) product.Slug            = dto.Slug;
                if (dto.Status          != null) product.Status          = dto.Status.Value;

                // 5. Xử lý danh sách ảnh nếu có
                if (dto.Images != null)
                {
                    // 5.1 Xóa toàn bộ ảnh cũ (EF sẽ track và xoá khi SaveChanges)
                    _unitOfWork.Context.ProductImages.RemoveRange(product.Images);

                    // 5.2 Thêm lại ảnh mới từ DTO
                    foreach (var imgDto in dto.Images)
                    {
                        product.Images.Add(new ProductImage
                        {
                            Id        = Guid.NewGuid(),
                            ProductId = id,
                            Url       = imgDto.Url,
                            IsPrimary = imgDto.IsPrimary
                        });
                    }
                }

                // 6. Cập nhật audit fields, nếu bạn có (ví dụ UpdatedAt, UpdatedBy)
                product.UpdatedAt = _currentTime.GetVietnamTime();
                product.UpdatedBy = _currentUserService.GetUserId() ?? Guid.Empty;

                // 7. Gọi repository update (nếu base service của bạn đã tự gọi SaveChanges thì bạn không cần SaveChanges ở đây)
                var updated = await UpdateAsync(product);

                // 8. Trả về kết quả
                return ApiResult<ProductDto>.Success(MapToDto(updated), "Product updated successfully");
            });
        }
        public async Task<ApiResult<BatchOperationResultDTO>> BulkDeleteAsync(BatchIdsRequest request)
        {
            return await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                var result = new BatchOperationResultDTO
                {
                    TotalRequested = request.Ids.Count
                };

                try
                {
                    foreach (var id in request.Ids)
                    {
                        try
                        {
                            var success = await _repository.SoftDeleteAsync(id, _currentUserService.GetUserId());
                            if (success)
                            {
                                result.SuccessCount++;
                                result.SuccessIds.Add(id.ToString());
                            }
                            else
                            {
                                result.FailureCount++;
                                result.Errors.Add(new BatchOperationErrorDTO
                                {
                                    Id = id.ToString(),
                                    ErrorMessage = "Product not found or already deleted",
                                    ErrorCode = "NOT_FOUND"
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            result.FailureCount++;
                            result.Errors.Add(new BatchOperationErrorDTO
                            {
                                Id = id.ToString(),
                                ErrorMessage = ex.Message,
                                ErrorCode = "DELETE_ERROR"
                            });
                        }
                    }

                    await _unitOfWork.SaveChangesAsync();

                    result.Message = result.IsCompleteSuccess
                        ? "All products deleted successfully"
                        : result.IsPartialSuccess
                            ? $"Partially completed: {result.SuccessCount} deleted, {result.FailureCount} failed"
                            : "All operations failed";

                    return ApiResult<BatchOperationResultDTO>.Success(result);
                }
                catch (Exception ex)
                {
                    return ApiResult<BatchOperationResultDTO>.Failure($"Bulk delete failed: {ex.Message}", ex);
                }
            });
        }

        public async Task<ApiResult<BatchOperationResultDTO>> BulkRestoreAsync(BatchIdsRequest request)
        {
            return await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                var result = new BatchOperationResultDTO
                {
                    TotalRequested = request.Ids.Count
                };

                try
                {
                    foreach (var id in request.Ids)
                    {
                        try
                        {
                            var success = await _repository.RestoreAsync(id, _currentUserService.GetUserId());
                            if (success)
                            {
                                result.SuccessCount++;
                                result.SuccessIds.Add(id.ToString());
                            }
                            else
                            {
                                result.FailureCount++;
                                result.Errors.Add(new BatchOperationErrorDTO
                                {
                                    Id = id.ToString(),
                                    ErrorMessage = "Product not found or not deleted",
                                    ErrorCode = "NOT_FOUND"
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            result.FailureCount++;
                            result.Errors.Add(new BatchOperationErrorDTO
                            {
                                Id = id.ToString(),
                                ErrorMessage = ex.Message,
                                ErrorCode = "RESTORE_ERROR"
                            });
                        }
                    }

                    await _unitOfWork.SaveChangesAsync();

                    result.Message = result.IsCompleteSuccess
                        ? "All products restored successfully"
                        : result.IsPartialSuccess
                            ? $"Partially completed: {result.SuccessCount} restored, {result.FailureCount} failed"
                            : "All operations failed";

                    return ApiResult<BatchOperationResultDTO>.Success(result);
                }
                catch (Exception ex)
                {
                    return ApiResult<BatchOperationResultDTO>.Failure($"Bulk restore failed: {ex.Message}", ex);
                }
            });
        }

        public async Task<ApiResult<List<ProductDto>>> GetBestSellersAsync(int count = 10)
        {
            try
            {
                var products = await _productRepository.GetBestSellersAsync(count);
                var productDtos = products.Select(MapToDto).ToList();
                return ApiResult<List<ProductDto>>.Success(productDtos);
            }
            catch (Exception ex)
            {
                return ApiResult<List<ProductDto>>.Failure($"Error retrieving bestsellers: {ex.Message}", ex);
            }
        }

        public async Task<ApiResult<List<ProductDto>>> GetFeaturedAsync(int count = 10)
        {
            try
            {
                var products = await _productRepository.GetFeaturedAsync(count);
                var productDtos = products.Select(MapToDto).ToList();
                return ApiResult<List<ProductDto>>.Success(productDtos);
            }
            catch (Exception ex)
            {
                return ApiResult<List<ProductDto>>.Failure($"Error retrieving featured products: {ex.Message}", ex);
            }
        }

        //public async Task<ApiResult<bool>> UpdateViewCountAsync(Guid id)
        //{
        //    try
        //    {
        //        await _productRepository.UpdateViewCountAsync(id);
        //        await _unitOfWork.SaveChangesAsync();
        //        return ApiResult<bool>.Success(true, "View count updated successfully");
        //    }
        //    catch (Exception ex)
        //    {
        //        return ApiResult<bool>.Failure($"Error updating view count: {ex.Message}", ex);
        //    }
        //}

        // ----------- ONLY map real entity fields below -----------

        private static ProductDto MapToDto(Product product)
        {
            return new ProductDto
            {
                Id = product.Id,
                Name = product.Name,
                Description = product.Description,
                Price = product.Price,
                SalePrice = product.SalePrice,
                Sku = product.Sku,
                Quantity = product.Variants.Sum(v => v.Quantity),
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.Name,
                Material = product.Material,
                Season = product.Season,
                MetaTitle = product.MetaTitle,
                MetaDescription = product.MetaDescription,
                Slug = product.Slug,
                Status = product.Status,
                Images       = product.Images?
                           .OrderByDescending(i => i.IsPrimary)
                           .Select(i => i.Url)
                           .ToList(),
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt,
                CreatedBy = product.CreatedBy,
                UpdatedBy = product.UpdatedBy
                // You can add Images and Variants mapping if your DTO supports it
            };
        }

        private static Product MapToEntity(CreateProductDto dto)
        {
            return new Product
            {
                Id = Guid.NewGuid(),
                Name = dto.Name,
                Description = dto.Description,
                Price = dto.Price,
                SalePrice = dto.SalePrice,
                Sku = dto.Sku,
                CategoryId = dto.CategoryId,
                Material = dto.Material,
                Season = dto.Season,
                MetaTitle = dto.MetaTitle,
                MetaDescription = dto.MetaDescription,
                Slug = dto.Slug,
                Status = dto.Status
            };
        }
    }
}