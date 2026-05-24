using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wallet.Domain.Aggregates;
using Wallet.Infrastructure.Persistence.Configurations.Common;

namespace Wallet.Infrastructure.Persistence.Configurations;

public class LedgerTransactionConfiguration : BaseEntityConfiguration<LedgerTransaction>
{
    protected override void ConfigureEntity(EntityTypeBuilder<LedgerTransaction> builder)
    {
        builder.Property(x => x.IdempotencyKey)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(x => x.ServiceType)
            .IsRequired();
        
        // Unique Index
        builder.HasIndex(x => x.IdempotencyKey)
            .HasDatabaseName("UIX_LedgerTransactions_IdempotencyKey")
            .IsUnique();

        builder.HasIndex(x => new { x.WalletId, x.ServiceType, x.CreatedAt })
            .HasDatabaseName("IX_LedgerTransactions_WalletId_ServiceType_CreatedAt");
        
        builder.Property(x => x.Amount)
            .HasPrecision(18, 0);

        // Relation
        builder.HasMany(x => x.Entries)
            .WithOne()
            .HasForeignKey(x => x.TransactionId)
            .HasConstraintName("FK_LedgerEntries_TransactionId")
            .IsRequired();
    }
}
