using MinimalApiArchitecture.Application;
using MinimalApiArchitecture.Application.Helpers;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace MinimalApiArchitecture.Api;
public partial class TokenValidationMiddleware(RequestDelegate next, ILogger<TokenValidationMiddleware> logger)
{
    private readonly RequestDelegate _next = next;
    private readonly ILogger<TokenValidationMiddleware> _logger = logger;

    private readonly List<Regex> _whitelistedPathPatterns =
    [
        GetStableProducts(),
        GetStableCountries()
    ];

    public async Task InvokeAsync(HttpContext context)
    {
        var path = context.Request.Path.Value;

        if (_whitelistedPathPatterns.Exists(pattern => pattern.IsMatch(path!)))
        {
            await _next(context);
            return;
        }

        var tokenString = ExtractToken.GetToken(context);
        
        if (string.IsNullOrEmpty(tokenString))
        {
            context.Response.StatusCode = (int)HttpStatusCode.Unauthorized;
            await context.Response.WriteAsync("Unauthorized");
            return;
        }

        try
        {
            context.Items["Token"] = tokenString; // Store the token object for later use
            await _next(context);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning("TokenValidationMiddleware failed: {Message}", ex.Message);
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            await context.Response.WriteAsync("Internal Server Error");
        }
    }

    [GeneratedRegex(@"^/api/getstableproducts/.*$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex GetStableProducts();

    [GeneratedRegex(@"^/api/getstablecountries.*$", RegexOptions.IgnoreCase | RegexOptions.Compiled, "en-US")]
    private static partial Regex GetStableCountries();

}
