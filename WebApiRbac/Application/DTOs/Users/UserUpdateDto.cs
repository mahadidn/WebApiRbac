using System.ComponentModel.DataAnnotations;

namespace WebApiRbac.Application.DTOs.Users
{
    public class UserUpdateDto
    {

        [Required(ErrorMessage = "Username tidak boleh kosong")]
        public string Username { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email tidak boleh kosong")]
        [EmailAddress(ErrorMessage = "Format email tidak valid")]
        public string Email { get; set; } = string.Empty;

    }
}
