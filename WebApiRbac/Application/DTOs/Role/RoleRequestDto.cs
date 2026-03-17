using System.ComponentModel.DataAnnotations;

namespace WebApiRbac.Application.DTOs.Role
{
    public class RoleRequestDto
    {
        // tambahkan validasi agar nama role tidak boleh kosong
        [Required(ErrorMessage = "Nama Role tidak boleh kosong")]
        [StringLength(50, MinimumLength = 3, ErrorMessage = "Nama role harus antara 3 - 50 karakter")]
        public string Name { get; set; } = string.Empty;

        // permissions yg ingin di include
        [Required(ErrorMessage = "Permission ID wajib diisi")]
        public List<Guid> PermissionIds { get; set; } = new List<Guid>();

    }
}
