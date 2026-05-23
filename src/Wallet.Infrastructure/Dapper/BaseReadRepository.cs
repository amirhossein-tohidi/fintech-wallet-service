using Dapper;
using Microsoft.EntityFrameworkCore;
using Wallet.Infrastructure.Persistence;

namespace Wallet.Infrastructure.Dapper;

public abstract class BaseReadRepository(WalletDbContext dbContext)
{
    protected async Task<T?> QuerySingleOrDefaultAsync<T>(
        string sql,
        object? parameters,
        CancellationToken ct)
    {
        var command = CreateCommand(
            sql: sql,
            parameters: parameters,
            ct: ct);

        return await dbContext.Database
            .GetDbConnection()
            .QuerySingleOrDefaultAsync<T>(command);
    }

    protected async Task<IReadOnlyCollection<T>> QueryAsync<T>(
        string sql,
        object? parameters,
        CancellationToken ct)
    {
        var command = CreateCommand(
            sql: sql,
            parameters: parameters,
            ct: ct);

        var rows = await dbContext.Database
            .GetDbConnection()
            .QueryAsync<T>(command);

        return rows.AsList();
    }

    private static CommandDefinition CreateCommand(
        string sql,
        object? parameters,
        CancellationToken ct)
    {
        return new CommandDefinition(
            commandText: sql,
            parameters: parameters,
            cancellationToken: ct);
    }
}
