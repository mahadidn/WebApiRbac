using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace WebApiRbac.Application.Common.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        // 1. Ambil User ID dan kembalikan dalam bentuk Guid (Bukan string!)
        public static Guid GetUserId(this ClaimsPrincipal user)
        {
            var id = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            // Mengubah string UUID menjadi tipe data Guid yang asli
            return Guid.TryParse(id, out var guid) ? guid : Guid.Empty;
        }

        // 2. Ambil Username
        public static string GetUsername(this ClaimsPrincipal user)
        {
            return user.FindFirst(JwtRegisteredClaimNames.Name)?.Value ?? string.Empty;
        }

        // 3. Ambil Email
        public static string GetEmail(this ClaimsPrincipal user)
        {
            return user.FindFirst(JwtRegisteredClaimNames.Email)?.Value ?? string.Empty;
        }

        public static List<string> GetRoles(this ClaimsPrincipal user)
        {
            return user.FindAll("role").Select(c => c.Value).ToList();
        }

        public static List<string> GetPermissions(this ClaimsPrincipal user)
        {
            return user.FindAll("permission").Select(c => c.Value).ToList();
        }
    }
}
