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

        // dependency injection
        public AuthController(IAuthService authService)
        {
            _authService = authService;
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
                // verifikasi & buat 2 token
                var result = await _authService.LoginAsync(request);

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
                var result = await _authService.RefreshTokenAsync(refreshToken);

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


        // helper method
        private void SetRefreshTokenCookie(string token)
        {
            var cookieOptions = new CookieOptions
            {
                HttpOnly = true, // Wajib! Agar tidak bisa dibaca oleh JavaScript (Anti-XSS)
                Secure = true, // Wajib HTTPS (Browser otomatis mengizinkan localhost)
                SameSite = SameSiteMode.Strict,// Anti-CSRF (Hanya dikirim jika request dari domain yang sama)
                Expires = DateTime.UtcNow.AddDays(7) // umur ini 7 hari
            };

            // memasukkan token kedalam cookkie bernama "refreshToken" dan menitipkannya ke Response browser
            Response.Cookies.Append("refreshToken", token, cookieOptions);
        }

    }
}
