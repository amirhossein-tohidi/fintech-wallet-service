using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wallet.Domain.Common;

namespace Wallet.Infrastructure.Persistence.Configurations.Common;

public abstract class BaseEntityConfiguration<TEntity> : IEntityTypeConfiguration<TEntity>
    where TEntity : BaseEntity
{
    public virtual void Configure(EntityTypeBuilder<TEntity> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id)
            .UseHiLo("WalletEntityIds");
        
        builder.Property(x => x.CreatedAt)
            .HasPrecision(3)
            .IsRequired();

        builder.Property(x => x.ModifiedAt)
            .HasPrecision(3);

        ConfigureEntity(builder);
    }

    protected abstract void ConfigureEntity(EntityTypeBuilder<TEntity> builder);
}
