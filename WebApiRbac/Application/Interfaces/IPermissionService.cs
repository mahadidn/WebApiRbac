using WebApiRbac.Application.DTOs.Permission;

namespace WebApiRbac.Application.Interfaces
{
    public interface IPermissionService
    {
        Task<IEnumerable<PermissionResponseDto>> GetAllPermissionsAsync();
    }
}
