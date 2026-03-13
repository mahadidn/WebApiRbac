using Microsoft.EntityFrameworkCore;
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

        // fourth layer: fluent API (Setting table names and Many-to-Many relationships)
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // customize the main table name (optional, to make all letters lowercase as in PostgreSQL)
            modelBuilder.Entity<User>().ToTable("users");
            modelBuilder.Entity<Role>().ToTable("roles");
            modelBuilder.Entity<Permission>().ToTable("permissions");

            // configuration many to many relationship: user-role
            modelBuilder.Entity<User>()
                .HasMany(u => u.Roles)
                .WithMany(r => r.Users)
                // Memaksa EF Core membuat tabel perantara dengan nama spesifik sesuai ERD
                //.UsingEntity(j => j.ToTable("user_has_roles"));
                // Memasukkan class UserRole sebagai representasi resmi tabel ini
                .UsingEntity<UserRole>(
                    j => j.HasOne(ur => ur.Role).WithMany().HasForeignKey(ur => ur.RolesId),
                    j => j.HasOne(ur => ur.User).WithMany().HasForeignKey(ur => ur.UsersId),
                    j =>
                    {
                        j.ToTable("user_has_roles");
                        j.HasKey(ur => new { ur.RolesId, ur.UsersId }); // set composite primary key
                    }
                );

            // configuration many to many relationship: role-permission
            modelBuilder.Entity<Role>()
                .HasMany(r => r.Permissions)
                .WithMany(p => p.Roles)
                // Memaksa EF Core membuat tabel perantara dengan nama spesifik sesuai ERD 
                .UsingEntity(j => j.ToTable("role_has_permissions"));

            // Menambahkan Index kustom untuk performa pencarian Login
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique(); // Menjamin tidak ada username ganda

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();
        }


    }
}
