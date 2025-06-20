using BusinessObjects.Shipping;
using Microsoft.AspNetCore.Mvc;
using Repositories.WorkSeeds.Interfaces;
using WebAPI.Middlewares;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ShippingMethodsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public ShippingMethodsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var repo = _unitOfWork.GetRepository<ShippingMethod, Guid>();
            var items = await repo.GetAllAsync();
            return Ok(items);
        }

        [HttpPost]
        [ServiceFilter(typeof(ValidateModelAttribute))]
        public async Task<IActionResult> Post([FromBody] ShippingMethod method)
        {
            var repo = _unitOfWork.GetRepository<ShippingMethod, Guid>();
            await repo.AddAsync(method);
            await _unitOfWork.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = method.Id }, method);
        }

        [HttpPut("{id}")]
        [ServiceFilter(typeof(ValidateModelAttribute))]
        public async Task<IActionResult> Put(Guid id, [FromBody] ShippingMethod method)
        {
            if (id != method.Id) return BadRequest();
            var repo = _unitOfWork.GetRepository<ShippingMethod, Guid>();
            await repo.UpdateAsync(method);
            await _unitOfWork.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var repo = _unitOfWork.GetRepository<ShippingMethod, Guid>();
            await repo.SoftDeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();
            return NoContent();
        }
    }
}
