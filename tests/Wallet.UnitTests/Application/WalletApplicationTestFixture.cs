using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Wallet.Application.Mapping;
using Wallet.Infrastructure.Persistence;

namespace Wallet.UnitTests.Application;

internal static class WalletApplicationTestFixture
{
    public static IMapper CreateMapper()
    {
        var configuration = new MapperConfiguration(
            configure: cfg => cfg.AddProfile<WalletMappingProfile>(),
            loggerFactory: NullLoggerFactory.Instance);
        configuration.AssertConfigurationIsValid();
        return configuration.CreateMapper();
    }

    public static WalletDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<WalletDbContext>()
            .UseInMemoryDatabase($"wallet-unit-tests-{Guid.NewGuid():N}")
            .Options;

        return new WalletDbContext(options);
    }
}
