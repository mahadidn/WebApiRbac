using Microsoft.EntityFrameworkCore;
using WebApiRbac.Application.DTOs.Permission;
using WebApiRbac.Domain.Entities;
using WebApiRbac.Domain.Interfaces;
using WebApiRbac.Infrastructure.Data;

namespace WebApiRbac.Infrastructure.Repositories
{
    public class RoleRepository : IRoleRepository
    {
        private readonly ApplicationDbContext _context;

        public RoleRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // get by id
        public async Task<Role?> GetByIdAsync(Guid id)
        {
            return await _context.Roles
                .AsNoTracking()
                .Include(r => r.Permissions)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<Role?> GetOnlyRoleAsync(Guid id)
        {
            return await _context.Roles
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<bool> ExistsAsync(Guid id)
        {
            // pakai FindAsync sangat cepat karena dia akan mengecek memori lokal EF Core dulu 
            // sebelum menembak query ke PostgreSQL.
            return await _context.Roles.AnyAsync(r => r.Id == id);
        }

        // get by name
        public async Task<Role?> GetByNameAsync(string name)
        {
            return await _context.Roles.FirstOrDefaultAsync(r => r.Name == name);
        }

        // get all
        public async Task<IEnumerable<Role>> GetAllAsync()
        {
            return await _context.Roles
                .AsNoTracking()
                .OrderBy(r => r.Name)
                .ToListAsync();
        }

        // add
        public async Task AddAsync(Role role)
        {
            await _context.Roles.AddAsync(role);
            await _context.SaveChangesAsync();
        }

        // update langsung query update ke 2 kolom aja (name dan updatedat)
        // cukup 1x query
        public async Task UpdateAsync(Role role)
        {
            await _context.Roles
                .Where(r => r.Id == role.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(r => r.Name, role.Name)
                    .SetProperty(r => r.UpdatedAt, role.UpdatedAt)
                );
        }

        // delete
        public async Task DeleteAsync(Guid id)
        {
            await _context.Roles
                .Where(r => r.Id == id)
                .ExecuteDeleteAsync();
        }

        // FUNGSI-FUNGSI RELASI (AddPermissions, SyncPermissions, dll)
        public async Task AddPermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds)
        {
            // cek apakah ada id yg dikirim
            if (permissionIds == null || !permissionIds.Any()) return;

            // tarik role beserta permissionnya dari database
            var role = await _context.Roles
                .Include(r => r.Permissions)
                .FirstOrDefaultAsync(r => r.Id == roleId);
            if (role == null) return; // jika role tidak ditemukan, hentikan

            // Query ini akan mencari ke tabel Permissions: 
            // "Tolong kembalikan ID yang BENAR-BENAR ADA di database dari daftar permissionIds ini."
            /*
             contoh query yg di generate:
                SELECT p."Id"
                FROM "Permissions" p
                WHERE p."Id" IN ('id-1', 'id-2', 'id-3') -- isi dari permissionIds
             */
            var validPermissionIds = await _context.Permissions
                .Where(p => permissionIds.Contains(p.Id))
                .Select(p => p.Id)
                .ToListAsync();

            // loop id hanya yg sudah terbukti valid
            foreach (var validId in validPermissionIds)
            {
                // cek apakah permission ini sudah ada didalam role agar tidak duplikat
                if (!role.Permissions.Any(p => p.Id == validId))
                {
                    // kalau id belum ada, maka tambah relasinya

                    // buat objek bohongan
                    var stubPermission = new Permission { Id = validId };

                    // Beri tahu EF Core: "Tolong jangan Insert data ini ke tabel Permission, 
                    // data ini sudah ada di database, saya cuma mau pakai ID-nya untuk relasi!"
                    _context.Permissions.Attach(stubPermission);

                    // Masukkan objek bohongan itu ke dalam kantong Role
                    role.Permissions.Add(stubPermission);
                }
            }
            // simpan perubahan: EF Core otomatis hanya akan membuat baris baru di tabel perantara (role_permissions)!
            await _context.SaveChangesAsync();
        }
        public async Task RemovePermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds)
        {
            // valiasi awal
            if (permissionIds == null || !permissionIds.Any()) return;

            // tarik role beserta permissionnya
            var role = await _context.Roles
                .Include(r => r.Permissions)
                .FirstOrDefaultAsync(r => r.Id == roleId);
            if (role == null) return;

            // saring: cari permission yg sedang dimiliki oleh Role ini,
            // yang ID-nya cocok dengan daftar ID yang mau dihapus.
            var permissionsToRemove = role.Permissions
                .Where(p => permissionIds.Contains(p.Id))
                .ToList();
            // kalau kosong tidak ada yg perlu dihapus, langsung return saja
            if (!permissionsToRemove.Any()) return;

            // kalau ada, maka buang dari role
            foreach (var permission in permissionsToRemove)
            {
                role.Permissions.Remove(permission);
            }

            // ef core akan mendeteksi permission yg di remove sebelumnya dan simpan
            await _context.SaveChangesAsync();
        }

        public async Task SyncPermissionsAsync(Guid roleId, IEnumerable<Guid> permissionIds)
        {
            // cegah error jika kirim null
            var newPermissionIds = permissionIds?.ToList() ?? new List<Guid>();

            // tarik kondisi database saat ini
            var role = await _context.Roles
                .Include(r => r.Permissions)
                .FirstOrDefaultAsync(r => r.Id == roleId);
            if (role == null) return;

            // ambil daftar ID yg sedang dimiliki role saat ini
            var existingPermissionIds = role.Permissions.Select(p => p.Id).ToList();

            // cari yg harus dihapus (ada di DB, tapi tidak ada di input baru)
            var idsToRemove = existingPermissionIds.Except(newPermissionIds).ToList();

            // cari yg harus ditambah (ada di input baru, tapi tidak ada di DB)
            var idsToAdd = newPermissionIds.Except(existingPermissionIds).ToList();

            // eksekusi penghapusan
            if (idsToRemove.Any())
            {
                var permissionToRemove = role.Permissions
                    .Where(p => idsToRemove.Contains(p.Id))
                    .ToList();

                foreach (var p in permissionToRemove)
                {
                    role.Permissions.Remove(p);
                }
            }

            // eksekusi penambahan
            if (idsToAdd.Any())
            {
                // saring ID bodong
                var validIdsToAdd = await _context.Permissions
                    .Where(p => idsToAdd.Contains(p.Id))
                    .Select(p => p.Id)
                    .ToListAsync();

                foreach (var validId in validIdsToAdd)
                {
                   
                    // Cek apakah permission ini kebetulan sudah ada di memori RAM EF Core saat ini? (apakah sudah di-track?)
                    // Kalau sudah di track, pakai yang di RAM. Kalau tidak ada, baru buat Objek Bohongan (Stub).
                    var permission = _context.Permissions.Local.FirstOrDefault(p => p.Id == validId)
                                    ?? new Permission { Id = validId };

                    // jika statusnya Detached (tidak dilacak oleh EF Core sama sekali), baru Attach
                    if(_context.Entry(permission).State == EntityState.Detached)
                    {
                        _context.Permissions.Attach(permission);
                    }

                    // masukkan ke role
                    role.Permissions.Add(permission);

                }
            }

            // simpan semua perubahan dalam 1 transaksi
            // jika hapus berhasil tapi tambah gagal, otomatis di-rollback semuanya
            await _context.SaveChangesAsync();
        }

        public async Task<IEnumerable<PermissionDto>> GetRolePermissionsAsync(Guid roleId)
        {
            return await _context.Roles
                .AsNoTracking()
                .Where(r => r.Id == roleId)
                .SelectMany(r => r.Permissions)
                .OrderBy(p => p.Name)
                .Select(p => new PermissionDto( p.Id, p.Name)) // select tidak seluruh kolom (hanya kolom spesifik)
                .ToListAsync();

        }

    }
}
