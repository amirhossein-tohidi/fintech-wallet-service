using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wallet.Domain.Common;

namespace Wallet.Infrastructure.Persistence.Configurations.Common;

public abstract class AggregateConfiguration<TEntity> : BaseEntityConfiguration<TEntity>
    where TEntity : AggregateRoot
{
    public override void Configure(EntityTypeBuilder<TEntity> builder)
    {
        base.Configure(builder);

        builder.Property(x => x.RowVersion)
            .IsRowVersion();
    }
}