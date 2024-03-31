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
using System.Net.Http.Json;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MinimalApiArchitecture.Application.Features.Authentication.Queries;

public class GetToken : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("api/getToken", (IMediator mediator, GetTokenQuery query) =>
        {
            return mediator.Send(query);
        })
        .WithName(nameof(GetToken));
    }

    public class GetTokenQuery : IRequest<IActionResult>
    {
        public string UserName { get; set; }
        public string Password { get; set; }

    }

    public class GetTokenHandler(ApiDbContext context, ILogger<GetTokenHandler> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        : IRequestHandler<GetTokenQuery, IActionResult>
    {
        public async Task<IActionResult> Handle(GetTokenQuery request, CancellationToken cancellationToken)
        {
            byte[] bytes = Convert.FromBase64String(request.Password);
            string decryptedPassword = Encoding.UTF8.GetString(bytes);

            // Use IHttpClientFactory to create an instance of HttpClient
            var httpClient = httpClientFactory.CreateClient("SimTokenClient");

            // Create an instance of HttpClient
            using (httpClient)
            {
                try
                {
                    var formData = new Dictionary<string, string>
                                        {
                                            { "grant_type", "password" },
                                            { "client_id", configuration["client_id"] },
                                            { "client_secret", configuration["client_secret"] },
                                            { "username", request.UserName },
                                            { "password", decryptedPassword }
                                        };

                    var content = new FormUrlEncodedContent(formData);

                    using var response = await httpClient.PostAsync("token", content, cancellationToken);
                    
                    // Check if the request was successful
                    if (response.IsSuccessStatusCode)
                    {
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
                    logger.LogWarning("GetTokenHandler: {0}", ex.Message);
                    return new StatusCodeResult((int)HttpStatusCode.InternalServerError);
                }
            }

        }

    }

    public class GetTokenResponse
    {
        public int CategoryId { get; set; }
        public string? Name { get; set; }
    }

}