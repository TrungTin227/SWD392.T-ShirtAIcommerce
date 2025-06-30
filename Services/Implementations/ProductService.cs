using BusinessObjects.Products;
using DTOs.Common;
using DTOs.Product;
using Repositories.Commons;
using Repositories.Helpers;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Interfaces;
using Services.Commons;
using Services.Extensions;
using Services.Interfaces;

namespace Services.Implements
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
                var pagedProducts = await _productRepository.GetPagedAsync(filter);
                var productDtos = pagedProducts.Select(MapToDto).ToList();

                var response = new PagedResponse<ProductDto>
                {
                    Data = productDtos,
                    CurrentPage = pagedProducts.MetaData.CurrentPage,
                    TotalPages = pagedProducts.MetaData.TotalPages,
                    PageSize = pagedProducts.MetaData.PageSize,
                    TotalCount = pagedProducts.MetaData.TotalCount,
                    HasNextPage = pagedProducts.MetaData.CurrentPage < pagedProducts.MetaData.TotalPages,
                    HasPreviousPage = pagedProducts.MetaData.CurrentPage > 1,
                    IsSuccess = true,
                    Message = "Products retrieved successfully"
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
                var product = await _productRepository.GetByIdAsync(id, p => p.Category);
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
                    {
                        return ApiResult<ProductDto>.Failure("SKU already exists");
                    }

                    if (!string.IsNullOrWhiteSpace(dto.Slug) && await _productRepository.IsSlugExistsAsync(dto.Slug))
                    {
                        return ApiResult<ProductDto>.Failure("Slug already exists");
                    }

                    // Validate business rules
                    if (dto.SalePrice.HasValue && dto.SalePrice >= dto.Price)
                    {
                        return ApiResult<ProductDto>.Failure("Sale price must be less than regular price");
                    }

                    if (dto.MinOrderQuantity > dto.MaxOrderQuantity)
                    {
                        return ApiResult<ProductDto>.Failure("Minimum order quantity cannot exceed maximum order quantity");
                    }

                    var product = MapToEntity(dto);
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
                try
                {
                    var product = await _productRepository.GetByIdAsync(id);
                    if (product == null || product.IsDeleted)
                    {
                        return ApiResult<ProductDto>.Failure("Product not found");
                    }

                    // Validate unique constraints
                    if (!string.IsNullOrWhiteSpace(dto.Sku) && dto.Sku != product.Sku)
                    {
                        if (await _productRepository.IsSkuExistsAsync(dto.Sku, id))
                        {
                            return ApiResult<ProductDto>.Failure("SKU already exists");
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(dto.Slug) && dto.Slug != product.Slug)
                    {
                        if (await _productRepository.IsSlugExistsAsync(dto.Slug, id))
                        {
                            return ApiResult<ProductDto>.Failure("Slug already exists");
                        }
                    }

                    // Validate business rules
                    var salePrice = dto.SalePrice ?? product.SalePrice;
                    var price = dto.Price ?? product.Price;
                    if (salePrice.HasValue && salePrice >= price)
                    {
                        return ApiResult<ProductDto>.Failure("Sale price must be less than regular price");
                    }

                    var minOrder = dto.MinOrderQuantity ?? product.MinOrderQuantity;
                    var maxOrder = dto.MaxOrderQuantity ?? product.MaxOrderQuantity;
                    if (minOrder > maxOrder)
                    {
                        return ApiResult<ProductDto>.Failure("Minimum order quantity cannot exceed maximum order quantity");
                    }

                    // Update product
                    UpdateProductFromDto(product, dto);
                    var result = await UpdateAsync(product);
                    return ApiResult<ProductDto>.Success(MapToDto(result));
                }
                catch (Exception ex)
                {
                    return ApiResult<ProductDto>.Failure($"Error updating product: {ex.Message}", ex);
                }
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

        public async Task<ApiResult<bool>> UpdateViewCountAsync(Guid id)
        {
            try
            {
                await _productRepository.UpdateViewCountAsync(id);
                await _unitOfWork.SaveChangesAsync();
                return ApiResult<bool>.Success(true, "View count updated successfully");
            }
            catch (Exception ex)
            {
                return ApiResult<bool>.Failure($"Error updating view count: {ex.Message}", ex);
            }
        }

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
                Quantity = product.Quantity,
                CategoryId = product.CategoryId,
                CategoryName = product.Category?.Name,
                Material = product.Material.ToString(),
                Season = product.Season.ToString(),
                Weight = product.Weight,
                Dimensions = product.Dimensions,
                MetaTitle = product.MetaTitle,
                MetaDescription = product.MetaDescription,
                Slug = product.Slug,
                ViewCount = product.ViewCount,
                SoldCount = product.SoldCount,
                MinOrderQuantity = product.MinOrderQuantity,
                MaxOrderQuantity = product.MaxOrderQuantity,
                IsFeatured = product.IsFeatured,
                IsBestseller = product.IsBestseller,
                DiscountPercentage = product.DiscountPercentage,
                AvailableColors = product.AvailableColors
                        .Select(c => c.ToString())
                        .ToList(),
                AvailableSizes = product.AvailableSizes
                        .Select(s => s.ToString())
                        .ToList(),
                Images = product.Images,
                Status = product.Status,
                CreatedAt = product.CreatedAt,
                UpdatedAt = product.UpdatedAt,
                CreatedBy = product.CreatedBy,
                UpdatedBy = product.UpdatedBy
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
                Quantity = dto.Quantity,
                CategoryId = dto.CategoryId,
                Material = dto.Material.Value,
                Season = dto.Season.Value,
                Weight = dto.Weight,
                Dimensions = dto.Dimensions,
                MetaTitle = dto.MetaTitle,
                MetaDescription = dto.MetaDescription,
                Slug = dto.Slug,
                MinOrderQuantity = dto.MinOrderQuantity,
                MaxOrderQuantity = dto.MaxOrderQuantity,
                IsFeatured = dto.IsFeatured,
                IsBestseller = dto.IsBestseller,
                DiscountPercentage = dto.DiscountPercentage,
                AvailableColors = dto.AvailableColors?
    .Select(name => Enum.Parse<ProductColor>(name, ignoreCase: true))
    .ToList()
    ?? new List<ProductColor>(),

                AvailableSizes = dto.AvailableSizes?
    .Select(name => Enum.Parse<ProductSize>(name, ignoreCase: true))
    .ToList()
    ?? new List<ProductSize>(),
                Images = dto.Images,
                Status = dto.Status
            };
        }

        private static void UpdateProductFromDto(Product product, UpdateProductDto dto)
        {
            if (!string.IsNullOrWhiteSpace(dto.Name))
                product.Name = dto.Name;

            if (dto.Description != null)
                product.Description = dto.Description;

            if (dto.Price.HasValue)
                product.Price = dto.Price.Value;

            if (dto.SalePrice.HasValue)
                product.SalePrice = dto.SalePrice;

            if (!string.IsNullOrWhiteSpace(dto.Sku))
                product.Sku = dto.Sku;

            if (dto.Quantity.HasValue)
                product.Quantity = dto.Quantity.Value;

            if (dto.CategoryId.HasValue)
                product.CategoryId = dto.CategoryId;

            if (!string.IsNullOrWhiteSpace(dto.Material.ToString()))
                product.Material = Enum.Parse<ProductMaterial>(dto.Material.ToString(), true);

            if (!string.IsNullOrWhiteSpace(dto.Season.ToString()))
                product.Season = Enum.Parse<ProductSeason>(dto.Season.ToString(), true);

            if (dto.Weight.HasValue)
                product.Weight = dto.Weight.Value;

            if (!string.IsNullOrWhiteSpace(dto.Dimensions))
                product.Dimensions = dto.Dimensions;

            if (!string.IsNullOrWhiteSpace(dto.MetaTitle))
                product.MetaTitle = dto.MetaTitle;

            if (!string.IsNullOrWhiteSpace(dto.MetaDescription))
                product.MetaDescription = dto.MetaDescription;

            if (!string.IsNullOrWhiteSpace(dto.Slug))
                product.Slug = dto.Slug;

            if (dto.MinOrderQuantity.HasValue)
                product.MinOrderQuantity = dto.MinOrderQuantity.Value;

            if (dto.MaxOrderQuantity.HasValue)
                product.MaxOrderQuantity = dto.MaxOrderQuantity.Value;

            if (dto.IsFeatured.HasValue)
                product.IsFeatured = dto.IsFeatured.Value;

            if (dto.IsBestseller.HasValue)
                product.IsBestseller = dto.IsBestseller.Value;

            if (dto.DiscountPercentage.HasValue)
                product.DiscountPercentage = dto.DiscountPercentage.Value;

            if (dto.AvailableColors != null)
                product.AvailableColors = dto.AvailableColors
                                              .Select(name => Enum.Parse<ProductColor>(name, true))
                                              .ToList();

            if (dto.AvailableSizes != null)
                product.AvailableSizes = dto.AvailableSizes
                                             .Select(name => Enum.Parse<ProductSize>(name, true))
                                             .ToList();

            if (dto.Images != null)
                product.Images = dto.Images;

            if (dto.Status.HasValue)
                product.Status = dto.Status.Value;
        }
    }
}