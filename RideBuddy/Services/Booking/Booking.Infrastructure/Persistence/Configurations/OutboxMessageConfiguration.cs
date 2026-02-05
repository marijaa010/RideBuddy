using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Booking.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for OutboxMessage.
/// </summary>
public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("OutboxMessages");

        builder.HasKey(o => o.Id);

        builder.Property(o => o.EventType)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(o => o.Payload)
            .IsRequired();

        builder.Property(o => o.CreatedAt)
            .IsRequired();

        builder.Property(o => o.ProcessedAt);

        builder.Property(o => o.Error)
            .HasMaxLength(2000);

        builder.Property(o => o.RetryCount)
            .HasDefaultValue(0);

        // Index for finding unprocessed messages
        builder.HasIndex(o => o.ProcessedAt)
            .HasDatabaseName("IX_OutboxMessages_ProcessedAt")
            .HasFilter("\"ProcessedAt\" IS NULL");

        builder.HasIndex(o => o.CreatedAt)
            .HasDatabaseName("IX_OutboxMessages_CreatedAt");
    }
}
