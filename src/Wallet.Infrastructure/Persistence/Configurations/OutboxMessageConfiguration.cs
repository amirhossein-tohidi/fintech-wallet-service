using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wallet.Infrastructure.Persistence.Messaging;

namespace Wallet.Infrastructure.Persistence.Configurations;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EventType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.Payload)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.LockedBy)
            .HasMaxLength(200);

        builder.Property(x => x.Error)
            .HasColumnType("nvarchar(max)");

        builder.HasIndex(x => new { x.ProcessedAt, x.LockedUntil, x.CreatedAt })
            .HasDatabaseName("IX_OutboxMessages_Processing");

        builder.HasIndex(x => new { x.EventType, x.ProcessedAt, x.DeadLetteredAt, x.CreatedAt })
            .HasDatabaseName("IX_OutboxMessages_EventType_Processing");

        builder.HasIndex(x => new { x.DeadLetteredAt, x.LockedUntil, x.LastAttemptedAt })
            .HasDatabaseName("IX_OutboxMessages_DeadLetter");
    }
}
