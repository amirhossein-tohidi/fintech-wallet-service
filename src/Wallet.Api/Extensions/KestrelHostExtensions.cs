namespace Wallet.Api.Extensions;

public static class KestrelHostExtensions
{
    public static ConfigureWebHostBuilder ConfigureApiKestrel(this ConfigureWebHostBuilder webHost)
    {
        webHost.ConfigureKestrel((context, options) =>
        {
            options.Configure(context.Configuration.GetSection("Kestrel"));
        });

        return webHost;
    }
}
