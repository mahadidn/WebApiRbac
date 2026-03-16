using WebApiRbac.Application.DTOs.Permission;
using WebApiRbac.Domain.Entities;

namespace WebApiRbac.Domain.Interfaces
{
    public interface IRoleRepository
    {
        Task<Role?> GetByIdAsync(Guid id);
        Task<Role?> GetByNameAsync(string name);
        
        // get all roles
        Task<IEnumerable<Role>> GetAllAsync();

        // role management
        Task AddAsync(Role role);
        Task UpdateAsync(Role role);
        Task DeleteAsync(Guid id);

        // add multiple permissions to a single role
        Task AddPermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds);

        // remove multiple permissions from a single role
        Task RemovePermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds);

        // sync permissions to role
        Task SyncPermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds);

        // what privileges this role has
        Task<IEnumerable<PermissionDto>> GetRolePermissionsAsync(Guid roleId);

    }
}
