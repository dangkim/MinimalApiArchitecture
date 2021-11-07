﻿using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using MinimalApiArchitecture.Api.Data;

namespace MinimalApiArchitecture.Api.IntegrationTests;

public class ApiWebApplication : WebApplicationFactory<MyApplication>
{
    public const string TestConnectionString = "Server=(localdb)\\mssqllocaldb;Database=ApiAngularFromZero_TestDb;Trusted_Connection=True;MultipleActiveResultSets=true";

    protected override IHost CreateHost(IHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            services.AddScoped(sp =>
            {
                // Usamos una LocalDB para pruebas de integración
                return new DbContextOptionsBuilder<ApiDbContext>()
                .UseSqlServer(TestConnectionString)
                .UseApplicationServiceProvider(sp)
                .Options;
            });
        });

        return base.CreateHost(builder);
    }
}