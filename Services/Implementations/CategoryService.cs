using BusinessObjects.Products;
using DTOs.Category;
using DTOs.Common;
using Repositories.Commons;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Interfaces;
using Services.Commons;
using Services.Extensions;
using Services.Interfaces;

namespace Services.Implements
{
    public class CategoryService : BaseService<Category, Guid>, ICategoryService
    {
        private readonly ICategoryRepository _categoryRepository;

        public CategoryService(
            ICategoryRepository categoryRepository,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            ICurrentTime currentTime)
            : base(categoryRepository, currentUserService, unitOfWork, currentTime)
        {
            _categoryRepository = categoryRepository;
        }

        public async Task<ApiResult<PagedResponse<CategoryDto>>> GetPagedAsync(CategoryFilterDto filter)
        {
            try
            {
                var pagedCategories = await _categoryRepository.GetPagedAsync(filter);
                var categoryDtos = pagedCategories.Select(MapToDto).ToList();

                var response = new PagedResponse<CategoryDto>
                {
                    Data = categoryDtos,
                    CurrentPage = pagedCategories.MetaData.CurrentPage,
                    TotalPages = pagedCategories.MetaData.TotalPages,
                    PageSize = pagedCategories.MetaData.PageSize,
                    TotalCount = pagedCategories.MetaData.TotalCount,
                    HasNextPage = pagedCategories.MetaData.CurrentPage < pagedCategories.MetaData.TotalPages,
                    HasPreviousPage = pagedCategories.MetaData.CurrentPage > 1,
                    IsSuccess = true,
                    Message = "Categories retrieved successfully"
                };

                return ApiResult<PagedResponse<CategoryDto>>.Success(response);
            }
            catch (Exception ex)
            {
                return ApiResult<PagedResponse<CategoryDto>>.Failure($"Error retrieving categories: {ex.Message}", ex);
            }
        }

        public async Task<ApiResult<CategoryDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var category = await _categoryRepository.GetByIdAsync(id);
                if (category == null || category.IsDeleted)
                {
                    return ApiResult<CategoryDto>.Failure("Category not found");
                }

                return ApiResult<CategoryDto>.Success(MapToDto(category));
            }
            catch (Exception ex)
            {
                return ApiResult<CategoryDto>.Failure($"Error retrieving category: {ex.Message}", ex);
            }
        }

        public async Task<ApiResult<CategoryDto>> CreateAsync(CreateCategoryDto dto)
        {
            return await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                try
                {
                    // Check if name already exists
                    if (await _categoryRepository.IsNameExistsAsync(dto.Name))
                    {
                        return ApiResult<CategoryDto>.Failure("Category name already exists");
                    }

                    var category = new Category
                    {
                        Id = Guid.NewGuid(),
                        Name = dto.Name,
                        Description = dto.Description,
                        IsActive = dto.IsActive
                    };

                    var result = await CreateAsync(category);
                    return ApiResult<CategoryDto>.Success(MapToDto(result));
                }
                catch (Exception ex)
                {
                    return ApiResult<CategoryDto>.Failure($"Error creating category: {ex.Message}", ex);
                }
            });
        }

        public async Task<ApiResult<CategoryDto>> UpdateAsync(Guid id, UpdateCategoryDto dto)
        {
            return await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                try
                {
                    var category = await _categoryRepository.GetByIdAsync(id);
                    if (category == null || category.IsDeleted)
                    {
                        return ApiResult<CategoryDto>.Failure("Category not found");
                    }

                    // Check name uniqueness if name is being updated
                    if (!string.IsNullOrWhiteSpace(dto.Name) && dto.Name != category.Name)
                    {
                        if (await _categoryRepository.IsNameExistsAsync(dto.Name, id))
                        {
                            return ApiResult<CategoryDto>.Failure("Category name already exists");
                        }
                        category.Name = dto.Name;
                    }

                    if (!string.IsNullOrWhiteSpace(dto.Description))
                    {
                        category.Description = dto.Description;
                    }

                    if (dto.IsActive.HasValue)
                    {
                        category.IsActive = dto.IsActive.Value;
                    }

                    var result = await UpdateAsync(category);
                    return ApiResult<CategoryDto>.Success(MapToDto(result));
                }
                catch (Exception ex)
                {
                    return ApiResult<CategoryDto>.Failure($"Error updating category: {ex.Message}", ex);
                }
            });
        }

        public async Task<ApiResult<bool>> DeleteAsync(Guid id)
        {
            return await _unitOfWork.ExecuteTransactionAsync(async () =>
            {
                try
                {
                    var success = await base.DeleteAsync(id);
                    if (!success)
                    {
                        return ApiResult<bool>.Failure("Category not found or already deleted");
                    }

                    return ApiResult<bool>.Success(true, "Category deleted successfully");
                }
                catch (Exception ex)
                {
                    return ApiResult<bool>.Failure($"Error deleting category: {ex.Message}", ex);
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
                                    ErrorMessage = "Category not found or already deleted",
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
                        ? "All categories deleted successfully"
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
                                    ErrorMessage = "Category not found or not deleted",
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
                        ? "All categories restored successfully"
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

        private static CategoryDto MapToDto(Category category)
        {
            return new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description,
                IsActive = category.IsActive,
                CreatedAt = category.CreatedAt,
                UpdatedAt = category.UpdatedAt,
                CreatedBy = category.CreatedBy,
                UpdatedBy = category.UpdatedBy
            };
        }
    }
}