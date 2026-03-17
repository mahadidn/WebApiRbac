using System.ComponentModel.DataAnnotations;

namespace WebApiRbac.Application.DTOs.Role
{
    public class SyncPermissionsRequestDto
    {
        [Required(ErrorMessage = "ID wajib diisi")]
        public List<Guid> PermissionIds { get; set; } = new List<Guid>();

    }
}
