using DTOs.CustomDesigns;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomDesignsController : ControllerBase
    {
        private readonly ICustomDesignService _customDesignService;

        public CustomDesignsController(ICustomDesignService customDesignService)
        {
            _customDesignService = customDesignService;
        }

        [HttpGet]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetCustomDesigns([FromQuery] CustomDesignFilterDto filter)
        {
            var result = await _customDesignService.GetCustomDesignsAsync(filter);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("{id:guid}")]
        [Authorize]
        public async Task<IActionResult> GetCustomDesignById(Guid id)
        {
            var result = await _customDesignService.GetByIdAsync(id);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("user/{userId:guid}")]
        [Authorize]
        public async Task<IActionResult> GetUserDesigns(Guid userId)
        {
            var result = await _customDesignService.GetUserDesignsAsync(userId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("staff/{staffId:guid}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetStaffDesigns(Guid staffId)
        {
            var result = await _customDesignService.GetStaffDesignsAsync(staffId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("pending")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetPendingDesigns()
        {
            var result = await _customDesignService.GetPendingDesignsAsync();
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> CreateCustomDesign([FromBody] CreateCustomDesignDto createDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _customDesignService.CreateCustomDesignAsync(createDto);
            return result.IsSuccess ? CreatedAtAction(nameof(GetCustomDesignById), new { id = result.Data?.Id }, result) : BadRequest(result);
        }

        [HttpPut("{id:guid}")]
        [Authorize]
        public async Task<IActionResult> UpdateCustomDesign(Guid id, [FromBody] UpdateCustomDesignDto updateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _customDesignService.UpdateCustomDesignAsync(id, updateDto);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{id:guid}/admin")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> AdminUpdateCustomDesign(Guid id, [FromBody] AdminUpdateCustomDesignDto updateDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _customDesignService.AdminUpdateCustomDesignAsync(id, updateDto);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpDelete("{id:guid}")]
        [Authorize]
        public async Task<IActionResult> DeleteCustomDesign(Guid id)
        {
            var result = await _customDesignService.DeleteCustomDesignAsync(id);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("{id:guid}/submit")]
        [Authorize]
        public async Task<IActionResult> SubmitDesign(Guid id)
        {
            var result = await _customDesignService.SubmitDesignAsync(id);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("{id:guid}/approve")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> ApproveDesign(Guid id)
        {
            var result = await _customDesignService.ApproveDesignAsync(id);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("{id:guid}/reject")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> RejectDesign(Guid id, [FromBody] string reason)
        {
            var result = await _customDesignService.RejectDesignAsync(id, reason);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("{id:guid}/start-production")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> StartProduction(Guid id)
        {
            var result = await _customDesignService.StartProductionAsync(id);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("{id:guid}/complete")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> CompleteDesign(Guid id)
        {
            var result = await _customDesignService.CompleteDesignAsync(id);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("stats")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetDesignStats()
        {
            var result = await _customDesignService.GetDesignStatsAsync();
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("stats/user/{userId:guid}")]
        [Authorize]
        public async Task<IActionResult> GetUserDesignStats(Guid userId)
        {
            var result = await _customDesignService.GetUserDesignStatsAsync(userId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("stats/staff/{staffId:guid}")]
        [Authorize(Roles = "Admin,Staff")]
        public async Task<IActionResult> GetStaffDesignStats(Guid staffId)
        {
            var result = await _customDesignService.GetStaffDesignStatsAsync(staffId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("calculate-pricing")]
        public async Task<IActionResult> CalculateDesignPricing([FromBody] CreateCustomDesignDto designDto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var result = await _customDesignService.CalculateDesignPricingAsync(designDto);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("{designId:guid}/assign-staff/{staffId:guid}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> AssignStaffToDesign(Guid designId, Guid staffId)
        {
            var result = await _customDesignService.AssignStaffToDesignAsync(designId, staffId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
    }
}