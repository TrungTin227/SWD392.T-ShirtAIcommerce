using BusinessObjects.Identity;
using DTOs.UserDTOs.Request;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Services.Interfaces;
using System.Security.Claims;

namespace WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly IExternalAuthService _externalAuthService;
        private readonly SignInManager<ApplicationUser> _signInManager;

        public AuthController(IUserService userService, IExternalAuthService externalAuthService, SignInManager<ApplicationUser> signInManager)
        {
            _userService = userService;
            _externalAuthService = externalAuthService;
            _signInManager = signInManager;
        }

        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] UserRegisterRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid data");

            var result = await _userService.RegisterAsync(request);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("confirm-email")]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(Guid userId, string token)
        {
            if (userId == Guid.Empty || string.IsNullOrWhiteSpace(token))
                return BadRequest("Invalid parameters");

            var result = await _userService.ConfirmEmailAsync(userId, token);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("resend-confirmation")]
        [AllowAnonymous]
        public async Task<IActionResult> ResendConfirmation([FromBody] ResendEmailRequestDTO dto)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid email");

            var result = await _userService.ResendConfirmationEmailAsync(dto.Email);
            return Ok(result);
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] UserLoginRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid data");

            var result = await _userService.LoginAsync(request);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPut("profile")]
        [Authorize]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateUserRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid data");

            // 1. Lấy claim NameIdentifier (hoặc "sub") an toàn hơn
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)
                           ?? User.FindFirst("sub");
            if (userIdClaim == null
                || !Guid.TryParse(userIdClaim.Value, out var userId))
            {
                return Unauthorized("Invalid or missing user ID claim");
            }

            // 2. Gọi service và trả kết quả
            var result = await _userService.UpdateProfileAsync(userId, request);
            if (result == null)
                // nếu service trả null (không nên), báo lỗi server
                return StatusCode(StatusCodes.Status500InternalServerError, "Update failed");

            return result.IsSuccess
                ? Ok(result)
                : BadRequest(result);
        }

        [HttpPost("logout")]
        [Authorize]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            var result = await _userService.LogoutAsync(request);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("validate-token")]
        [Authorize]
        public async Task<IActionResult> ValidateToken([FromBody] ValidateTokenRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid token");

            var result = await _userService.ValidateTokenAsync(request.Token);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("forgot-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDTO req)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid email");

            var result = await _userService.InitiatePasswordResetAsync(req);
            return Ok(result);
        }

        [HttpPost("reset-password")]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDTO req)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid data");

            var result = await _userService.ResetPasswordAsync(req);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("send-2fa-code")]
        [Authorize]
        public async Task<IActionResult> Send2FACode()
        {
            var result = await _userService.Send2FACodeAsync();
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("verify-2fa")]
        [Authorize]
        public async Task<IActionResult> Verify2FA([FromBody] Verify2FARequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid 2FA code");

            var result = await _userService.Verify2FAAsync(request);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest("Invalid data");

            var result = await _userService.ChangePasswordAsync(request);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("refresh-token")]
        [AllowAnonymous]
        public async Task<IActionResult> RefreshToken([FromBody] RefreshTokenRequest request)
        {
            var result = await _userService.RefreshTokenAsync(request);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("revoke-refresh-token")]
        [Authorize]
        public async Task<IActionResult> RevokeRefreshToken([FromBody] RefreshTokenRequest request)
        {
            var result = await _userService.RevokeRefreshTokenAsync(request);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("current-user")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var result = await _userService.GetCurrentUserAsync();
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpGet("google-login")]
        [AllowAnonymous]
        public IActionResult GoogleLogin()
        {
            var redirectUrl = Url.Action(nameof(GoogleResponse), "Auth");
            var props = _signInManager.ConfigureExternalAuthenticationProperties("Google", redirectUrl);
            return Challenge(props, "Google");
        }

        [HttpGet("google-response")]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleResponse()
        {
            var result = await _externalAuthService.ProcessGoogleLoginAsync();
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }

        [HttpPost("google-login-token")]
        [AllowAnonymous]
        public async Task<IActionResult> GoogleLoginToken([FromBody] GoogleLoginRequest request)
        {
            if (string.IsNullOrEmpty(request.TokenId))
                return BadRequest("Token ID is required");

            var result = await _externalAuthService.ProcessGoogleTokenAsync(request.TokenId);
            return result.IsSuccess ? Ok(result) : BadRequest(result);
        }
    }
}