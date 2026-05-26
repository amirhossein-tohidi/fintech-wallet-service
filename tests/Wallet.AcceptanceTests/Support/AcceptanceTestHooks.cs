using Reqnroll;
using Wallet.IntegrationTests.Infrastructure;

namespace Wallet.AcceptanceTests.Support;

[Binding]
public static class AcceptanceTestHooks
{
    private static readonly SemaphoreSlim FixtureLock = new(1, 1);

    public static WalletIntegrationTestFixture Fixture { get; } = new();

    [BeforeTestRun]
    public static async Task BeforeTestRun()
    {
        await Fixture.InitializeAsync();
    }

    [AfterTestRun]
    public static async Task AfterTestRun()
    {
        await Fixture.DisposeAsync();
    }

    [BeforeScenario]
    public static async Task BeforeScenario()
    {
        await FixtureLock.WaitAsync();
        await Fixture.ResetAsync();
    }

    [AfterScenario]
    public static void AfterScenario()
    {
        FixtureLock.Release();
    }
}
