using BusinessObjects.Identity;
using DTOs.UserDTOs.Request;
using DTOs.UserDTOs.Response;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using WebAPI.Middlewares;

namespace WebAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ITokenService _tokenService;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ITokenService tokenService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
        }

        [HttpPost("register")]
        [ServiceFilter(typeof(ValidateModelAttribute))]
        public async Task<IActionResult> Register([FromBody] UserRegisterRequestDTO request)
        {
            var user = new ApplicationUser
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Gender = request.Gender
            };

            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                return BadRequest(result.Errors);
            }

            await _userManager.AddToRoleAsync(user, "Customer");
            return Ok(new { user.Id, user.Email });
        }

        [HttpPost("login")]
        [ServiceFilter(typeof(ValidateModelAttribute))]
        public async Task<IActionResult> Login([FromBody] UserLoginRequest request)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);
            if (user == null) return Unauthorized();

            var result = await _signInManager.CheckPasswordSignInAsync(user, request.Password, false);
            if (!result.Succeeded) return Unauthorized();

            var tokenResult = await _tokenService.GenerateToken(user);
            if (!tokenResult.IsSuccess) return StatusCode(500, tokenResult.Message);

            return Ok(new LoginResponse(tokenResult.Data!));
        }
    }
}
