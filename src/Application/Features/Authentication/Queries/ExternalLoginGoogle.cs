using AutoMapper;
using AutoMapper.QueryableExtensions;
using Azure;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MinimalApiArchitecture.Application.Infrastructure.Persistence;

namespace MinimalApiArchitecture.Application.Features.Authentication.Queries;

public class ExternalLoginGoogle : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("api/externalLoginGoogle", (IMediator mediator, ExternalLoginGoogleQuery query) =>
        {
            return mediator.Send(query);
        })
        .WithName(nameof(ExternalLoginGoogle));
    }

    public class ExternalLoginGoogleQuery : IRequest<IActionResult>
    {
        public string? Provider { get; set; }
    }

    public class ExternalLoginGoogleRegisterHandler(ApiDbContext context, ILogger<ExternalLoginGoogleRegisterHandler> logger, IHttpClientFactory httpClientFactory)
        : IRequestHandler<ExternalLoginGoogleQuery, IActionResult>
    {
        public async Task<IActionResult> Handle(ExternalLoginGoogleQuery request, CancellationToken cancellationToken)
        {            
            // Use IHttpClientFactory to create an instance of HttpClient
            var httpClient = httpClientFactory.CreateClient("SimApiClient");           

            // Create an instance of HttpClient
            using (httpClient)
            {
                try
                {
                    // Create an instance of HttpContent with the serialized request body
                    // var httpContent = new StringContent(requestBodyJson, Encoding.UTF8, "application/json");

                    // Send a POST request with the HttpContent
                    var response = await httpClient.GetAsync("ExternalLoginCallback", cancellationToken);

                    // Check if the request was successful
                    if (response.IsSuccessStatusCode)
                    {
                        // Read the response content as a string
                        
                        string responseData = await response.Content.ReadAsStringAsync(cancellationToken);

                        return new OkObjectResult(responseData);
                    }
                    else
                    {
                        return new NotFoundResult();
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning("ExternalLoginGoogleRegisterHandler: {0}", ex.Message);
                    return new NotFoundResult();
                }
            }

        }

    }

    //public class ExternalLoginGoogleRegisterResponse
    //{
    //    public int CategoryId { get; set; }
    //    public string? Name { get; set; }
    //}

}