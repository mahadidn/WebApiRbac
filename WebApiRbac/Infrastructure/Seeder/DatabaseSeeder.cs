using Microsoft.EntityFrameworkCore;
using WebApiRbac.Domain.Entities;
using WebApiRbac.Infrastructure.Data;

namespace WebApiRbac.Infrastructure.Seeder
{
    public class DatabaseSeeder
    {

        public static async Task PermissionSeedAsync(ApplicationDbContext context)
        {
            // daftar permission
            var permissions = new List<string>
            {
                "users:read", "users:create", "users:update", "users:delete", "users:assign_roles",
                "roles:read", "roles:create", "roles:update", "roles:delete", "roles:assign_permissions",
                "permissions:read"
            };

            bool hasChanges = false;

            // looping & cek, apakah sudah ada di database
            foreach (var permissionName in permissions)
            {
                // jika belum ada, tambahkan
                if (!await context.Permissions.AnyAsync(p => p.Name == permissionName))
                {
                    context.Permissions.Add(new Permission
                    {
                        Name = permissionName,
                    });
                    hasChanges = true;
                }
                
            }

            if (hasChanges)
            {
                await context.SaveChangesAsync();
            }

        }


        public static async Task SuperAdminSeedAsync(ApplicationDbContext context, IConfiguration configuration)
        {
            // 🛡️ Buka Transaksi: Jika gagal di tengah jalan, database akan di-rollback ke kondisi awal
            using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                // ==========================================
                // 1. SIAPKAN ROLE SUPERADMIN & HAK AKSESNYA
                // ==========================================

                // Tarik role superadmin beserta hak aksesnya (jika sudah ada)
                var superAdminRole = await context.Roles
                    .Include(r => r.Permissions)
                    .FirstOrDefaultAsync(r => r.Name == "Superadmin");

                // Jika belum ada, buat baru
                if (superAdminRole == null)
                {
                    superAdminRole = new Role { Name = "Superadmin" };
                    await context.Roles.AddAsync(superAdminRole);
                }

                // Tarik SEMUA permission yang ada di tabel Permissions (hasil dari Permission Seeder)
                var allPermissions = await context.Permissions.ToListAsync();

                // Pengecekan Tahan Banting: Tambahkan HANYA permission yang belum dimiliki role ini
                foreach (var permission in allPermissions)
                {
                    if (!superAdminRole.Permissions.Any(p => p.Id == permission.Id))
                    {
                        superAdminRole.Permissions.Add(permission);
                    }
                }

                // Simpan perubahan ke database (agar Role punya ID jika baru dibuat)
                await context.SaveChangesAsync();


                // ==========================================
                // 2. SIAPKAN AKUN SUPERADMIN & JABATANNYA
                // ==========================================

                // Tarik user superadmin beserta rolenya (jika sudah ada)
                var superAdminUser = await context.Users
                    .Include(u => u.Roles)
                    .FirstOrDefaultAsync(u => u.Email == "superadmin@admin.com");

                // Jika belum ada, buat baru
                if (superAdminUser == null)
                {

                    // BACA DARI USER SECRETS / APPSETTINGS
                    var adminPassword = configuration["SuperAdmin:Password"];
                    // Cegah aplikasi berjalan kalau admin lupa setting password di server!
                    if (string.IsNullOrEmpty(adminPassword))
                    {
                        throw new Exception("CRITICAL ERROR: Password Superadmin belum disetting di User Secrets / appsettings.json!");
                    }

                    superAdminUser = new User
                    {
                        Username = "superadmin",
                        Email = "superadmin@admin.com",

                        // ⚠️ PENTING: Sesuaikan fungsi Hash ini dengan library yang kamu pakai di AuthService!
                        // Jika kamu pakai BCrypt, biarkan seperti ini. Jika pakai yang lain, silakan sesuaikan.
                        Password = BCrypt.Net.BCrypt.HashPassword(adminPassword)
                    };

                    await context.Users.AddAsync(superAdminUser);
                }

                // Pengecekan Tahan Banting: Tambahkan Role Superadmin HANYA jika user ini belum memilikinya
                if (!superAdminUser.Roles.Any(r => r.Id == superAdminRole.Id))
                {
                    superAdminUser.Roles.Add(superAdminRole);
                }

                // Simpan semua perubahan
                await context.SaveChangesAsync();

                // Jika semua baris di atas sukses tanpa error, resmikan transaksinya!
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                // Jika ada yang meledak, batalkan semuanya agar database tidak kotor
                await transaction.RollbackAsync();
                throw;
            }
        }

    }
}
