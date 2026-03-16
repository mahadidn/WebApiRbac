using Microsoft.EntityFrameworkCore;
using WebApiRbac.Infrastructure.Data;
using WebApiRbac.Infrastructure.Seeder;

namespace WebApiRbac.Extensions
{
    public static class ApplicationBuilderExtensions
    {

        // try catch seeder
        public static async Task SeedDatabaseAsync(this WebApplication app)
        {
            using (var scope = app.Services.CreateScope())
            {
                var services = scope.ServiceProvider;
                try
                {
                    var context = services.GetRequiredService<ApplicationDbContext>();
                    // pastikan database sudah terbuat
                    await context.Database.MigrateAsync();
                    // jalankan seeder
                    await DatabaseSeeder.PermissionSeedAsync(context);
                }
                catch (Exception ex)
                {
                    var logger = services.GetRequiredService<ILogger<Program>>();
                    logger.LogError(ex, "Terjadi kesalahan saat melakukan seeding database.");
                }
            }
        }

    }
}
