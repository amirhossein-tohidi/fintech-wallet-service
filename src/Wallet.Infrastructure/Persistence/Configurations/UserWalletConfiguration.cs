using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wallet.Domain.Aggregates;
using Wallet.Infrastructure.Persistence.Configurations.Common;

namespace Wallet.Infrastructure.Persistence.Configurations;

public class UserWalletConfiguration: AggregateConfiguration<UserWallet>
{
    protected override void ConfigureEntity(EntityTypeBuilder<UserWallet> builder)
    {
        builder.HasIndex(x => x.UserId)
            .HasDatabaseName("UIX_UserWallets_UserId")
            .IsUnique();
        
        builder.Property(x => x.AvailableBalance)
            .HasPrecision(18, 0);
        
        builder.Property(x => x.ReservedBalance)
            .HasPrecision(18, 0);

        builder.HasMany(x => x.Reservations)
            .WithOne()
            .HasForeignKey("WalletId")
            .HasConstraintName("FK_Reservations_WalletId")
            .IsRequired();
        
        builder.HasMany(x => x.LedgerTransactions)
            .WithOne()
            .HasForeignKey("WalletId")
            .HasConstraintName("FK_LedgerTransactions_WalletId")
            .IsRequired();
    }
}
