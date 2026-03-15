using WebApiRbac.Application.DTOs.Auth;
using WebApiRbac.Application.DTOs.Users;

namespace WebApiRbac.Application.Interfaces
{
    public interface IAuthService
    {
        // receive DTO request, and return DTO response
        Task<UserResponseDto> RegisterAsync(RegisterRequestDto request);
        Task<LoginResponseDto> LoginAsync(LoginRequestDto request, string? ipAddress = null);
        Task<LoginResponseDto> RefreshTokenAsync(string refreshToken, string? ipAddress = null);
        Task LogoutAsync(string refreshToken);

    }
}
