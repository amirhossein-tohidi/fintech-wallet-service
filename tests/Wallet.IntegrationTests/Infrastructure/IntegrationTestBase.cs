using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Wallet.Api.Constants;

namespace Wallet.IntegrationTests.Infrastructure;

[Collection(IntegrationTestCollection.Name)]
public abstract class IntegrationTestBase(WalletIntegrationTestFixture fixture) : IAsyncLifetime
{
    protected static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    protected WalletIntegrationTestFixture Fixture { get; } = fixture;
    protected HttpClient Client { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await Fixture.ResetAsync();
        Client = Fixture.CreateClient();
    }

    public Task DisposeAsync()
    {
        Client.Dispose();
        return Task.CompletedTask;
    }

    protected async Task<HttpResponseMessage> PostAsync<TRequest>(
        string uri,
        TRequest body,
        string? idempotencyKey = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = JsonContent.Create(body, options: JsonOptions)
        };

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            request.Headers.Add(HeaderNames.IdempotencyKey, idempotencyKey);
        }

        return await Client.SendAsync(request);
    }

    protected async Task<HttpResponseMessage> PostAsync(
        string uri,
        string? idempotencyKey = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, uri);

        if (!string.IsNullOrWhiteSpace(idempotencyKey))
        {
            request.Headers.Add(HeaderNames.IdempotencyKey, idempotencyKey);
        }

        return await Client.SendAsync(request);
    }

    protected static async Task<T> ReadRequiredJsonAsync<T>(HttpResponseMessage response)
    {
        var value = await response.Content.ReadFromJsonAsync<T>(JsonOptions);
        Assert.NotNull(value);
        return value;
    }

    protected static async Task AssertStatusAsync(HttpResponseMessage response, HttpStatusCode expectedStatusCode)
    {
        if (response.StatusCode != expectedStatusCode)
        {
            var body = await response.Content.ReadAsStringAsync();
            Assert.Fail($"Expected {(int)expectedStatusCode}, got {(int)response.StatusCode}. Body: {body}");
        }
    }

    protected static async Task WaitUntilAsync(
        Func<Task<bool>> condition,
        TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow.Add(timeout ?? TimeSpan.FromSeconds(20));
        Exception? lastException = null;

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                if (await condition())
                {
                    return;
                }
            }
            catch (Exception ex)
            {
                lastException = ex;
            }

            await Task.Delay(TimeSpan.FromMilliseconds(150));
        }

        Assert.Fail(lastException == null
            ? "Timed out waiting for integration-test condition."
            : $"Timed out waiting for integration-test condition. Last exception: {lastException}");
    }
}
