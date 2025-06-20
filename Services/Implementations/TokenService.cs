using BusinessObjects.Common;
using BusinessObjects.Identity;
using DTOs.UserDTOs.Identities;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Repositories.Commons;
using Services.Interfaces;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Extensions.Logging;

namespace Services.Implementations
{
    public class TokenService : ITokenService
    {
        private static readonly JwtSecurityTokenHandler _tokenHandler = new();
        private readonly SymmetricSecurityKey _secretKey;
        private readonly JwtSettings _jwtSettings;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<TokenService> _logger;

        public TokenService(
            IOptions<JwtSettings> jwtOptions,
            UserManager<ApplicationUser> userManager,
            ILogger<TokenService> logger)
        {
            _jwtSettings = jwtOptions?.Value ?? throw new ArgumentNullException(nameof(jwtOptions));
            if (string.IsNullOrEmpty(_jwtSettings.Key))
                throw new InvalidOperationException("JWT secret key is not configured.");

            _secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Key));
            _userManager = userManager ?? throw new ArgumentNullException(nameof(userManager));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        public RefreshTokenInfo GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);

            return new RefreshTokenInfo
            {
                Token = Convert.ToBase64String(randomNumber),
                Expiry = DateTime.UtcNow.AddDays(_jwtSettings.RefreshTokenDays)
            };
        }

        private async Task<List<Claim>> GetClaimsAsync(ApplicationUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, user.UserName ?? string.Empty),
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email ?? string.Empty),
                new Claim("FirstName", user.FirstName ?? string.Empty),
                new Claim("LastName", user.LastName ?? string.Empty),
                new Claim("Gender", user.Gender.ToString()),
                new Claim("securityStamp", await _userManager.GetSecurityStampAsync(user))
            };

            var roles = await _userManager.GetRolesAsync(user).ConfigureAwait(false);
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            return claims;
        }

        private JwtSecurityToken GenerateTokenOptions(SigningCredentials signingCredentials, List<Claim> claims)
        {
            return new JwtSecurityToken(
                issuer: _jwtSettings.ValidIssuer,
                audience: _jwtSettings.ValidAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(_jwtSettings.Expires),
                signingCredentials: signingCredentials
            );
        }

        public async Task<ApiResult<string>> GenerateToken(ApplicationUser user)
        {
            if (user == null)
            {
                _logger.LogError("Cannot generate token: user is null");
                return ApiResult<string>.Error(
                    data: null,
                    exception: new ArgumentNullException(nameof(user), "User is null.")
                );
            }

            try
            {
                _logger.LogInformation("Generating token for user: {UserId}", user.Id);

                // Create SigningCredentials using HMAC SHA256 algorithm
                var signingCredentials = new SigningCredentials(_secretKey, SecurityAlgorithms.HmacSha256);

                // Get user claims
                var claims = await GetClaimsAsync(user).ConfigureAwait(false);

                // Generate JWT token
                var tokenOptions = GenerateTokenOptions(signingCredentials, claims);

                // Convert to string
                var token = _tokenHandler.WriteToken(tokenOptions);

                _logger.LogInformation("Token generated successfully for user: {UserId}", user.Id);

                return ApiResult<string>.Success(
                    data: token,
                    message: "Token generated successfully."
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating token for user: {UserId}", user.Id);
                return ApiResult<string>.Failure(ex);
            }
        }
    }
}