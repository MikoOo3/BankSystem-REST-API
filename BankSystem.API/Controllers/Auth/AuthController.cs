using BankDataAccessLayer;
using BankSystem.API.Services;
using BankSystem.Business;
using BankSystem.Business.BusinessObjects;
using BankSystem.DataAccess.DTOs.Auth;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace BankSystem.API.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [EnableRateLimiting("AuthLimiter")]
    public class AuthController : ControllerBase
    {
        private readonly BankSystemDbContext _context;
        private readonly JwtTokenService _jwtService;

        public AuthController(BankSystemDbContext context, JwtTokenService jwtService)
        {
            _context = context;
            _jwtService = jwtService;
        }

        [HttpPost("login")]
        public async Task<ActionResult<TokenResponseDTO>> Login([FromBody] LoginDTO loginDto)
        {
            var (user, errorMessage) = await UserBusiness.AuthenticateAsync(_context, loginDto);

            if (user == null)
            {
                await AuditLogBusiness.LogAsync(_context, null, "Failed Login Attempt", null, $"Email: {loginDto.Email}, Reason: {errorMessage}");
                return Unauthorized(new { message = errorMessage });
            }

            var accessToken = _jwtService.GenerateAccessToken(user);
            var refreshToken = _jwtService.GenerateRefreshToken();

            bool isSaved = await RefreshTokenBusiness.SaveRefreshTokenAsync(_context, user.Id, refreshToken);

            if (!isSaved)
                return StatusCode(500, "Failed to generate session token.");

            await AuditLogBusiness.LogAsync(_context, user.Id, "User Login", null, $"Email: {user.Email}, Role: {user.Role}");

            return Ok(new TokenResponseDTO
            {
                AccessToken = accessToken,
                RefreshToken = refreshToken
            });
        }

        [HttpPost("refresh")]
        public async Task<ActionResult<TokenResponseDTO>> Refresh([FromBody] RefreshTokenRequestDTO requestDto)
        {
            var (user, errorMessage) = await RefreshTokenBusiness.ValidateAndRevokeAsync(_context, requestDto.RefreshToken);

            if (user == null)
                return Unauthorized(new { message = errorMessage });

            var newAccessToken = _jwtService.GenerateAccessToken(user);
            var newRefreshToken = _jwtService.GenerateRefreshToken();

            bool isSaved = await RefreshTokenBusiness.SaveRefreshTokenAsync(_context, user.Id, newRefreshToken);

            if (!isSaved)
                return StatusCode(500, "Failed to rotate session token.");

            await AuditLogBusiness.LogAsync(_context, user.Id, "Token Refreshed", null, $"Email: {user.Email}");

            return Ok(new TokenResponseDTO
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] RefreshTokenRequestDTO requestDto)
        {
            bool isRevoked = await RefreshTokenBusiness.RevokeTokenAsync(_context, requestDto.RefreshToken);

            if (!isRevoked)
                return BadRequest(new { message = "Invalid or already revoked token." });

            await AuditLogBusiness.LogAsync(_context, User.GetUserId() == Guid.Empty ? null : User.GetUserId(), "User Logout", null, "Refresh token revoked");

            return Ok(new { message = "Logged out successfully." });
        }
    }
}