using Wallet.Application.Abstractions;

namespace Wallet.Api.Common;

public class HttpContextRouteInfo(IHttpContextAccessor httpContextAccessor) : IRouteInfo
{
    private readonly HttpContext _httpContext = httpContextAccessor?.HttpContext ?? throw new InvalidOperationException("HttpContext is not available.");

    public string HttpMethod => _httpContext.Request.Method.ToUpperInvariant();
    public string Path => _httpContext.Request.Path.ToString();
}