using Carter;
using Microsoft.AspNetCore.Authentication;
using MinimalApiArchitecture.Api;
using MinimalApiArchitecture.Api.Extensions;
using MinimalApiArchitecture.Application;
using MinimalApiArchitecture.Application.Helpers;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Net;
using System.IO;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

builder.Host.AddSerilog();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient("SimTokenClient", client =>
{
    client.BaseAddress = new Uri("https://openapi.thuesimao.com/connect/");
});

builder.Services.AddHttpClient("SimApiClient", client =>
{
    client.BaseAddress = new Uri("https://openapi.thuesimao.com/api/content/");
});

builder.Services.AddHttpClient("SimGraphClient", client =>
{
    client.BaseAddress = new Uri("https://openapi.thuesimao.com/api/graphql");
});

builder.Services.Configure<CookiePolicyOptions>(options =>
{
    options.MinimumSameSitePolicy = SameSiteMode.Lax;
    options.OnAppendCookie = cookieContext =>
        CheckSameSiteBackwardsCompatibility(cookieContext.Context, cookieContext.CookieOptions);
    options.OnDeleteCookie = cookieContext =>
        CheckSameSiteBackwardsCompatibility(cookieContext.Context, cookieContext.CookieOptions);
});

builder.Services.AddWebApiConfig();
builder.Services.AddApplicationCore();
builder.Services.AddPersistence(builder.Configuration);
builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = "38.242.150.60:6379";
});

var app = builder.Build();
app.UseCors(AppConstants.CorsPolicy);
app.UseStaticFiles();
app.MapSwagger();
app.MapCarter();
app.UseCookiePolicy();
app.Run();

// SameSite cookie compatibility check
void CheckSameSiteBackwardsCompatibility(HttpContext httpContext, CookieOptions options)
{
    if (options.SameSite == SameSiteMode.None)
    {
        var userAgent = httpContext.Request.Headers["User-Agent"].ToString();
        if (DisallowsSameSiteNone(userAgent))
        {
            options.SameSite = SameSiteMode.Unspecified;
        }
    }
}

bool DisallowsSameSiteNone(string userAgent)
{
    // Check for various user agents that don't support SameSite=None
    // You can customize this function as needed.
    if (string.IsNullOrEmpty(userAgent))
        return false;

    if (userAgent.Contains("CPU iPhone OS 12") || userAgent.Contains("iPad; CPU OS 12"))
        return true;

    if (userAgent.Contains("Macintosh; Intel Mac OS X 10_14") && userAgent.Contains("Version/"))
        return true;

    if (userAgent.Contains("Chrome/5") || userAgent.Contains("Chrome/6"))
        return true;

    return false;
}