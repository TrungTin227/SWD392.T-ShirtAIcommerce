using BusinessObjects.Cart;
using Microsoft.AspNetCore.Mvc;
using Repositories.WorkSeeds.Interfaces;
using WebAPI.Middlewares;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CartItemsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public CartItemsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var repo = _unitOfWork.GetRepository<CartItem, Guid>();
            var items = await repo.GetAllAsync();
            return Ok(items);
        }

        [HttpPost]
        [ServiceFilter(typeof(ValidateModelAttribute))]
        public async Task<IActionResult> Post([FromBody] CartItem item)
        {
            var repo = _unitOfWork.GetRepository<CartItem, Guid>();
            await repo.AddAsync(item);
            await _unitOfWork.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = item.Id }, item);
        }

        [HttpPut("{id}")]
        [ServiceFilter(typeof(ValidateModelAttribute))]
        public async Task<IActionResult> Put(Guid id, [FromBody] CartItem item)
        {
            if (id != item.Id) return BadRequest();
            var repo = _unitOfWork.GetRepository<CartItem, Guid>();
            await repo.UpdateAsync(item);
            await _unitOfWork.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var repo = _unitOfWork.GetRepository<CartItem, Guid>();
            await repo.DeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();
            return NoContent();
        }
    }
}
