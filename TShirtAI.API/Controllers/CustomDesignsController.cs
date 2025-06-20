using BusinessObjects.CustomDesigns;
using Microsoft.AspNetCore.Mvc;
using Repositories.WorkSeeds.Interfaces;
using WebAPI.Middlewares;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CustomDesignsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public CustomDesignsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var repo = _unitOfWork.GetRepository<CustomDesign, Guid>();
            var items = await repo.GetAllAsync();
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var repo = _unitOfWork.GetRepository<CustomDesign, Guid>();
            var item = await repo.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        [ServiceFilter(typeof(ValidateModelAttribute))]
        public async Task<IActionResult> Post([FromBody] CustomDesign design)
        {
            var repo = _unitOfWork.GetRepository<CustomDesign, Guid>();
            await repo.AddAsync(design);
            await _unitOfWork.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = design.Id }, design);
        }

        [HttpPut("{id}")]
        [ServiceFilter(typeof(ValidateModelAttribute))]
        public async Task<IActionResult> Put(Guid id, [FromBody] CustomDesign design)
        {
            if (id != design.Id) return BadRequest();
            var repo = _unitOfWork.GetRepository<CustomDesign, Guid>();
            await repo.UpdateAsync(design);
            await _unitOfWork.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var repo = _unitOfWork.GetRepository<CustomDesign, Guid>();
            await repo.SoftDeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();
            return NoContent();
        }
    }
}
