using DTOs.UserAddressDTOs.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using WebAPI.Middlewares;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UserAddressController : ControllerBase
    {
        private readonly IUserAddressService _userAddressService;

        public UserAddressController(IUserAddressService userAddressService)
        {
            _userAddressService = userAddressService;
        }

        [HttpGet]
        public async Task<IActionResult> GetUserAddresses()
        {
            var result = await _userAddressService.GetUserAddressesAsync();
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserAddress(Guid id)
        {
            var result = await _userAddressService.GetUserAddressByIdAsync(id);
            return result.IsSuccess ? Ok(result) : NotFound(result);
        }

        [HttpPost]
        [ServiceFilter(typeof(ValidateModelAttribute))]
        public async Task<IActionResult> CreateUserAddress([FromBody] CreateUserAddressRequest request)
        {
            var result = await _userAddressService.CreateUserAddressAsync(request);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPut("{id}")]
        [ServiceFilter(typeof(ValidateModelAttribute))]
        public async Task<IActionResult> UpdateUserAddress(Guid id, [FromBody] UpdateUserAddressRequest request)
        {
            var result = await _userAddressService.UpdateUserAddressAsync(id, request);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
    }
}