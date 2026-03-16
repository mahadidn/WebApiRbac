using WebApiRbac.Application.DTOs.Permission;
using WebApiRbac.Application.Interfaces;
using WebApiRbac.Domain.Interfaces;

namespace WebApiRbac.Application.Services
{
    public class PermissionService : IPermissionService
    {
        private readonly IPermissionRepository _permissionRepository;

        public PermissionService(IPermissionRepository permissionRepository)
        {
            _permissionRepository = permissionRepository;
        }

        public async Task<IEnumerable<PermissionResponseDto>> GetAllPermissionsAsync()
        {
            // ambil data mentah
            var permissions = await _permissionRepository.GetAllAsync();

            // ubah entity menjadi dto menggunakan linq
            return permissions.Select(p => new PermissionResponseDto
            {
                Id = p.Id,
                Name = p.Name
            });
        }

    }
}
