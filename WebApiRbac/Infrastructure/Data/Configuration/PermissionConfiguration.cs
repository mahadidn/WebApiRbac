using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebApiRbac.Domain.Entities;

namespace WebApiRbac.Infrastructure.Data.Configuration
{
    public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
    {
        public void Configure(EntityTypeBuilder<Permission> builder)
        {
            builder.ToTable("permissions");
            builder.Property(p => p.Name).HasMaxLength(100).IsRequired();

        }

    }
}
