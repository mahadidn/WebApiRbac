using WebApiRbac.Application.DTOs.Role;
using WebApiRbac.Application.Interfaces;
using WebApiRbac.Domain.Entities;
using WebApiRbac.Domain.Interfaces;

namespace WebApiRbac.Application.Services
{
    public class RoleService : IRoleService
    {
        private readonly IRoleRepository _roleRepository;

        public RoleService(IRoleRepository roleRepository)
        {
            _roleRepository = roleRepository;
        }


        // ambil semua role
        public async Task<IEnumerable<RolesResponseDto>> GetAllRolesAsync()
        {

            var roles = await _roleRepository.GetAllAsync();

            return roles.Select(r => new RolesResponseDto
            {
                Id = r.Id,
                Name = r.Name,
                UpdatedAt = r.UpdatedAt,
                CreatedAt = r.CreatedAt
            });

        }

        // ambil 1 role beserta daftar hak aksesnya
        public async Task<RoleResponseDto?> GetRoleByIdAsync(Guid id)
        {
            var role = await _roleRepository.GetByIdAsync(id);

            if (role == null) return null;

            return new RoleResponseDto
            {
                Id = role.Id,
                Name = role.Name,
                CreatedAt = role.CreatedAt,
                UpdatedAt = role.UpdatedAt,
                Permissions = role.Permissions.Select(p => p.Name).ToList(),
            };

        }
        // buat role baru (menerima request DTO, mengembalikan response DTO)
        public async Task<RoleResponseDto> CreateRoleAsync(RoleRequestDto request)
        {

            // apakah role sudah dipakai
            var existingRole = await _roleRepository.GetByNameAsync(request.Name);
            if(existingRole != null)
            {
                throw new Exception($"Role dengan nama '{request.Name}' sudah digunakan!");
            }

            // buat entity
            // ubah DTO menjadi Entity untuk disimpan ke database
            var newRole = new Role
            {
                Name = request.Name
            };

            // simpan ke database
            await _roleRepository.AddAsync(newRole);

            // tambahkan permission
            if(request.PermissionIds != null && request.PermissionIds.Any())
            {
                await _roleRepository.AddPermissionsAsync(newRole.Id, request.PermissionIds);
            }

            // kembalikan hasilnya, beserta dengan permission
            // Untuk menampilkan hasil yang utuh (beserta nama permissions-nya), 
            // tarik ulang dari database menggunakan fungsi GetById yang sudah dibuat di atas.
            return (await GetRoleByIdAsync(newRole.Id))!;
            
        }
        
        // ubah role 
        public async Task<RoleResponseDto> UpdateRoleAsync(Guid id, RoleRequestDto request)
        {
            // pastikan role yg diedit emang ada
            var existingRole = await _roleRepository.GetOnlyRoleAsync(id);
            if(existingRole == null) throw new Exception($"Role dengan ID {id} tidak ditemukan.");

            // validasi konflik nama
            var roleWithSameName = await _roleRepository.GetByNameAsync(request.Name);
            // validasi, kalau namanya ditemukan dan namanya tidak sama seperti nama role dengan nama role lama
            if(roleWithSameName != null && roleWithSameName.Name != existingRole.Name)
            {
                throw new Exception($"Role dengan nama '{request.Name}' sudah digunakan oleh role lain");
            }

            // eksekusi update nama
            var roleToUpdate = new Role { Id = id, Name = request.Name };
            await _roleRepository.UpdateAsync(roleToUpdate);

            // sinkronisasi hak akses (jika dikirim)
            // cek dulu jika null, artinya:
            // - jika kirim array kosong [], berarti mau menghapus semua permissionnya (dieksekusi)
            // - jika tidak kirim apa-apa (null), berarti cuma mau ganti nama role saja (dilewati)
            if(request.PermissionIds != null)
            {
                await _roleRepository.SyncPermissionsAsync(id, request.PermissionIds);
            }

            // tarik ulang data terbaru dari database, & kembalikan ke controller
            return (await GetRoleByIdAsync(id))!;

        }
        
        // hapus role
        public async Task DeleteRoleAsync(Guid id)
        {

            var existingRole = await _roleRepository.ExistsAsync(id);
            if(!existingRole) throw new Exception($"Role dengan ID {id} tidak ditemukan.");
            
            // hapus role
            await _roleRepository.DeleteAsync(id);

        }
        
        // sync permission
        public async Task SyncRolePermissionAsync(Guid roleId, SyncPermissionsRequestDto request)
        {
            // eksekusi (SyncPermissionsAsync sudah meriksa rolenya ada atau nggak)
            await _roleRepository.SyncPermissionsAsync(roleId, request.PermissionIds);

        }

    }
}
