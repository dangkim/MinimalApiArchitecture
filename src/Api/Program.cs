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

var builder = WebApplication.CreateBuilder(args);

builder.Host.AddSerilog();
builder.Services.AddHttpContextAccessor();
builder.Services.AddHttpClient("SimTokenClient", client =>
{
    client.BaseAddress = new Uri("https://localhost:44300/connect/");
});

builder.Services.AddHttpClient("SimApiClient", client =>
{
    client.BaseAddress = new Uri("https://localhost:44300/api/content/");
});

builder.Services.AddWebApiConfig();
builder.Services.AddApplicationCore();
builder.Services.AddPersistence(builder.Configuration);

var app = builder.Build();
app.UseCors(AppConstants.CorsPolicy);
app.UseStaticFiles();
app.MapSwagger();
app.MapCarter();
app.Run();