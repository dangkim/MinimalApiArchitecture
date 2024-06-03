using AutoMapper;
using AutoMapper.QueryableExtensions;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MinimalApiArchitecture.Application.Domain.Entities;
using MinimalApiArchitecture.Application.Features.Products.EventHandlers;
using MinimalApiArchitecture.Application.Infrastructure.Persistence;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace MinimalApiArchitecture.Application.Features.Authentication.Queries;

public class RevokeToken : ICarterModule

{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/revokeToken", (IMediator mediator) =>
        {
            //var httpClientFactory = app.ServiceProvider.GetRequiredService<IHttpClientFactory>();
            //var httpClient = httpClientFactory.CreateClient("SimTokenClient");

            return mediator.Send(new RevokeTokenQuery());
        })
        .WithName(nameof(RevokeToken));
    }

    public class RevokeTokenQuery : IRequest<IActionResult>
    {

    }

    public class RevokeTokenHandler(ApiDbContext context, ILogger<RevokeTokenHandler> logger, IHttpClientFactory httpClientFactory)
        : IRequestHandler<RevokeTokenQuery, IActionResult>
    {

        public async Task<IActionResult> Handle(RevokeTokenQuery request, CancellationToken cancellationToken)
        {
            // Use IHttpClientFactory to create an instance of HttpClient
            var httpClient = httpClientFactory.CreateClient("SimTokenClient");

            // Create an instance of the request body
            var requestBody = new
            {
                client_Id = "your_client_id",
                token = "your_token"
            };

            // Serialize the request body to JSON
            var requestBodyJson = JsonSerializer.Serialize(requestBody);

            // Create an instance of HttpClient
            using (httpClient)
            {
                try
                {
                    // Create an instance of HttpContent with the serialized request body
                    var httpContent = new StringContent(requestBodyJson, Encoding.UTF8, "application/json");

                    // Send a POST request with the HttpContent
                    HttpResponseMessage response = await httpClient.PostAsync("revoke", httpContent, cancellationToken);

                    // Check if the request was successful
                    if (response.IsSuccessStatusCode)
                    {
                        // Read the response content as a string
                        string responseData = await response.Content.ReadAsStringAsync(cancellationToken);

                        return new OkObjectResult(responseData);
                    }
                    else
                    {
                        return new StatusCodeResult((int)response.StatusCode);                       
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning("RevokeTokenHandler: {Message}", ex.InnerException);
                    return new StatusCodeResult((int)HttpStatusCode.InternalServerError);
                }
            }            
            
        }

    }

    public class RevokeTokenResponse
    {
        public int CategoryId { get; set; }
        public string? Name { get; set; }
    }
}