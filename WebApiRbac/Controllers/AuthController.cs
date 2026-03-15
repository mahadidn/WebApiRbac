using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using WebApiRbac.Application.DTOs.Auth;
using WebApiRbac.Application.Interfaces;

namespace WebApiRbac.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;
        private readonly IWebHostEnvironment _env;

        // dependency injection
        public AuthController(IAuthService authService, IConfiguration configuration, IWebHostEnvironment env)
        {
            _authService = authService;
            _configuration = configuration;
            _env = env;
        }

        // Register
        [HttpPost("register")]
        // [FromBody] memberi tahu .NET bahwa data RegisterRequestDto harus diambil dari Body HTTP Request dalam format JSON, bukan dari URL (query string).
        public async Task<IActionResult> Register([FromBody] RegisterRequestDto request) 
        {
            try
            {
                var result = await _authService.RegisterAsync(request);

                // jika berhasil daftar
                return StatusCode(201, new
                {
                    message = "User successfully registered!",
                    data = result
                });
            }
            catch (Exception ex)
            {
                // jika sudah terdaftar (misal username & email)
                return BadRequest(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequestDto request)
        {
            try
            {
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                // verifikasi & buat 2 token
                var result = await _authService.LoginAsync(request, ipAddress);

                // selipkan refresh token ke dalam amplop cookie
                SetRefreshTokenCookie(result.RefreshToken);

                // sajikan hanya access token jwt ke json
                // tidak me-return result.RefreshToken ke sini agar tidak bocor ke body JSON!
                return Ok(new
                {
                    message = "Login berhasil",
                    data = new
                    {
                        accessToken = result.AccessToken,
                        tokenType = result.TokenType,
                        expiresIn = result.ExpiresIn
                    }
                });
            }
            catch (Exception ex)
            {
                // Jika gagal login (password salah), kembalikan error 401 Unauthorized
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpPost("refresh-token")]
        public async Task<IActionResult> RefreshToken()
        {
            try
            {
                // 1. Pelayan memeriksa apakah tamu membawa Amplop Cookie bernama "refreshToken"
                var refreshToken = Request.Cookies["refreshToken"];

                if (string.IsNullOrEmpty(refreshToken))
                {
                    return Unauthorized(new { message = "Refresh Token tidak ditemukan di dalam Cookie." });
                }

                // 2. Serahkan token lama itu ke Koki untuk divalidasi dan ditukar dengan yang baru
                var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
                var result = await _authService.RefreshTokenAsync(refreshToken, ipAddress);

                // 3. Selipkan Refresh Token YANG BARU ke dalam Amplop Cookie (menimpa cookie yang lama)
                SetRefreshTokenCookie(result.RefreshToken);

                // 4. Sajikan JWT yang baru ke atas piring JSON
                return Ok(new
                {
                    message = "Token berhasil diperbarui",
                    data = new
                    {
                        accessToken = result.AccessToken,
                        tokenType = result.TokenType,
                        expiresIn = result.ExpiresIn
                    }
                });
            }
            catch (Exception ex)
            {
                // Jika terjadi anomali (misal: Reuse Detection menyala), 
                // kita hapus cookie-nya agar browser user ikut ter-logout.
                Response.Cookies.Delete("refreshToken");

                // Kembalikan error 401 agar Frontend mengarahkan user ke halaman Login
                return Unauthorized(new { message = ex.Message });
            }
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout()
        {
            // ambil token dari cookie
            var refreshToken = Request.Cookies["refreshToken"];

            // kalau tokennya tidak null/empty
            if (!string.IsNullOrEmpty(refreshToken))
            {
                // matikan token ini di database
                await _authService.LogoutAsync(refreshToken);
            }

            // perintahkan browser untuk menghapus cookie
            // browser akan mencari cookie dengan nama "refreshToken" dan langsung menghancurkannya.
            Response.Cookies.Delete("refreshToken");

            return Ok(new
            {
                message = "Logout berhasil."
            });
        }


        // helper method
        private void SetRefreshTokenCookie(string token)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true, // Wajib! Agar tidak bisa dibaca oleh JavaScript (Anti-XSS)
                Secure = true, // Wajib HTTPS (Browser otomatis mengizinkan localhost)
                SameSite = _env.IsProduction() ? SameSiteMode.Strict : SameSiteMode.Lax,// Anti-CSRF (Hanya dikirim jika request dari domain yang sama)
                Expires = DateTime.UtcNow.AddDays(
                    Convert.ToInt32(_configuration["Jwt:RefreshTokenExpireDays"] ?? "7")
                ) 
            };

            // memasukkan token kedalam cookkie bernama "refreshToken" dan menitipkannya ke Response browser
            Response.Cookies.Append("refreshToken", token, cookieOptions);
        }

    }
}
