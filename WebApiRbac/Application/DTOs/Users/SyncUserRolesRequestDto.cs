using System.ComponentModel.DataAnnotations;

namespace WebApiRbac.Application.DTOs.Users
{
    public class SyncUserRolesRequestDto
    {
        [Required(ErrorMessage = "Daftar Role ID harus diisi")]
        public List<Guid> RoleIds { get; set; } = new List<Guid>();

    }
}
