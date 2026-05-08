using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetCare.Domain.Pets;
using VetCare.Domain.Vaccinations;

namespace VetCare.Infrastructure.Persistence.Configurations;

public sealed class VaccinationConfiguration : IEntityTypeConfiguration<Vaccination>
{
    public void Configure(EntityTypeBuilder<Vaccination> builder)
    {
        builder.ToTable("vaccinations");

        builder.HasKey(v => v.Id);

        builder.Property(v => v.Id).ValueGeneratedNever();

        builder.Property(v => v.TenantId).IsRequired();
        builder.Property(v => v.PetId).IsRequired();

        builder.Property(v => v.VaccineName).HasMaxLength(Vaccination.VaccineNameMaxLength).IsRequired();
        builder.Property(v => v.AdministeredAt).IsRequired();
        builder.Property(v => v.NextDueAt);
        builder.Property(v => v.BatchNumber).HasMaxLength(Vaccination.BatchNumberMaxLength).IsRequired();

        builder.Property(v => v.CreatedAt).IsRequired();
        builder.Property(v => v.UpdatedAt).IsRequired();

        builder.HasIndex(v => new { v.TenantId, v.PetId });

        builder.HasOne<Pet>()
            .WithMany()
            .HasForeignKey(v => v.PetId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Ignore(v => v.DomainEvents);
    }
}
