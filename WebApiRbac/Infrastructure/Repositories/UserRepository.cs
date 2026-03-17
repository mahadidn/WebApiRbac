using Microsoft.EntityFrameworkCore;
using WebApiRbac.Domain.Entities;
using WebApiRbac.Domain.Interfaces;
using WebApiRbac.Infrastructure.Data;

namespace WebApiRbac.Infrastructure.Repositories
{

    // first layer: implementation IUserRepository interface to this class
    public class UserRepository : IUserRepository
    {
        private readonly ApplicationDbContext _context;

        // second layer: Requesting a database bridge (DbContext) from the system
        public UserRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        // get by email
        public async Task<User?> GetByEmailAsync(string email)
        {
            // Mirip dengan User::where('email', email)->first() di Laravel
            return await _context.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        // get by username
        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _context.Users.FirstOrDefaultAsync(u => u.Username == username);
        }

        // get user with role by id
        public async Task<User?> GetByIdAsync(Guid id)
        {
            // Include() digunakan untuk mengambil data relasi Many-to-Many sekaligus (Eager Loading)
            return await _context.Users
                .AsNoTracking()
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.Id == id);
        }

        // get only user
        public async Task<User?> GetOnlyUserByIdAsync(Guid id)
        {
            return await _context.Users
                .FindAsync(id);
        }

        public async Task<(IEnumerable<User> Users, int TotalCount)> GetAllAsync(int pageNumber, int pageSize)
        {
            // siapkan query dasarnya
            var query = _context.Users.AsNoTracking();

            // hitung seluruh data sebelum dipotong pagination
            var totalCount = await query.CountAsync();

            // eksekusi pagination
            var users = await query
                .OrderBy(u => u.Username)
                .Skip((pageNumber - 1) * pageSize) // lewati data halaman sebelumnya
                .Take(pageSize) // ambil data sebanyak ukuran halaman
                .ToListAsync(); // eksekusi ke db

            return (users, totalCount);

        }

        // add new user
        public async Task AddAsync(User user)
        {
            // taruh data di memori antrian EF Core
            await _context.Users.AddAsync(user);

            // simpan perubahan, eksekusi insert into ke postgre
            await _context.SaveChangesAsync();
        }

        // update user
        public async Task UpdateAsync(User user)
        {
            await _context.Users
                .Where(u => u.Id == user.Id)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(u => u.Username, user.Username)
                    .SetProperty(u => u.Email, user.Email)
                    .SetProperty(u => u.UpdatedAt, DateTime.UtcNow)
                );
        }

        // get user roles
        public async Task<IEnumerable<Role>> GetUserRolesAsync(Guid userId)
        {
            var user = await GetByIdAsync(userId);
            return user?.Roles ?? new List<Role>();
        }


        // add multiple roles
        public async Task AddRolesAsync(Guid userId, IEnumerable<Guid> roleIds)
        {
            // ambil ID role yang sudah dimiliki user saat ini
            /*
                 SELECT "RolesId"
                 FROM user_has_roles
                 WHERE "UsersId" = @userId
                 AND "RolesId" IN (@roleId1, @roleId2, ...)
             */
            var existingRoleIds = await _context.Set<UserRole>()
                .AsNoTracking()
                .Where(ur => ur.UsersId == userId && roleIds.Contains(ur.RolesId))
                .Select(ur => ur.RolesId)
                .ToListAsync();

            // Saring, Buang ID yang sudah ada di database dari daftar request.
            var newRoleIds = roleIds.Except(existingRoleIds).ToList();

            // jika semuanya duplikat, langsung berhenti
            if (!newRoleIds.Any())return;

            // masukan hanya data yg benar-benar baru
            var userRoles = newRoleIds.Select(roleId => new UserRole
            {
                UsersId = userId,
                RolesId = roleId
            });

            // eksekusi 1 baris sql batch insert
            await _context.Set<UserRole>().AddRangeAsync(userRoles);
            await _context.SaveChangesAsync();

        }

        // remove role
        public async Task RemoveRolesAsync(Guid userId, IEnumerable<Guid> roleIds)
        {

            roleIds = roleIds.Distinct(); // untuk menghindari duplikat

            // ExecuteDeleteAsync Dia TIDAK menarik data ke RAM. Dia langsung menerjemahkan LINQ ini
            // menjadi "DELETE FROM user_has_roles WHERE..." langsung di server Postgres.
            /*
              Contoh query yg dijalankan:
                DELETE FROM user_has_roles
                WHERE "UsersId" = @userId
                AND "RolesId" IN (@role1, @role2, @role3)
             
             */
            await _context.Set<UserRole>()
                .Where(ur => ur.UsersId == userId && roleIds.Contains(ur.RolesId))
                .ExecuteDeleteAsync();
        }

        // sync roles
        public async Task SyncRolesAsync(Guid userId, IEnumerable<Guid> roleIds)
        {
            // untuk menghindari duplikat
            var uniqueRoleIds = roleIds.Distinct().ToList();

            // tansaksi
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // hapus seluruh role lama
                await _context.Set<UserRole>()
                    .Where(ur => ur.UsersId == userId)
                    .ExecuteDeleteAsync();

                // masukkan role baru (jika ada)
                if (uniqueRoleIds.Any())
                {

                    // cek apakah uniqueRoleIds benar-benar ada di database
                    var validRoleIds = await _context.Roles
                        .Where(r => uniqueRoleIds.Contains(r.Id))
                        .Select(r => r.Id)
                        .ToListAsync();

                    // kalau ada maka tambahkan validRoleIds ke database
                    if (validRoleIds.Any())
                    {
                        var newRoles = validRoleIds.Select(roleId => new UserRole
                        {
                            UsersId = userId,
                            RolesId = roleId
                        });

                        await _context.Set<UserRole>().AddRangeAsync(newRoles);
                        // simpan perubahan insert
                        await _context.SaveChangesAsync();
                    }
                    
                }

                // jika baris ini tercapai, artinya delete dan insert berhasil tanpa error
                await transaction.CommitAsync();
            }
            catch (Exception)
            {
                // jika terjadi error, maka rollback
                await transaction.RollbackAsync();
                throw; // lempar error ke controller
            }
        }

        // ambil daftar nama permission dari semua role yg dimiliki user
        public async Task<IEnumerable<string>> GetUserPermissionsAsync(Guid userId)
        {
            /*
                SELECT DISTINCT p."Name"
                FROM "Users" u
                JOIN "user_has_roles" ur ON u."Id" = ur."UsersId"
                JOIN "Roles" r ON ur."RolesId" = r."Id"
                JOIN "role_has_permissions" rp ON r."Id" = rp."RolesId"
                JOIN "Permissions" p ON rp."PermissionsId" = p."Id"
                WHERE u."Id" = @userId;
             */
            var permissions = await _context.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                // SelectMany digunakan untuk "melebur" List di dalam List (Flattening)
                .SelectMany(u => u.Roles)
                .SelectMany(r => r.Permissions)
                .Select(p => p.Name)
                .Distinct()
                .ToListAsync();

            return permissions;
        }

        // refresh token implementation
        public async Task AddRefreshTokenAsync(RefreshToken refreshToken)
        {
            // insert the token into the memory queue
            await _context.RefreshTokens.AddAsync(refreshToken);

            // store permanently to postgre
            await _context.SaveChangesAsync();
        }

        public async Task<RefreshToken?> GetRefreshTokenAsync(string token)
        {
            // menggunakan .Include(rt => rt.User) di sini
            // Mengapa? Karena saat user membawa string Refresh Token ke API,
            // kita butuh tahu token itu milik siapa (Username, Email, dll) 
            // untuk nantinya dibuatkan JWT yang baru. Include akan otomatis melakukan SQL JOIN.
            /*
             * perintah sql yg dijalanin kira-kira
                SELECT r."Id", r."Token", r."Expires", r."UserId", ..., 
                       u."Id", u."Username", u."Email", ...
                FROM refresh_tokens AS r
                INNER JOIN users AS u ON r."UserId" = u."Id"
                WHERE r."Token" = @token
                LIMIT 1;
             */
            return await _context.RefreshTokens
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.Token == token);
        }

        public async Task UpdateRefreshTokenAsync(RefreshToken refreshToken)
        {
            // update status token (For example: The “Revoked” property is set to the date of logout)
            _context.RefreshTokens.Update(refreshToken);
            await _context.SaveChangesAsync();
        }

        // revoke all refresh token
        public async Task RevokeAllRefreshTokensAsync(Guid userId)
        {
            await _context.RefreshTokens
                .Where(rt => rt.UserId == userId && rt.Revoked == null) // cari token milik user ini yang masih aktif 
                .ExecuteUpdateAsync(s => s.SetProperty(rt => rt.Revoked, DateTime.UtcNow));
        }


    }
}
