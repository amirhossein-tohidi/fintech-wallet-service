namespace Wallet.IntegrationTests.Infrastructure;

[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class IntegrationTestCollection : ICollectionFixture<WalletIntegrationTestFixture>
{
    public const string Name = "Wallet integration tests";
}
