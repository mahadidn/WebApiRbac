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

    }
}
