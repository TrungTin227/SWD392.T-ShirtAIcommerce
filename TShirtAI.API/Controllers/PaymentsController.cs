using BusinessObjects.Entities.Payments;
using Microsoft.AspNetCore.Mvc;
using Repositories.WorkSeeds.Interfaces;
using WebAPI.Middlewares;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly IUnitOfWork _unitOfWork;

        public PaymentsController(IUnitOfWork unitOfWork)
        {
            _unitOfWork = unitOfWork;
        }

        [HttpGet]
        public async Task<IActionResult> Get()
        {
            var repo = _unitOfWork.GetRepository<Payment, Guid>();
            var items = await repo.GetAllAsync();
            return Ok(items);
        }

        [HttpPost]
        [ServiceFilter(typeof(ValidateModelAttribute))]
        public async Task<IActionResult> Post([FromBody] Payment payment)
        {
            var repo = _unitOfWork.GetRepository<Payment, Guid>();
            await repo.AddAsync(payment);
            await _unitOfWork.SaveChangesAsync();
            return CreatedAtAction(nameof(Get), new { id = payment.Id }, payment);
        }
    }
}
