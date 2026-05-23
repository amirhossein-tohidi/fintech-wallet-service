using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wallet.Infrastructure.Persistence.Messaging;

namespace Wallet.Infrastructure.Persistence.Configurations;

public class InboxMessageConfiguration : IEntityTypeConfiguration<InboxMessage>
{
    public void Configure(EntityTypeBuilder<InboxMessage> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.EventType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(x => x.Error)
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.Payload)
            .IsRequired()
            .HasColumnType("nvarchar(max)");

        builder.Property(x => x.LockedBy)
            .HasMaxLength(200);

        builder.HasIndex(x => new { x.ProcessedAt, x.LockedUntil, x.ReceivedAt })
            .HasDatabaseName("IX_InboxMessages_Processing");

        builder.HasIndex(x => new { x.EventType, x.ProcessedAt, x.DeadLetteredAt, x.ReceivedAt })
            .HasDatabaseName("IX_InboxMessages_EventType_Processing");

        builder.HasIndex(x => new { x.DeadLetteredAt, x.LockedUntil, x.LastAttemptedAt })
            .HasDatabaseName("IX_InboxMessages_DeadLetter");
    }
}
