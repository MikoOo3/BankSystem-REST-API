using BankDataAccessLayer;
using BankSystem.Business.BusinessObjects;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace BankSystem.Business
{
    public class RefreshTokenBusiness
    {
        private static string _HashToken(string rawToken)
        {
            using var sha256 = SHA256.Create();
            var bytes = Encoding.UTF8.GetBytes(rawToken);
            var hash = sha256.ComputeHash(bytes);
            return Convert.ToBase64String(hash);
        }

        public static async Task<bool> SaveRefreshTokenAsync(BankSystemDbContext context, Guid userId, string rawRefreshToken, int expiryDays = 7)
        {
            var tokenEntity = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                TokenHash = _HashToken(rawRefreshToken),
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(expiryDays),
                IsRevoked = false
            };

            context.RefreshTokens.Add(tokenEntity);
            return (await context.SaveChangesAsync()) > 0;
        }

        public static async Task<(UserBusiness? User, string ErrorMessage)> ValidateAndRevokeAsync(BankSystemDbContext context, string rawRefreshToken)
        {
            string hashedToken = _HashToken(rawRefreshToken);

            var tokenEntity = await context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.TokenHash == hashedToken);

            if (tokenEntity == null)
                return (null, "Invalid refresh token.");

            if (tokenEntity.IsRevoked)
                return (null, "Refresh token has already been revoked.");

            if (tokenEntity.ExpiresAt <= DateTime.UtcNow)
                return (null, "Refresh token has expired. Please login again.");


            var user = await context.Users
                        .AsNoTracking()
                        .FirstOrDefaultAsync(u => u.Id == tokenEntity.UserId);

            if (user == null)
                return (null, "User not found.");

            if (!user.IsActive)
                return (null, "User account is deactivated.");

            tokenEntity.IsRevoked = true;
            await context.SaveChangesAsync();

            var userDto = new UserDTO(user.Id, user.FullName, user.Email, user.PasswordHash, user.Role, user.FailedLoginAttempts, user.LockoutEnd, user.IsActive, user.CreatedAt);
            
            return (new UserBusiness(context, userDto, UserBusiness.enMode.Update), string.Empty);
        }

        public static async Task<bool> RevokeTokenAsync(BankSystemDbContext context, string rawRefreshToken)
        {
            string hashedToken = _HashToken(rawRefreshToken);

            var tokenEntity = await context.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.TokenHash == hashedToken && !rt.IsRevoked);

            if (tokenEntity == null) return false;

            tokenEntity.IsRevoked = true;
            return (await context.SaveChangesAsync()) > 0;
        }
    }
}