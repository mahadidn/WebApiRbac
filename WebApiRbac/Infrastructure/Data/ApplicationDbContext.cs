using Microsoft.EntityFrameworkCore;
using System.Reflection;
using WebApiRbac.Domain.Entities;

namespace WebApiRbac.Infrastructure.Data
{

    // first layer: DbContext inheritance
    public class ApplicationDbContext : DbContext
    {

        // second layer: constructor to receive connection configuration
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) 
        { 
        }

        // third layer: Registering Tables (DbSet)
        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RefreshToken> RefreshTokens { get; set; }

        // fourth layer: fluent API (Setting table names and Many-to-Many relationships)
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Baris ini menyuruh EF Core untuk otomatis memindai seluruh proyek, 
            // mencari semua file yang mengimplementasikan IEntityTypeConfiguration, 
            // dan mengaplikasikannya secara otomatis
            modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        }


    }
}
