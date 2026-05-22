using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Wallet.Application.Abstractions;

namespace Wallet.Application.Common;

public class RequestHasher : IRequestHasher
{
    public string ComputeHash<T>(T request)
    {
        if (request == null)
        {
            return string.Empty;
        }

        var jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = false,
        };
        var jsonString = JsonSerializer.Serialize(request, jsonOptions);

        using var sha256 = SHA256.Create();
        var bytes = Encoding.UTF8.GetBytes(jsonString);
        var hashBytes = sha256.ComputeHash(bytes);
        return Convert.ToBase64String(hashBytes);
    }
}