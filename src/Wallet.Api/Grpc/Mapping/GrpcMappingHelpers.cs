using System.Globalization;
using Google.Protobuf.WellKnownTypes;

namespace Wallet.Api.Grpc.Mapping;

internal static class GrpcMappingHelpers
{
    public static string FormatDecimal(decimal value)
    {
        return value.ToString(CultureInfo.InvariantCulture);
    }

    public static Timestamp ToTimestamp(DateTime value)
    {
        var utcValue = value.Kind switch
        {
            DateTimeKind.Utc => value,
            DateTimeKind.Local => value.ToUniversalTime(),
            _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
        };

        return Timestamp.FromDateTime(utcValue);
    }
}
