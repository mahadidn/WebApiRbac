using System.ComponentModel.DataAnnotations;

namespace WebApiRbac.Application.DTOs.Auth
{
    public class LoginRequestDto
    {
        // login menggunakan username/email
        [Required(ErrorMessage = "Username or Email is required")]
        public string Identifier { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; } = string.Empty;

    }
}
