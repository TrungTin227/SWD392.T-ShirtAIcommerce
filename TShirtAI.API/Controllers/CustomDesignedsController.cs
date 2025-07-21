using BusinessObjects.Common;
using BusinessObjects.CustomDesigns;
using DTOs.CustomDesigns;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Repositories.Helpers;
using Repositories.Interfaces;
using Services.Interfaces;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class CustomDesignController : ControllerBase
    {
        private readonly ICustomDesignedService _service;
        private readonly ICurrentUserService _currentUserService;
        private readonly ILogger<CustomDesignController> _logger;
        private readonly IAiImageService _aiImageService;

        public CustomDesignController(
            ICustomDesignedService service,
            ICurrentUserService currentUserService,
            ILogger<CustomDesignController> logger,
            IAiImageService aiImageService)
        {
            _service = service;
            _currentUserService = currentUserService;
            _logger = logger;
            _aiImageService = aiImageService;
        }

        // Helper: convert entity to response DTO
        private static CustomDesignResponse ToResponse(CustomDesign entity) => new()
        {
            Id = entity.Id,
            DesignName = entity.DesignName,
            PromptText = entity.PromptText ?? "",
            GarmentType = (int)entity.ShirtType,
            BaseColor = (int)entity.BaseColor,
            Size = (int)entity.Size,
            SpecialRequirements = entity.SpecialRequirements,
            Quantity = entity.Quantity,
            TotalPrice = entity.TotalPrice,
            DesignImageUrl = entity.DesignImageUrl,
            Status = (int)entity.Status,
            CreatedAt = entity.CreatedAt
        };
        // GET: api/CustomDesign/filter
        [HttpGet("filter")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<ActionResult> GetCustomDesigns([FromQuery] CustomDesignFilterRequest filter)
        {
            if (!User.IsInRole("Admin") && !filter.UserId.HasValue)
                filter.UserId = _currentUserService.GetUserId();

            // Lấy danh sách dạng PagedList từ service
            var pagedList = await _service.GetCustomDesignsAsync(filter);

            // Map sang DTO
            var result = pagedList.Select(ToResponse).ToList();

            // Trả về metadata chuẩn
            return Ok(new
            {
                metaData = pagedList.MetaData,
                items = result
            });
        }
        // GET: api/CustomDesign/filter-user
        [HttpGet("filter-user")]
        public async Task<ActionResult> GetCustomDesignsByIDAsync([FromQuery] CustomDesignFilterRequest filter)
        {
            // Luôn ép lấy user hiện tại (bất kể client truyền userId nào)
            var userId = _currentUserService.GetUserId();
            if (!userId.HasValue)
                return Unauthorized("Bạn cần đăng nhập!");

            filter.UserId = userId; // ép cứng UserId

            var pagedList = await _service.GetCustomDesignsByIDAsync(filter);

            var result = pagedList.Select(ToResponse).ToList();

            return Ok(new
            {
                metaData = pagedList.MetaData,
                items = result
            });
        }


        [HttpPatch("{id}/status")]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateCustomDesignStatusRequest req)
        {
            // Validate quyền sở hữu hoặc admin
            var entity = await _service.GetByIdAsync(id);
            if (entity == null) return NotFound();
            if (entity.UserId != _currentUserService.GetUserId() && !User.IsInRole("Admin"))
                return Forbid();

            // Check giá trị status hợp lệ (nếu muốn chỉ cho phép 1 số trạng thái)
            // if (!Enum.IsDefined(typeof(CustomDesignStatus), req.Status)) return BadRequest("Invalid status.");

            var success = await _service.UpdateStatusAsync(id, req.Status);
            if (!success)
                return StatusCode(500, new { message = "Không thể cập nhật trạng thái." });

            return NoContent();
        }

        // POST: api/CustomDesign
        [HttpPost]
        public async Task<ActionResult<CustomDesignResponse>> Create([FromBody] CreateCustomDesignRequest req)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userId = _currentUserService.GetUserId();
            if (!userId.HasValue)
                return Unauthorized("Bạn cần đăng nhập!");

            string? aiImageUrl = null;
            if (!string.IsNullOrWhiteSpace(req.PromptText))
            {
                try
                {
                    aiImageUrl = await _aiImageService.GenerateDesignImageAsync(
   (int)req.ShirtType,
   (int)req.BaseColor,
   (int)req.Size,
   req.SpecialRequirements,
   req.PromptText
);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Không thể sinh ảnh AI từ prompt, sẽ tiếp tục tạo mẫu không có ảnh.");
                }
            }

            try
            {
                var entity = new CustomDesign
                {
                    UserId = userId.Value,
                    DesignName = req.DesignName?.Trim() ?? string.Empty,
                    PromptText = req.PromptText?.Trim(),
                    ShirtType = req.ShirtType,
                    BaseColor = req.BaseColor,
                    Size = req.Size,
                    SpecialRequirements = req.SpecialRequirements?.Trim(),
                    Quantity = req.Quantity > 0 ? req.Quantity : 1,
                    TotalPrice = 300000,
                    Status = CustomDesignStatus.Draft,
                    DesignImageUrl = aiImageUrl
                };

                var created = await _service.CreateAsync(entity);
                return CreatedAtAction(nameof(GetById), new { id = created.Id }, ToResponse(created));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi khi tạo CustomDesign");
                return StatusCode(500, new { message = "Có lỗi xảy ra khi tạo mẫu thiết kế." });
            }
        }


        // GET: api/CustomDesign/{id}
        [HttpGet("{id}")]
        public async Task<ActionResult<CustomDesignResponse>> GetById(Guid id)
        {
            var entity = await _service.GetByIdAsync(id);
            if (entity == null) return NotFound();
            if (entity.UserId != _currentUserService.GetUserId() && !User.IsInRole("Admin"))
                return Forbid();

            return Ok(ToResponse(entity));
        }

        // GET: api/CustomDesign/my-designs
        [HttpGet("my-designs")]
        public async Task<ActionResult<IEnumerable<CustomDesignResponse>>> GetMyDesigns()
        {
            var userId = _currentUserService.GetUserId();
            if (!userId.HasValue)
                return Unauthorized("Bạn cần đăng nhập!");

            var list = await _service.GetByUserIdAsync(userId.Value);
            return Ok(list.OrderByDescending(x => x.CreatedAt).Select(ToResponse));
        }

        // GET: api/CustomDesign/latest
        [HttpGet("latest")]
        public async Task<ActionResult<CustomDesignResponse>> GetLatest()
        {
            var userId = _currentUserService.GetUserId();
            if (!userId.HasValue)
                return Unauthorized("Bạn cần đăng nhập!");

            var list = await _service.GetByUserIdAsync(userId.Value);
            var latest = list.OrderByDescending(x => x.CreatedAt).FirstOrDefault();
            if (latest == null)
                return NotFound();

            return Ok(ToResponse(latest));
        }

        // PATCH: api/CustomDesign/{id}/image
        [HttpPatch("{id}/image")]
        public async Task<IActionResult> UpdateImageUrl(Guid id, [FromBody] UpdateDesignImageUrlRequest req)
        {
            var entity = await _service.GetByIdAsync(id);
            if (entity == null) return NotFound();
            if (entity.UserId != _currentUserService.GetUserId() && !User.IsInRole("Admin"))
                return Forbid();

            entity.DesignImageUrl = req.DesignImageUrl;
            await _service.UpdateAsync(entity);
            return NoContent();
        }

        // PATCH: api/CustomDesign/{id}/hide
        [HttpPatch("{id}/hide")]
        public async Task<IActionResult> Hide(Guid id)
        {
            await _service.HideAsync(id);
            return NoContent();
        }

        // PATCH: api/CustomDesign/{id}/show
        [HttpPatch("{id}/show")]
        public async Task<IActionResult> Show(Guid id)
        {
            await _service.ShowAsync(id);
            return NoContent();
        }

        // DELETE: api/CustomDesign/{id}
        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var entity = await _service.GetByIdAsync(id);
            if (entity == null) return NotFound();
            if (entity.UserId != _currentUserService.GetUserId() && !User.IsInRole("Admin"))
                return Forbid();

            await _service.DeleteAsync(id);
            return NoContent();
        }
    }
}
