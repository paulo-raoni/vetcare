using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetCare.Domain.Owners;

namespace VetCare.Infrastructure.Persistence.Configurations;

public sealed class OwnerConfiguration : IEntityTypeConfiguration<Owner>
{
    public void Configure(EntityTypeBuilder<Owner> builder)
    {
        builder.ToTable("owners");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id).ValueGeneratedNever();

        builder.Property(o => o.TenantId).IsRequired();

        builder.Property(o => o.FullName).HasMaxLength(160).IsRequired();
        builder.Property(o => o.Phone).HasMaxLength(32).IsRequired();
        builder.Property(o => o.Email).HasMaxLength(256).IsRequired();

        builder.Property(o => o.CreatedAt).IsRequired();
        builder.Property(o => o.UpdatedAt).IsRequired();

        builder.HasIndex(o => new { o.TenantId, o.Email });

        builder.Ignore(o => o.DomainEvents);
    }
}
