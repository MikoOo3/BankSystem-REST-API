using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace BankSystem.API.Services
{
    public static class ClaimsPrincipalExtensions
    {
        public static Guid GetUserId(this ClaimsPrincipal user)
        {
            var idClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                          ?? user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            return Guid.TryParse(idClaim, out Guid userId) ? userId : Guid.Empty;
        }

        public static bool IsBankStaff(this ClaimsPrincipal user)
        {
            return user.IsInRole("Manager") || user.IsInRole("Employee");
        }
    }
}
