using Wallet.Api.Endpoints.Wallet;

namespace Wallet.Api.Endpoints;

public static class EndpointExtensions
{
    private const string ApiPrefix = "/api";
    private const string VersionOnePrefix = "/v1";

    public static void MapWalletEndpoints(this WebApplication app)
    {
        var versionOne = app
            .MapGroup(ApiPrefix)
            .MapGroup(VersionOnePrefix);

        versionOne.MapWalletBusinessRoutes();
        versionOne.MapPromoBusinessRoutes();
    }

    private static void MapWalletBusinessRoutes(this RouteGroupBuilder versionOne)
    {
        const string walletPrefix = "/wallet";

        var wallet = versionOne.MapGroup(walletPrefix);
        wallet.MapTopUpRoutes();
        wallet.MapPaymentRoutes();
        wallet.MapReservationRoutes();
        wallet.MapWalletReportRoutes();
    }

    private static void MapPromoBusinessRoutes(this RouteGroupBuilder versionOne)
    {
        const string promoPrefix = "/promo";
        var promo = versionOne.MapGroup(promoPrefix);

        promo.MapPromoRoutes();
    }
}