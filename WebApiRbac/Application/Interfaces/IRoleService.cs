using WebApiRbac.Application.DTOs.Role;

namespace WebApiRbac.Application.Interfaces
{
    public interface IRoleService
    {
        // ambil semua role
        Task<IEnumerable<RolesResponseDto>> GetAllRolesAsync();

        // ambil 1 role beserta daftar hak aksesnya
        Task<RoleResponseDto?> GetRoleByIdAsync(Guid id);

        // buat role baru (menerima request DTO, mengembalikan response DTO)
        Task<RoleResponseDto> CreateRoleAsync(RoleRequestDto request);

        // ubah role 
        Task<RoleResponseDto> UpdateRoleAsync(Guid id, RoleRequestDto request);

        // hapus role
        Task DeleteRoleAsync(Guid id);

        // sync permission
        Task SyncRolePermissionAsync(Guid roleId, SyncPermissionsRequestDto request);

    }
}
