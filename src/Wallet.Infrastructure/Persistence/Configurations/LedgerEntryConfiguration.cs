using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wallet.Domain.Aggregates;
using Wallet.Infrastructure.Persistence.Configurations.Common;

namespace Wallet.Infrastructure.Persistence.Configurations;

public class LedgerEntryConfiguration : BaseEntityConfiguration<LedgerEntry>
{
    protected override void ConfigureEntity(EntityTypeBuilder<LedgerEntry> builder)
    {
        builder.Property(x => x.Amount)
            .HasPrecision(18, 0);
        
        builder.HasOne<LedgerTransaction>()
            .WithMany(t => t.Entries)
            .HasForeignKey(e => e.TransactionId)
            .HasConstraintName("FK_LedgerEntries_TransactionId")
            .IsRequired();
    }
}