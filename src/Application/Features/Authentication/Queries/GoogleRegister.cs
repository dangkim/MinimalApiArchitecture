using AutoMapper;
using AutoMapper.QueryableExtensions;
using Azure;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
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
using System.Security.Claims;
using System.Text;
using System.Text.Json;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace MinimalApiArchitecture.Application.Features.Authentication.Queries;

public class GoogleRegister : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("api/signin-google", (IMediator mediator) =>
        {
            return mediator.Send(new GoogleRegisterQuery());
        })
        .WithName(nameof(GoogleRegister));
    }

    public class GoogleRegisterQuery : IRequest<IResult>
    {
        //public string? Provider { get; set; }
    }

    public class GoogleRegisterHandler(IHttpContextAccessor httpContextAccessor, ApiDbContext context, ILogger<GoogleRegisterHandler> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        : IRequestHandler<GoogleRegisterQuery, IResult>
    {
        public async Task<IResult> Handle(GoogleRegisterQuery request, CancellationToken cancellationToken)
        {
            var httpContext = httpContextAccessor.HttpContext;
            var authResult = await httpContext!.AuthenticateAsync(GoogleDefaults.AuthenticationScheme);

            try
            {
                if (authResult.Succeeded)
                {
                    // Retrieve user's email
                    var email = authResult.Principal.FindFirst(ClaimTypes.Email)?.Value;

                    // Authentication succeeded, redirect to home page or any other secure page
                    httpContext.Response.Redirect("/");
                    return Results.Ok();
                }

                return Results.Problem(authResult.Failure.Message, "", (int)HttpStatusCode.Unauthorized);
            }
            catch (Exception ex)
            {
                logger.LogWarning("GetTokenHandler: {0}", ex.Message);
                return Results.Problem(ex.Message, "", (int)HttpStatusCode.InternalServerError);
            }
        }

    }

    public class GoogleRegisterResponse
    {
        public int CategoryId { get; set; }
        public string? Name { get; set; }
    }

}