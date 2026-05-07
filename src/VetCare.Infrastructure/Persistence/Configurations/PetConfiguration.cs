using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetCare.Domain.Pets;

namespace VetCare.Infrastructure.Persistence.Configurations;

public sealed class PetConfiguration : IEntityTypeConfiguration<Pet>
{
    public void Configure(EntityTypeBuilder<Pet> builder)
    {
        builder.ToTable("pets");

        builder.HasKey(p => p.Id);

        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.TenantId).IsRequired();

        builder.Property(p => p.OwnerId).IsRequired();

        builder.Property(p => p.Name).HasMaxLength(80).IsRequired();
        builder.Property(p => p.Species).HasConversion<int>().IsRequired();
        builder.Property(p => p.Breed).HasMaxLength(80).IsRequired();
        builder.Property(p => p.DateOfBirth).IsRequired();
        builder.Property(p => p.PhotoUrl).HasMaxLength(512);

        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired();

        builder.HasIndex(p => new { p.TenantId, p.OwnerId });

        builder.Ignore(p => p.DomainEvents);
    }
}
