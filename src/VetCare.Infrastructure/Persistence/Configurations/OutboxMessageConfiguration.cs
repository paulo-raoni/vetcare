using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VetCare.Infrastructure.Outbox;

namespace VetCare.Infrastructure.Persistence.Configurations;

public sealed class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.Id).ValueGeneratedNever();

        builder.Property(o => o.OccurredOnUtc).IsRequired();

        builder.Property(o => o.Type)
            .HasColumnType("text")
            .IsRequired();

        builder.Property(o => o.Content)
            .HasColumnType("jsonb")
            .IsRequired();

        builder.Property(o => o.TenantId).IsRequired();

        builder.Property(o => o.ProcessedOnUtc);

        builder.Property(o => o.Error).HasColumnType("text");

        builder.Property(o => o.Attempts)
            .HasDefaultValue(0)
            .IsRequired();

        builder.HasIndex(o => new { o.ProcessedOnUtc, o.OccurredOnUtc })
            .HasDatabaseName("IX_outbox_messages_ProcessedOnUtc_OccurredOnUtc");
    }
}
