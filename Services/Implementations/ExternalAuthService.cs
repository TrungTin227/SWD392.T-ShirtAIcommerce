using BusinessObjects.Identity;
using DTOs.UserDTOs.Identities;
using DTOs.UserDTOs.Response;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Repositories.Commons;
using Services.Helpers.Mappers;
using Services.Interfaces;
using System.Security.Claims;

public class ExternalAuthService : IExternalAuthService
{
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly IUserService _userService;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITokenService _tokenService;
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly JwtSettings _jwtSettings;
    private readonly IConfiguration _configuration;

    public ExternalAuthService(
        SignInManager<ApplicationUser> signInManager,
        IUserService userService,
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService,
        IHttpContextAccessor httpContextAccessor,
        IOptions<JwtSettings> jwtOptions,
        IConfiguration configuration)
    {
        _signInManager = signInManager;
        _userService = userService;
        _userManager = userManager;
        _tokenService = tokenService;
        _httpContextAccessor = httpContextAccessor;
        _jwtSettings = jwtOptions.Value;
        _configuration = configuration;
    }

    public async Task<ApiResult<UserResponse>> ProcessGoogleTokenAsync(string tokenId)
    {
        try
        {
            Console.WriteLine($"Using Client ID: {_configuration["Authentication:Google:ClientId"]}");

            // Fix: Properly validate Google token with RS256 algorithm
            var validationSettings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { _configuration["Authentication:Google:ClientId"] },
                HostedDomain = null,
                IssuedAtClockTolerance = TimeSpan.FromSeconds(30),
                ExpirationTimeClockTolerance = TimeSpan.FromSeconds(30)
            };

            GoogleJsonWebSignature.Payload payload;
            try
            {
                payload = await GoogleJsonWebSignature.ValidateAsync(tokenId, validationSettings);
            }
            catch (InvalidJwtException jwtEx)
            {
                Console.WriteLine($"JWT validation failed: {jwtEx.Message}");
                return ApiResult<UserResponse>.Failure(new Exception($"Invalid Google token: {jwtEx.Message}"));
            }

            Console.WriteLine($"Token validated successfully for email: {payload.Email}");

            var googleInfo = new GoogleUserInfo
            {
                Email = payload.Email,
                FirstName = payload.GivenName,
                LastName = payload.FamilyName
            };

            // FIX: Ensure all async operations are properly awaited
            var userResp = await _userService.CreateOrUpdateGoogleUserAsync(googleInfo);
            if (userResp == null)
                return ApiResult<UserResponse>.Failure(new Exception("Cannot create or update Google user"));

            var user = await _userManager.FindByEmailAsync(payload.Email);
            if (user == null)
                return ApiResult<UserResponse>.Failure(new Exception("User not found after creation"));

            // FIX: Await token generation properly
            var tokenResult = await _tokenService.GenerateToken(user);
            if (!tokenResult.IsSuccess)
                return ApiResult<UserResponse>.Failure(new Exception("Cannot generate token"));

            var accessToken = tokenResult.Data;
            var refreshToken = _tokenService.GenerateRefreshToken();

            // Set HttpOnly cookie for refresh token
            var cookieOpts = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays)
            };
            _httpContextAccessor.HttpContext!
                .Response.Cookies.Append("refreshToken", refreshToken.Token, cookieOpts);

            // FIX: Ensure user response mapping is awaited
            var dto = await user.ToUserResponseAsync(_userManager, accessToken);
            return ApiResult<UserResponse>.Success(dto, "Google authentication successful");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Google authentication error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return ApiResult<UserResponse>.Failure(new Exception($"Google authentication failed: {ex.Message}"));
        }
    }

    public async Task<ApiResult<UserResponse>> ProcessGoogleLoginAsync()
    {
        try
        {
            // 1) Get Google login info
            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
                return ApiResult<UserResponse>.Failure(new Exception("Google login information not found"));

            // 2) Try to sign in if already linked
            var signInResult = await _signInManager.ExternalLoginSignInAsync(
                info.LoginProvider, info.ProviderKey,
                isPersistent: false, bypassTwoFactor: true);

            if (!signInResult.Succeeded)
            {
                // 3) Create or update User - FIX: Ensure sequential async operations
                var email = info.Principal.FindFirstValue(ClaimTypes.Email);
                var given = info.Principal.FindFirstValue(ClaimTypes.GivenName);
                var family = info.Principal.FindFirstValue(ClaimTypes.Surname);

                var googleInfo = new GoogleUserInfo
                {
                    Email = email,
                    FirstName = given,
                    LastName = family
                };

                // FIX: Await user creation/update
                var userResp = await _userService.CreateOrUpdateGoogleUserAsync(googleInfo);
                if (userResp == null)
                    return ApiResult<UserResponse>.Failure(new Exception("Cannot create or update Google user"));

                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                    return ApiResult<UserResponse>.Failure(new Exception("User not found after creation"));

                // 4) Confirm email - FIX: Await user update
                if (!user.EmailConfirmed)
                {
                    user.EmailConfirmed = true;
                    var updateResult = await _userManager.UpdateAsync(user);
                    if (!updateResult.Succeeded)
                    {
                        return ApiResult<UserResponse>.Failure(new Exception("Failed to confirm email"));
                    }
                }

                // 5) Link Google login - FIX: Await login addition
                var addLoginResult = await _userManager.AddLoginAsync(user, info);
                if (!addLoginResult.Succeeded)
                {
                    return ApiResult<UserResponse>.Failure(new Exception("Failed to link Google account"));
                }

                // 6) Sign-in cookie - FIX: Await sign in
                await _signInManager.SignInAsync(user, isPersistent: false);

                // 7) Generate tokens - FIX: Await token generation
                var tokenResult = await _tokenService.GenerateToken(user);
                if (!tokenResult.IsSuccess)
                    return ApiResult<UserResponse>.Failure(new Exception("Cannot generate token"));

                var accessToken = tokenResult.Data;
                var refreshToken = _tokenService.GenerateRefreshToken();

                // 8) Set HttpOnly cookie for refresh token
                var cookieOpts = new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Strict,
                    Expires = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays)
                };
                _httpContextAccessor.HttpContext!
                    .Response.Cookies.Append("refreshToken", refreshToken.Token, cookieOpts);

                // 9) Return access token and user info - FIX: Await response mapping
                var dto = await user.ToUserResponseAsync(_userManager, accessToken);
                return ApiResult<UserResponse>.Success(dto, "Google login successful");
            }

            // 10) If already linked - FIX: Ensure all async operations are awaited
            var existingUser = await _userManager.FindByLoginAsync(info.LoginProvider, info.ProviderKey);
            if (existingUser == null)
                return ApiResult<UserResponse>.Failure(new Exception("Linked user not found"));

            var existingTokenResult = await _tokenService.GenerateToken(existingUser);
            if (!existingTokenResult.IsSuccess)
                return ApiResult<UserResponse>.Failure(new Exception("Cannot generate token for existing user"));

            var existingAccess = existingTokenResult.Data;
            var newRefresh = _tokenService.GenerateRefreshToken();

            // Update cookie
            var opts = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays)
            };
            _httpContextAccessor.HttpContext!
                .Response.Cookies.Append("refreshToken", newRefresh.Token, opts);

            var existingDto = await existingUser.ToUserResponseAsync(_userManager, existingAccess);
            return ApiResult<UserResponse>.Success(existingDto, "Google login successful");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Google login error: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            return ApiResult<UserResponse>.Failure(new Exception($"Google login failed: {ex.Message}"));
        }
    }
}