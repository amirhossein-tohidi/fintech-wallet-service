using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wallet.Domain.Aggregates;
using Wallet.Infrastructure.Persistence.Configurations.Common;

namespace Wallet.Infrastructure.Persistence.Configurations;

public class PromoGrantConfiguration : BaseEntityConfiguration<PromoGrant>
{
    protected override void ConfigureEntity(EntityTypeBuilder<PromoGrant> builder)
    {
        builder.ToTable("PromoGrants");

        builder.Property(x => x.Amount)
            .HasPrecision(18, 0);

        builder.Property(x => x.RemainingAmount)
            .HasPrecision(18, 0);

        builder.Property(x => x.ServiceType)
            .IsRequired();

        builder.HasIndex(x => new { x.WalletId, x.ServiceType, x.ExpiresAt })
            .HasDatabaseName("IX_PromoGrants_WalletId_ServiceType_ExpiresAt");

        builder.HasOne<UserWallet>()
            .WithMany(w => w.PromoGrants)
            .HasForeignKey(x => x.WalletId)
            .HasConstraintName("FK_PromoGrants_WalletId")
            .IsRequired();

        builder.Ignore(x => x.ConsumedAmount);
    }
}
