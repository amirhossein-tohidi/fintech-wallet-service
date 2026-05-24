using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wallet.Domain.Aggregates;
using Wallet.Infrastructure.Persistence.Configurations.Common;

namespace Wallet.Infrastructure.Persistence.Configurations;

public class ReservationConfiguration: BaseEntityConfiguration<Reservation>
{
    protected override void ConfigureEntity(EntityTypeBuilder<Reservation> builder)
    {
        builder.Property(x => x.Amount)
            .HasPrecision(18, 0);
        
        builder.Property(x => x.Status)
            .IsRequired();

        builder.Property(x => x.ServiceType)
            .IsRequired();

        builder.HasIndex(x => new { x.WalletId, x.ServiceType, x.Status })
            .HasDatabaseName("IX_Reservations_WalletId_ServiceType_Status");

        builder.HasOne<UserWallet>()
            .WithMany(w => w.Reservations)
            .HasForeignKey(x => x.WalletId)
            .HasConstraintName("FK_Reservations_WalletId")
            .IsRequired();
    }
}
