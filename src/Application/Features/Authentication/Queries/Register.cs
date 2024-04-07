using AutoMapper;
using AutoMapper.QueryableExtensions;
using Azure;
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

public class Register : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("api/register", (IMediator mediator, RegisterQuery query) =>
        {
            return mediator.Send(query);
        })
        .WithName(nameof(Register));
    }

    public class RegisterQuery : IRequest<IResult>
    {
        public string UserName { get; set; }
        public string Password { get; set; }
        public string ConfirmPassword { get; set; }

    }

    public class RegisterHandler(ApiDbContext context, ILogger<RegisterHandler> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        : IRequestHandler<RegisterQuery, IResult>
    {
        public async Task<IResult> Handle(RegisterQuery request, CancellationToken cancellationToken)
        {
            byte[] bytes = Convert.FromBase64String(request.Password);
            byte[] bytesConfirm = Convert.FromBase64String(request.ConfirmPassword);
            string decryptedPassword = Encoding.UTF8.GetString(bytes);
            string decryptedConfirmPassword = Encoding.UTF8.GetString(bytesConfirm);

            // Use IHttpClientFactory to create an instance of HttpClient
            var httpClient = httpClientFactory.CreateClient("SimApiClient");

            // Create an instance of the request body
            var requestBody = new
            {
                email = request.UserName,
                password = decryptedPassword,
                confirmPassword = decryptedConfirmPassword
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
                    HttpResponseMessage response = await httpClient.PostAsync("SimRegister", httpContent, cancellationToken);

                    // Check if the request was successful
                    if (response.IsSuccessStatusCode)
                    {
                        // Read the response content as a string
                        string responseData = await response.Content.ReadAsStringAsync(cancellationToken);

                        return Results.Ok(responseData);
                    }
                    else
                    {
                        string responseData = await response.Content.ReadAsStringAsync(cancellationToken);
                        var responseObject = JsonSerializer.Deserialize<ProblemDetails>(responseData);
                        return Results.Problem(responseObject!);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogWarning("RegisterHandler: {0}", ex.Message);
                    return Results.Problem(ex.Message, "", (int)HttpStatusCode.InternalServerError);
                }
            }

        }

    }

    public class RegisterResponse
    {
        public int CategoryId { get; set; }
        public string? Name { get; set; }
    }

}