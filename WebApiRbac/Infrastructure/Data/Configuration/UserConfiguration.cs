using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebApiRbac.Domain.Entities;

namespace WebApiRbac.Infrastructure.Data.Configuration
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            // nama tabel
            builder.ToTable("users");

            // tipe data
            builder.Property(u => u.Username).HasMaxLength(50).IsRequired();
            builder.Property(u => u.Email).HasMaxLength(255).IsRequired();
            builder.Property(u => u.Password).HasMaxLength(255).IsRequired();

            // index & unique constraint
            builder.HasIndex(u => u.Username).IsUnique();
            builder.HasIndex(u => u.Email).IsUnique();

            // relasi many-to-many
            builder.HasMany(u => u.Roles)
                .WithMany(r => r.Users)
                .UsingEntity<UserRole>(
                    j => j.HasOne(ur => ur.Role).WithMany().HasForeignKey(ur => ur.RolesId),
                    j => j.HasOne(ur => ur.User).WithMany().HasForeignKey(ur => ur.UsersId),
                    j =>
                    {
                        j.ToTable("user_has_roles");
                        j.HasKey(ur => new { ur.RolesId, ur.UsersId });
                    }
                );
        }

    }
}
