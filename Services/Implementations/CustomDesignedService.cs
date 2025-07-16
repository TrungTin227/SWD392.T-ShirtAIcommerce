using BusinessObjects.Common;
using BusinessObjects.CustomDesigns;
using Data.Repositories.CustomDesigns;
using DTOs.CustomDesigns;
using Repositories.Helpers;
using Repositories.Interfaces;
using Services.Interfaces;
using Services.Interfaces.Services.Commons.User;

namespace Services.Implementations
{
    public class CustomDesignedService : ICustomDesignedService
    {
        private readonly IAICustomDesignRepository _repository;
        private readonly IAiImageService _aiImageService;
        private readonly IUserEmailService _userEmailService;

        public CustomDesignedService(
            IAICustomDesignRepository repository,
            IAiImageService aiImageService,
            IUserEmailService userEmailService)
        {
            _repository = repository;
            _aiImageService = aiImageService;
            _userEmailService = userEmailService;
        }

        public async Task<CustomDesign> CreateAsync(CustomDesign entity)
        {
            if (string.IsNullOrEmpty(entity.DesignImageUrl) && !string.IsNullOrWhiteSpace(entity.PromptText))
            {
                try
                {
                    entity.DesignImageUrl = await _aiImageService.GenerateDesignImageAsync(
                        entity.PromptText ?? entity.DesignName
                    );
                }
                catch (Exception ex)
                {
                    // Ghi log, không throw ra ngoài
                    Console.WriteLine($"[AI Image Service] Sinh ảnh thất bại: {ex.Message}");
                    // Hoặc nếu có logger, hãy log warning:
                    // _logger.LogWarning(ex, "AI Image Service failed for prompt: {Prompt}", entity.PromptText);
                    // Cho phép DesignImageUrl = null
                }
            }
            return await _repository.CreateAsync(entity);
        }


        public async Task<CustomDesign?> GetByIdAsync(Guid id)
            => await _repository.GetByIdAsync(id);


        public async Task<PagedList<CustomDesign>> GetCustomDesignsByIDAsync(CustomDesignFilterRequest filter)
            => await _repository.GetCustomDesignsByIDAsync(filter);

        public async Task<IEnumerable<CustomDesign>> GetByUserIdAsync(Guid userId)
            => await _repository.GetByUserIdAsync(userId);

        public async Task UpdateAsync(CustomDesign entity)
            => await _repository.UpdateAsync(entity);

        public async Task DeleteAsync(Guid id)
            => await _repository.DeleteAsync(id);

        public async Task ShowAsync(Guid id)
            => await _repository.ShowAsync(id);

        public async Task HideAsync(Guid id)
            => await _repository.HideAsync(id);
        public async Task<PagedList<CustomDesign>> GetCustomDesignsAsync(CustomDesignFilterRequest filter)
        {
            return await _repository.GetCustomDesignsAsync(filter);
        }

        public async Task<bool> UpdateStatusAsync(Guid id, CustomDesignStatus status)
        {
            // 1. Lấy entity
            var entity = await _repository.GetByIdAsync(id);
            if (entity == null) return false;

            // 2. Lưu thời gian thực vào DB cho từng trạng thái nếu chưa có (tránh overwrite)
            var now = DateTime.Now;
            switch (status)
            {
                case CustomDesignStatus.Order:
                    if (!entity.OrderCreatedAt.HasValue)
                        entity.OrderCreatedAt = now;
                    break;
                case CustomDesignStatus.Shipping:
                    if (!entity.ShippingStartedAt.HasValue)
                        entity.ShippingStartedAt = now;
                    break;
                case CustomDesignStatus.Delivered:
                    if (!entity.DeliveredAt.HasValue)
                        entity.DeliveredAt = now;
                    break;
                case CustomDesignStatus.Done:
                    if (!entity.DoneAt.HasValue)
                        entity.DoneAt = now;
                    break;
            }
            entity.Status = status;
            entity.UpdatedAt = now;

            // 3. Cập nhật entity vào DB
            await _repository.UpdateAsync(entity);

            // 4. Gửi email nếu đúng trạng thái
            if (entity.User != null && !string.IsNullOrEmpty(entity.User.Email))
            {
                switch (status)
                {
                    case CustomDesignStatus.Accepted:
                        await _userEmailService.SendCustomDesignStatusEmailAsync(
                            entity.User.Email, entity.DesignName, status
                        );
                        break;
                    case CustomDesignStatus.Order:
                        await _userEmailService.SendCustomDesignStatusEmailAsync(
                            entity.User.Email, entity.DesignName, status, orderCreatedAt: entity.OrderCreatedAt
                        );
                        break;
                    case CustomDesignStatus.Shipping:
                        await _userEmailService.SendCustomDesignStatusEmailAsync(
                            entity.User.Email, entity.DesignName, status, shippingStartAt: entity.ShippingStartedAt
                        );
                        break;
                    case CustomDesignStatus.Delivered:
                        await _userEmailService.SendCustomDesignStatusEmailAsync(
                            entity.User.Email, entity.DesignName, status, deliveredAt: entity.DeliveredAt
                        );
                        break;
                    case CustomDesignStatus.Done:
                        await _userEmailService.SendCustomDesignStatusEmailAsync(
                            entity.User.Email, entity.DesignName, status, doneAt: entity.DoneAt
                        );
                        break;
                    case CustomDesignStatus.Rejected:
                        await _userEmailService.SendCustomDesignStatusEmailAsync(
                            entity.User.Email, entity.DesignName, status
                        );
                        break;
                }
            }

            return true;
        }



    }
}
