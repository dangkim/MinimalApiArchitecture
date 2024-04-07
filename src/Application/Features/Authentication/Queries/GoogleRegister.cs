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
using MinimalApiArchitecture.Application.Domain.Entities;
using MinimalApiArchitecture.Application.Features.Products.EventHandlers;
using MinimalApiArchitecture.Application.Helpers;
using MinimalApiArchitecture.Application.Infrastructure.Persistence;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MinimalApiArchitecture.Application.Features.Authentication.Queries;

public class GoogleRegister : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("api/externalLogin", (IMediator mediator) =>
        {
            return mediator.Send(new GoogleRegisterQuery());
        })
        .WithName(nameof(GoogleRegister));
    }

    public class GoogleRegisterQuery : IRequest<IActionResult>
    {
        //public string? Provider { get; set; }
    }

    public class GoogleRegisterHandler(IHttpContextAccessor httpContextAccessor, ApiDbContext context, ILogger<GoogleRegisterHandler> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        : IRequestHandler<GoogleRegisterQuery, IActionResult>
    {
        public async Task<IActionResult> Handle(GoogleRegisterQuery request, CancellationToken cancellationToken)
        {            
            // Use IHttpClientFactory to create an instance of HttpClient
            var httpClient = httpClientFactory.CreateClient("SimApiClient");

            // Create an instance of the request body
            var requestBody = new
            {                
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
                    var response = await httpClient.PostAsync("externalLoginGoogle", httpContent, cancellationToken);

                    // Check if the request was successful
                    if (response.IsSuccessStatusCode)
                    {
                        // Read the response content as a string
                        
                        string responseData = await response.Content.ReadAsStringAsync(cancellationToken);

                        // Deserialize the JSON content using JsonSerializer
                        var challengeResult = JsonSerializer.Deserialize<ChallengeResult>(responseData);
                        
                        var httpContext = httpContextAccessor.HttpContext;

                        //httpContext!.Response.Headers.Location = properties!.RedirectUri;
                        //httpContext.Response.StatusCode = StatusCodes.Status302Found;

                        // Get the content from the response
                        //var content = await response.Content.ReadAsStringAsync();

                        // Set the content type for the response
                        //httpContext.Response.ContentType = "text/html";

                        // Return the content directly to the client
                        //await httpContext.Response.WriteAsync(content);

                        //var redirectUrl = response.RequestMessage.RequestUri.OriginalString;

                        // Redirect the client to the external authentication provider
                        //return Results.Redirect("https://localhost:44300/api/content/ExternalLoginGoogle");

                        //await httpContext.ChallengeAsync("Google", properties);

                        //return Results.Empty;
                        return challengeResult!;
                    }
                    else
                    {
                        //var redirectUrl = response.Headers.Location.AbsoluteUri;

                        // Redirect the client to the external authentication provider
                        //return new RedirectResult(redirectUrl);
                        string responseData = await response.Content.ReadAsStringAsync(cancellationToken);
                        //var responseObject = JsonSerializer.Deserialize<ProblemDetails>(responseData);
                        return new ObjectResult(responseData);
                    }

                    //return new EmptyResult();
                }
                catch (Exception ex)
                {
                    logger.LogWarning("GoogleRegisterHandler: {0}", ex.Message);
                    return new ObjectResult(ex.Message);
                    //return Results.Problem(ex.Message, "", (int)HttpStatusCode.InternalServerError);
                }
            }

        }

    }

    public class GoogleRegisterResponse
    {
        public int CategoryId { get; set; }
        public string? Name { get; set; }
    }

}