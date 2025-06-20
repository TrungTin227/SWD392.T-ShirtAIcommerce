using BusinessObjects.Orders;
using Microsoft.AspNetCore.Mvc;
using Repositories.WorkSeeds.Interfaces;
using WebAPI.Middlewares;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public OrdersController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var repo = _unitOfWork.GetRepository<Order, Guid>();
            var items = await repo.GetAllAsync();
            return Ok(items);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Get(Guid id)
        {
            var repo = _unitOfWork.GetRepository<Order, Guid>();
            var item = await repo.GetByIdAsync(id);
            if (item == null) return NotFound();
            return Ok(item);
        }

        [HttpPost]
        [ServiceFilter(typeof(ValidateModelAttribute))]
        public async Task<IActionResult> Post([FromBody] Order order)
        {
            var repo = _unitOfWork.GetRepository<Order, Guid>();
            await repo.AddAsync(order);
            await _unitOfWork.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = order.Id }, order);
        }

        [HttpPut("{id}")]
        [ServiceFilter(typeof(ValidateModelAttribute))]
        public async Task<IActionResult> Put(Guid id, [FromBody] Order order)
        {
            if (id != order.Id) return BadRequest();
            var repo = _unitOfWork.GetRepository<Order, Guid>();
            await repo.UpdateAsync(order);
            await _unitOfWork.SaveChangesAsync();
            return NoContent();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete(Guid id)
        {
            var repo = _unitOfWork.GetRepository<Order, Guid>();
            await repo.SoftDeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();
            return NoContent();
        }
    }
}
