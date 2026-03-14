using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using WebApiRbac.Domain.Entities;

namespace WebApiRbac.Infrastructure.Data.Configuration
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("refresh_tokens");

            //tipe data
            builder.Property(rt => rt.Token).HasMaxLength(255).IsRequired();

            // IPv6 memiliki panjang max 45 characters
            builder.Property(rt => rt.CreatedByIp).HasMaxLength(45);

            // relasi ke user (One-to-Many)
            builder.HasOne(rt => rt.User)
                .WithMany(u => u.RefreshTokens)
                .HasForeignKey(rt => rt.UserId)
                .OnDelete(DeleteBehavior.Cascade); // kalau user dihapus, semua tokennya ilang
        }

    }
}
