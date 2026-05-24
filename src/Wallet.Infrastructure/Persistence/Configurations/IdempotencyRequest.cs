using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wallet.Domain.Idempotency;
using Wallet.Infrastructure.Persistence.Configurations.Common;

namespace Wallet.Infrastructure.Persistence.Configurations;

public class IdempotencyRequestConfiguration : BaseEntityConfiguration<IdempotencyRequest>
{
    protected override void ConfigureEntity(EntityTypeBuilder<IdempotencyRequest> builder)
    {
        builder.Property(x => x.Key)
            .IsRequired()
            .HasMaxLength(255);
        
        // Unique Index
        builder.HasIndex(x => x.Key)
            .HasDatabaseName("UIX_IdempotencyRequests_Key")
            .IsUnique();
        
        builder.Property(x => x.ResponseBody)
            .HasColumnType("nvarchar(max)");
    }
}