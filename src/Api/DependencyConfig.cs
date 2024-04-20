using MinimalApiArchitecture.Application.Helpers;

namespace MinimalApiArchitecture.Api;

public static class DependencyConfig
{
    public static IServiceCollection AddWebApiConfig(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(name: AppConstants.CorsPolicy,
                builder => { builder.WithOrigins("https://localhost:3000").AllowAnyMethod().AllowAnyHeader().AllowCredentials(); });
        });

        services.AddEndpointsApiExplorer();
        services.AddOpenApiDocument(c =>
        {
            c.Title = "Minimal APIs";
            c.Version = "v1";
        });


        return services;
    }

    public static WebApplication MapSwagger(this WebApplication app)
    {
        app.UseOpenApi();
        app.UseSwaggerUi(settings => { settings.Path = "/api"; });

        return app;
    }
}