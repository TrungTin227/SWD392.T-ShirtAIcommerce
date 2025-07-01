using BusinessObjects.CustomDesigns;
using BusinessObjects.Products;
using DTOs.CustomDesigns;
using Microsoft.EntityFrameworkCore;
using Repositories.Commons;
using Repositories.Helpers;
using Repositories.Interfaces;
using Repositories.WorkSeeds.Interfaces;
using Services.Commons;
using Services.Extensions;
using Services.Interfaces;

namespace Services.Implementations
{
    public class CustomDesignService : BaseService<CustomDesign, Guid>, ICustomDesignService
    {
        private readonly ICustomDesignRepository _customDesignRepository;

        public CustomDesignService(
            ICustomDesignRepository customDesignRepository,
            ICurrentUserService currentUserService,
            IUnitOfWork unitOfWork,
            ICurrentTime currentTime)
            : base(customDesignRepository, currentUserService, unitOfWork, currentTime)
        {
            _customDesignRepository = customDesignRepository;
        }

        public async Task<ApiResult<CustomDesignDto>> GetByIdAsync(Guid id)
        {
            try
            {
                var design = await _customDesignRepository.GetByIdAsync(id);
                if (design == null)
                {
                    return ApiResult<CustomDesignDto>.Failure("Custom design not found");
                }

                var designDto = await MapToCustomDesignDto(design);
                return ApiResult<CustomDesignDto>.Success(designDto);
            }
            catch (Exception ex)
            {
                return ApiResult<CustomDesignDto>.Failure($"Error retrieving custom design: {ex.Message}");
            }
        }

        public async Task<ApiResult<PagedList<CustomDesignDto>>> GetCustomDesignsAsync(CustomDesignFilterDto filter)
        {
            try
            {
                var query = _customDesignRepository.GetQueryable()
                    .Include(d => d.User)
                    .Include(d => d.Staff)
                    .AsQueryable();

                // Apply filters
                if (filter.UserId.HasValue)
                    query = query.Where(d => d.UserId == filter.UserId);

                if (filter.StaffId.HasValue)
                    query = query.Where(d => d.StaffId == filter.StaffId);

                if (filter.Status.HasValue)
                    query = query.Where(d => d.Status == filter.Status);

                if (filter.ShirtType.HasValue)
                    query = query.Where(d => d.ShirtType == filter.ShirtType);

                if (filter.BaseColor.HasValue)
                    query = query.Where(d => d.BaseColor == filter.BaseColor);

                if (filter.Size.HasValue)
                    query = query.Where(d => d.Size == filter.Size);

                if (filter.FromDate.HasValue)
                    query = query.Where(d => d.CreatedAt >= filter.FromDate);

                if (filter.ToDate.HasValue)
                    query = query.Where(d => d.CreatedAt <= filter.ToDate);

                if (filter.MinPrice.HasValue)
                    query = query.Where(d => d.TotalPrice >= filter.MinPrice);

                if (filter.MaxPrice.HasValue)
                    query = query.Where(d => d.TotalPrice <= filter.MaxPrice);

                if (!string.IsNullOrEmpty(filter.SearchTerm))
                    query = query.Where(d => d.DesignName.Contains(filter.SearchTerm) ||
                                           (d.LogoText != null && d.LogoText.Contains(filter.SearchTerm)));

                // Apply ordering
                query = filter.OrderBy.ToLower() switch
                {
                    "name" => filter.OrderByDescending ? query.OrderByDescending(d => d.DesignName) : query.OrderBy(d => d.DesignName),
                    "price" => filter.OrderByDescending ? query.OrderByDescending(d => d.TotalPrice) : query.OrderBy(d => d.TotalPrice),
                    "status" => filter.OrderByDescending ? query.OrderByDescending(d => d.Status) : query.OrderBy(d => d.Status),
                    _ => filter.OrderByDescending ? query.OrderByDescending(d => d.CreatedAt) : query.OrderBy(d => d.CreatedAt)
                };

                var totalCount = await query.CountAsync();
                var items = await query
                    .Skip((filter.Page - 1) * filter.PageSize)
                    .Take(filter.PageSize)
                    .ToListAsync();

                var designDtos = new List<CustomDesignDto>();
                foreach (var design in items)
                {
                    designDtos.Add(await MapToCustomDesignDto(design));
                }

                var pagedResult = new PagedList<CustomDesignDto>(designDtos, totalCount, filter.Page, filter.PageSize);
                return ApiResult<PagedList<CustomDesignDto>>.Success(pagedResult);
            }
            catch (Exception ex)
            {
                return ApiResult<PagedList<CustomDesignDto>>.Failure($"Error retrieving custom designs: {ex.Message}");
            }
        }

        public async Task<ApiResult<List<CustomDesignDto>>> GetUserDesignsAsync(Guid userId)
        {
            try
            {
                var designs = await _customDesignRepository.GetDesignsByUserIdAsync(userId);
                var designDtos = new List<CustomDesignDto>();
                
                foreach (var design in designs)
                {
                    designDtos.Add(await MapToCustomDesignDto(design));
                }

                return ApiResult<List<CustomDesignDto>>.Success(designDtos);
            }
            catch (Exception ex)
            {
                return ApiResult<List<CustomDesignDto>>.Failure($"Error retrieving user designs: {ex.Message}");
            }
        }

        public async Task<ApiResult<List<CustomDesignDto>>> GetStaffDesignsAsync(Guid staffId)
        {
            try
            {
                var designs = await _customDesignRepository.GetDesignsByStaffIdAsync(staffId);
                var designDtos = new List<CustomDesignDto>();
                
                foreach (var design in designs)
                {
                    designDtos.Add(await MapToCustomDesignDto(design));
                }

                return ApiResult<List<CustomDesignDto>>.Success(designDtos);
            }
            catch (Exception ex)
            {
                return ApiResult<List<CustomDesignDto>>.Failure($"Error retrieving staff designs: {ex.Message}");
            }
        }

        public async Task<ApiResult<List<CustomDesignDto>>> GetPendingDesignsAsync()
        {
            try
            {
                var designs = await _customDesignRepository.GetPendingDesignsAsync();
                var designDtos = new List<CustomDesignDto>();
                
                foreach (var design in designs)
                {
                    designDtos.Add(await MapToCustomDesignDto(design));
                }

                return ApiResult<List<CustomDesignDto>>.Success(designDtos);
            }
            catch (Exception ex)
            {
                return ApiResult<List<CustomDesignDto>>.Failure($"Error retrieving pending designs: {ex.Message}");
            }
        }

        public async Task<ApiResult<CustomDesignDto>> CreateCustomDesignAsync(CreateCustomDesignDto createDto)
        {
            try
            {
                var currentUserId = _currentUserService.GetUserId();
                if (!currentUserId.HasValue)
                {
                    return ApiResult<CustomDesignDto>.Failure("User not authenticated");
                }

                // Calculate price
                var totalPrice = await _customDesignRepository.CalculateDesignPriceAsync(createDto);

                var design = new CustomDesign
                {
                    UserId = currentUserId.Value,
                    DesignName = createDto.DesignName,
                    ShirtType = createDto.ShirtType,
                    BaseColor = createDto.BaseColor,
                    Size = createDto.Size,
                    DesignImageUrl = createDto.DesignImageUrl,
                    LogoText = createDto.LogoText,
                    LogoPosition = createDto.LogoPosition,
                    SpecialRequirements = createDto.SpecialRequirements,
                    TotalPrice = totalPrice,
                    Quantity = createDto.Quantity,
                    EstimatedDays = createDto.EstimatedDays,
                    Status = DesignStatus.Draft,
                    CreatedAt = _currentTime.GetVietnamTime(),
                    CreatedBy = currentUserId.Value
                };

                await _customDesignRepository.AddAsync(design);
                await _unitOfWork.SaveChangesAsync();

                var designDto = await MapToCustomDesignDto(design);
                return ApiResult<CustomDesignDto>.Success(designDto);
            }
            catch (Exception ex)
            {
                return ApiResult<CustomDesignDto>.Failure($"Error creating custom design: {ex.Message}");
            }
        }

        public async Task<ApiResult<CustomDesignDto>> UpdateCustomDesignAsync(Guid id, UpdateCustomDesignDto updateDto)
        {
            try
            {
                var currentUserId = _currentUserService.GetUserId();
                var design = await _customDesignRepository.GetByIdAsync(id);
                
                if (design == null)
                {
                    return ApiResult<CustomDesignDto>.Failure("Custom design not found");
                }

                if (!currentUserId.HasValue || design.UserId != currentUserId.Value)
                {
                    return ApiResult<CustomDesignDto>.Failure("You can only update your own designs");
                }

                if (design.Status != DesignStatus.Draft)
                {
                    return ApiResult<CustomDesignDto>.Failure("Only draft designs can be updated");
                }

                // Update properties
                design.DesignName = updateDto.DesignName;
                design.ShirtType = updateDto.ShirtType;
                design.BaseColor = updateDto.BaseColor;
                design.Size = updateDto.Size;
                design.DesignImageUrl = updateDto.DesignImageUrl;
                design.LogoText = updateDto.LogoText;
                design.LogoPosition = updateDto.LogoPosition;
                design.SpecialRequirements = updateDto.SpecialRequirements;
                design.Quantity = updateDto.Quantity;
                design.EstimatedDays = updateDto.EstimatedDays;
                design.UpdatedAt = _currentTime.GetVietnamTime();
                design.UpdatedBy = currentUserId.Value;

                // Recalculate price
                var createDto = new CreateCustomDesignDto
                {
                    DesignName = updateDto.DesignName,
                    ShirtType = updateDto.ShirtType,
                    BaseColor = updateDto.BaseColor,
                    Size = updateDto.Size,
                    DesignImageUrl = updateDto.DesignImageUrl,
                    LogoText = updateDto.LogoText,
                    LogoPosition = updateDto.LogoPosition,
                    SpecialRequirements = updateDto.SpecialRequirements,
                    Quantity = updateDto.Quantity,
                    EstimatedDays = updateDto.EstimatedDays
                };
                design.TotalPrice = await _customDesignRepository.CalculateDesignPriceAsync(createDto);

                await _customDesignRepository.UpdateAsync(design);
                await _unitOfWork.SaveChangesAsync();

                var designDto = await MapToCustomDesignDto(design);
                return ApiResult<CustomDesignDto>.Success(designDto);
            }
            catch (Exception ex)
            {
                return ApiResult<CustomDesignDto>.Failure($"Error updating custom design: {ex.Message}");
            }
        }

        public async Task<ApiResult<CustomDesignDto>> AdminUpdateCustomDesignAsync(Guid id, AdminUpdateCustomDesignDto updateDto)
        {
            try
            {
                var currentUserId = _currentUserService.GetUserId();
                var design = await _customDesignRepository.GetByIdAsync(id);
                
                if (design == null)
                {
                    return ApiResult<CustomDesignDto>.Failure("Custom design not found");
                }

                // Update status and staff notes
                design.Status = updateDto.Status;
                design.StaffNotes = updateDto.StaffNotes;
                design.UpdatedAt = _currentTime.GetVietnamTime();
                design.UpdatedBy = currentUserId ?? Guid.Empty;

                // Set timestamp based on status
                if (updateDto.Status == DesignStatus.Approved && design.ApprovedAt == null)
                {
                    design.ApprovedAt = _currentTime.GetVietnamTime();
                }
                else if (updateDto.Status == DesignStatus.Completed && design.CompletedAt == null)
                {
                    design.CompletedAt = _currentTime.GetVietnamTime();
                }

                // Update price if provided
                if (updateDto.TotalPrice.HasValue)
                {
                    design.TotalPrice = updateDto.TotalPrice.Value;
                }

                // Update estimated days if provided
                if (updateDto.EstimatedDays.HasValue)
                {
                    design.EstimatedDays = updateDto.EstimatedDays.Value;
                }

                await _customDesignRepository.UpdateAsync(design);
                await _unitOfWork.SaveChangesAsync();

                var designDto = await MapToCustomDesignDto(design);
                return ApiResult<CustomDesignDto>.Success(designDto);
            }
            catch (Exception ex)
            {
                return ApiResult<CustomDesignDto>.Failure($"Error updating custom design: {ex.Message}");
            }
        }

        public async Task<ApiResult<bool>> DeleteCustomDesignAsync(Guid id)
        {
            try
            {
                var currentUserId = _currentUserService.GetUserId();
                var design = await _customDesignRepository.GetByIdAsync(id);
                
                if (design == null)
                {
                    return ApiResult<bool>.Failure("Custom design not found");
                }

                if (!currentUserId.HasValue || design.UserId != currentUserId.Value)
                {
                    return ApiResult<bool>.Failure("You can only delete your own designs");
                }

                if (design.Status != DesignStatus.Draft)
                {
                    return ApiResult<bool>.Failure("Only draft designs can be deleted");
                }

                await _customDesignRepository.DeleteAsync(design.Id);
                await _unitOfWork.SaveChangesAsync();

                return ApiResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ApiResult<bool>.Failure($"Error deleting custom design: {ex.Message}");
            }
        }

        public async Task<ApiResult<CustomDesignDto>> SubmitDesignAsync(Guid id)
        {
            try
            {
                var currentUserId = _currentUserService.GetUserId();
                var design = await _customDesignRepository.GetByIdAsync(id);
                
                if (design == null)
                {
                    return ApiResult<CustomDesignDto>.Failure("Custom design not found");
                }

                if (!currentUserId.HasValue || design.UserId != currentUserId.Value)
                {
                    return ApiResult<CustomDesignDto>.Failure("You can only submit your own designs");
                }

                if (design.Status != DesignStatus.Draft)
                {
                    return ApiResult<CustomDesignDto>.Failure("Only draft designs can be submitted");
                }

                design.Status = DesignStatus.Submitted;
                design.UpdatedAt = _currentTime.GetVietnamTime();
                design.UpdatedBy = currentUserId.Value;

                await _customDesignRepository.UpdateAsync(design);
                await _unitOfWork.SaveChangesAsync();

                var designDto = await MapToCustomDesignDto(design);
                return ApiResult<CustomDesignDto>.Success(designDto);
            }
            catch (Exception ex)
            {
                return ApiResult<CustomDesignDto>.Failure($"Error submitting design: {ex.Message}");
            }
        }

        public async Task<ApiResult<CustomDesignDto>> ApproveDesignAsync(Guid id)
        {
            try
            {
                var currentUserId = _currentUserService.GetUserId();
                var design = await _customDesignRepository.GetByIdAsync(id);
                
                if (design == null)
                {
                    return ApiResult<CustomDesignDto>.Failure("Custom design not found");
                }

                if (design.Status != DesignStatus.Submitted && design.Status != DesignStatus.UnderReview)
                {
                    return ApiResult<CustomDesignDto>.Failure("Only submitted or under review designs can be approved");
                }

                design.Status = DesignStatus.Approved;
                design.ApprovedAt = _currentTime.GetVietnamTime();
                design.StaffId = currentUserId ?? Guid.Empty;
                design.UpdatedAt = _currentTime.GetVietnamTime();
                design.UpdatedBy = currentUserId ?? Guid.Empty;

                await _customDesignRepository.UpdateAsync(design);
                await _unitOfWork.SaveChangesAsync();

                var designDto = await MapToCustomDesignDto(design);
                return ApiResult<CustomDesignDto>.Success(designDto);
            }
            catch (Exception ex)
            {
                return ApiResult<CustomDesignDto>.Failure($"Error approving design: {ex.Message}");
            }
        }

        public async Task<ApiResult<CustomDesignDto>> RejectDesignAsync(Guid id, string reason)
        {
            try
            {
                var currentUserId = _currentUserService.GetUserId();
                var design = await _customDesignRepository.GetByIdAsync(id);
                
                if (design == null)
                {
                    return ApiResult<CustomDesignDto>.Failure("Custom design not found");
                }

                if (design.Status != DesignStatus.Submitted && design.Status != DesignStatus.UnderReview)
                {
                    return ApiResult<CustomDesignDto>.Failure("Only submitted or under review designs can be rejected");
                }

                design.Status = DesignStatus.Rejected;
                design.StaffNotes = reason;
                design.StaffId = currentUserId ?? Guid.Empty;
                design.UpdatedAt = _currentTime.GetVietnamTime();
                design.UpdatedBy = currentUserId ?? Guid.Empty;

                await _customDesignRepository.UpdateAsync(design);
                await _unitOfWork.SaveChangesAsync();

                var designDto = await MapToCustomDesignDto(design);
                return ApiResult<CustomDesignDto>.Success(designDto);
            }
            catch (Exception ex)
            {
                return ApiResult<CustomDesignDto>.Failure($"Error rejecting design: {ex.Message}");
            }
        }

        public async Task<ApiResult<CustomDesignDto>> StartProductionAsync(Guid id)
        {
            try
            {
                var currentUserId = _currentUserService.GetUserId();
                var design = await _customDesignRepository.GetByIdAsync(id);
                
                if (design == null)
                {
                    return ApiResult<CustomDesignDto>.Failure("Custom design not found");
                }

                if (design.Status != DesignStatus.Approved)
                {
                    return ApiResult<CustomDesignDto>.Failure("Only approved designs can start production");
                }

                design.Status = DesignStatus.InProduction;
                design.UpdatedAt = _currentTime.GetVietnamTime();
                design.UpdatedBy = currentUserId ?? Guid.Empty;

                await _customDesignRepository.UpdateAsync(design);
                await _unitOfWork.SaveChangesAsync();

                var designDto = await MapToCustomDesignDto(design);
                return ApiResult<CustomDesignDto>.Success(designDto);
            }
            catch (Exception ex)
            {
                return ApiResult<CustomDesignDto>.Failure($"Error starting production: {ex.Message}");
            }
        }

        public async Task<ApiResult<CustomDesignDto>> CompleteDesignAsync(Guid id)
        {
            try
            {
                var currentUserId = _currentUserService.GetUserId();
                var design = await _customDesignRepository.GetByIdAsync(id);
                
                if (design == null)
                {
                    return ApiResult<CustomDesignDto>.Failure("Custom design not found");
                }

                if (design.Status != DesignStatus.InProduction)
                {
                    return ApiResult<CustomDesignDto>.Failure("Only designs in production can be completed");
                }

                design.Status = DesignStatus.Completed;
                design.CompletedAt = _currentTime.GetVietnamTime();
                design.UpdatedAt = _currentTime.GetVietnamTime();
                design.UpdatedBy = currentUserId ?? Guid.Empty;

                await _customDesignRepository.UpdateAsync(design);
                await _unitOfWork.SaveChangesAsync();

                var designDto = await MapToCustomDesignDto(design);
                return ApiResult<CustomDesignDto>.Success(designDto);
            }
            catch (Exception ex)
            {
                return ApiResult<CustomDesignDto>.Failure($"Error completing design: {ex.Message}");
            }
        }

        public async Task<ApiResult<CustomDesignStatsDto>> GetDesignStatsAsync()
        {
            try
            {
                var stats = await _customDesignRepository.GetDesignStatsAsync();
                return ApiResult<CustomDesignStatsDto>.Success(stats);
            }
            catch (Exception ex)
            {
                return ApiResult<CustomDesignStatsDto>.Failure($"Error retrieving design stats: {ex.Message}");
            }
        }

        public async Task<ApiResult<CustomDesignStatsDto>> GetUserDesignStatsAsync(Guid userId)
        {
            try
            {
                var stats = await _customDesignRepository.GetUserDesignStatsAsync(userId);
                return ApiResult<CustomDesignStatsDto>.Success(stats);
            }
            catch (Exception ex)
            {
                return ApiResult<CustomDesignStatsDto>.Failure($"Error retrieving user design stats: {ex.Message}");
            }
        }

        public async Task<ApiResult<CustomDesignStatsDto>> GetStaffDesignStatsAsync(Guid staffId)
        {
            try
            {
                var stats = await _customDesignRepository.GetStaffDesignStatsAsync(staffId);
                return ApiResult<CustomDesignStatsDto>.Success(stats);
            }
            catch (Exception ex)
            {
                return ApiResult<CustomDesignStatsDto>.Failure($"Error retrieving staff design stats: {ex.Message}");
            }
        }

        public async Task<ApiResult<DesignPricingDto>> CalculateDesignPricingAsync(CreateCustomDesignDto designDto)
        {
            try
            {
                var totalPrice = await _customDesignRepository.CalculateDesignPriceAsync(designDto);
                
                // Calculate detailed pricing breakdown
                decimal basePrice = designDto.ShirtType switch
                {
                    GarmentType.TShirt => 150000m,
                    GarmentType.Hoodie => 250000m,
                    GarmentType.Sweatshirt => 200000m,
                    GarmentType.TankTop => 120000m,
                    GarmentType.LongSleeve => 180000m,
                    GarmentType.Jacket => 300000m,
                    _ => 150000m
                };

                decimal customizationFee = 50000m;
                decimal logoFee = !string.IsNullOrEmpty(designDto.LogoText) ? 30000m : 0m;

                var pricing = new DesignPricingDto
                {
                    ShirtType = designDto.ShirtType,
                    BaseColor = designDto.BaseColor,
                    Size = designDto.Size,
                    BasePrice = basePrice,
                    CustomizationFee = customizationFee,
                    TotalPrice = totalPrice,
                    HasLogo = !string.IsNullOrEmpty(designDto.LogoText),
                    LogoFee = logoFee
                };

                return ApiResult<DesignPricingDto>.Success(pricing);
            }
            catch (Exception ex)
            {
                return ApiResult<DesignPricingDto>.Failure($"Error calculating design pricing: {ex.Message}");
            }
        }

        public async Task<ApiResult<bool>> AssignStaffToDesignAsync(Guid designId, Guid staffId)
        {
            try
            {
                var result = await _customDesignRepository.AssignStaffToDesignAsync(designId, staffId);
                if (!result)
                {
                    return ApiResult<bool>.Failure("Design not found or assignment failed");
                }

                await _unitOfWork.SaveChangesAsync();
                return ApiResult<bool>.Success(true);
            }
            catch (Exception ex)
            {
                return ApiResult<bool>.Failure($"Error assigning staff to design: {ex.Message}");
            }
        }

        private async Task<CustomDesignDto> MapToCustomDesignDto(CustomDesign design)
        {
            return await Task.FromResult(new CustomDesignDto
            {
                Id = design.Id,
                UserId = design.UserId,
                UserName = design.User?.UserName ?? "Unknown User",
                DesignName = design.DesignName,
                ShirtType = design.ShirtType,
                BaseColor = design.BaseColor,
                Size = design.Size,
                DesignImageUrl = design.DesignImageUrl,
                LogoText = design.LogoText,
                LogoPosition = design.LogoPosition,
                SpecialRequirements = design.SpecialRequirements,
                TotalPrice = design.TotalPrice,
                Quantity = design.Quantity,
                EstimatedDays = design.EstimatedDays,
                Status = design.Status,
                StaffId = design.StaffId,
                StaffName = design.Staff?.UserName,
                StaffNotes = design.StaffNotes,
                ApprovedAt = design.ApprovedAt,
                CompletedAt = design.CompletedAt,
                CreatedAt = design.CreatedAt,
                UpdatedAt = design.UpdatedAt
            });
        }
    }
}