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
                if (!dto.SalePrice.HasValue || dto.SalePrice.Value <= 0)
                {
                    dto.SalePrice = null; 
                }
                // 1. Validate unique constraints
                if (!string.IsNullOrWhiteSpace(dto.Sku) && await _productRepository.IsSkuExistsAsync(dto.Sku))
                    return ApiResult<ProductDto>.Failure("SKU already exists");

                if (!string.IsNullOrWhiteSpace(dto.Slug) && await _productRepository.IsSlugExistsAsync(dto.Slug))
                    return ApiResult<ProductDto>.Failure("Slug already exists");

                // 2. Validate business rules
                if (dto.SalePrice.HasValue && dto.SalePrice >= dto.Price)
                    return ApiResult<ProductDto>.Failure("Sale price must be less than regular price");

                // 3. Map từ DTO sang Entity
                var product = MapToEntity(dto);

                // 4. Map collection Images
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

                // 5. Thêm entity vào DbContext để theo dõi
                await CreateAsync(product); // Gọi hàm base, chỉ add vào context, không save

                // 6. LƯU TẤT CẢ THAY ĐỔI XUỐNG DATABASE
                await _unitOfWork.SaveChangesAsync();

                // 7. Trả về DTO đã được tạo thành công
                return ApiResult<ProductDto>.Success(MapToDto(product), "Product created successfully");
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

                // 2. Validate (giữ nguyên logic)
                if (!string.IsNullOrWhiteSpace(dto.Sku) && dto.Sku != product.Sku &&
                    await _productRepository.IsSkuExistsAsync(dto.Sku, id))
                    return ApiResult<ProductDto>.Failure("SKU already exists");
                if (dto.SalePrice.HasValue && dto.SalePrice.Value <= 0)
                {
                    dto.SalePrice = null; 
                }
                if (!string.IsNullOrWhiteSpace(dto.Slug) && dto.Slug != product.Slug &&
                    await _productRepository.IsSlugExistsAsync(dto.Slug, id))
                    return ApiResult<ProductDto>.Failure("Slug already exists");

                var newPrice = dto.Price ?? product.Price;
                var newSalePrice = dto.SalePrice;
                if (newSalePrice.HasValue && newSalePrice.Value >= newPrice)
                    return ApiResult<ProductDto>.Failure("Sale price must be less than regular price");

                // 3. Map các trường cơ bản từ DTO lên entity (EF Core sẽ tự động track những thay đổi này)
                product.Name = dto.Name ?? product.Name;
                product.Description = dto.Description ?? product.Description;
                product.Price = dto.Price ?? product.Price;
                product.SalePrice = dto.SalePrice; // Cho phép set null
                product.Sku = dto.Sku ?? product.Sku;
                product.CategoryId = dto.CategoryId ?? product.CategoryId;
                product.Material = dto.Material ?? product.Material;
                product.Season = dto.Season ?? product.Season;
                product.MetaTitle = dto.MetaTitle ?? product.MetaTitle;
                product.MetaDescription = dto.MetaDescription ?? product.MetaDescription;
                product.Slug = dto.Slug ?? product.Slug;
                product.Status = dto.Status ?? product.Status;


                // 4. Xử lý danh sách ảnh nếu có (cập nhật thay vì xóa hết tạo lại)
                if (dto.Images != null)
                {
                    // Xóa ảnh cũ
                    _unitOfWork.Context.ProductImages.RemoveRange(product.Images);
                    product.Images.Clear();

                    // Thêm ảnh mới
                    foreach (var imgDto in dto.Images)
                    {
                        product.Images.Add(new ProductImage
                        {
                            Id = Guid.NewGuid(),
                            ProductId = id,
                            Url = imgDto.Url,
                            IsPrimary = imgDto.IsPrimary
                        });
                    }
                }

                // 5. Cập nhật audit fields
                product.UpdatedAt = _currentTime.GetVietnamTime();
                product.UpdatedBy = _currentUserService.GetUserId() ?? Guid.Empty;

                // Không cần gọi base.UpdateAsync(product) vì EF Core đã track entity `product`
                // và biết nó đã bị thay đổi (Modified)

                // 6. LƯU TẤT CẢ THAY ĐỔI XUỐNG DATABASE
                await _unitOfWork.SaveChangesAsync();

                // 7. Load lại dữ liệu liên quan (nếu cần) và trả về kết quả mới nhất
                // (Trong trường hợp này, product đã được cập nhật, ta có thể map lại trực tiếp)
                // Nếu muốn chắc chắn 100% dữ liệu là mới nhất từ DB (bao gồm cả trigger), có thể get lại
                var updatedProduct = await _productRepository.GetByIdAsync(id, p => p.Category, p => p.Images, p => p.Variants);

                return ApiResult<ProductDto>.Success(MapToDto(updatedProduct), "Product updated successfully");
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
                // Tính toán Quantity an toàn
                Quantity = product.Variants?.Sum(v => v.Quantity) ?? 0,
                CategoryId = product.CategoryId,
                // Lấy CategoryName an toàn
                CategoryName = product.Category?.Name,
                Material = product.Material,
                Season = product.Season,
                MetaTitle = product.MetaTitle,
                MetaDescription = product.MetaDescription,
                Slug = product.Slug,
                Status = product.Status,
                // Lấy Images an toàn
                Images = product.Images?
                           .OrderByDescending(i => i.IsPrimary)
                           .Select(i => i.Url)
                           .ToList() ?? new List<string>(),
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