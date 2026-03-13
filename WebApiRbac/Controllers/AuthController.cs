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

    }
}
