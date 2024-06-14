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
using Microsoft.Extensions.Caching.Distributed;
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
using System.Text;
using System.Text.Json;

namespace MinimalApiArchitecture.Application.Features.Authentication.Queries;

public class RefreshToken : ICarterModule

{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/refreshToken", (IMediator mediator) =>
        {
            return mediator.Send(new RevokeTokenQuery());
        })
        .WithName(nameof(RefreshToken));
    }

    public class RevokeTokenQuery : IRequest<IResult>
    {
        public string? UserName { get; set; }
    }

    public class RevokeTokenHandler(IDistributedCache cache, IHttpContextAccessor httpContextAccessor, ILogger<RevokeTokenHandler> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        : IRequestHandler<RevokeTokenQuery, IResult>
    {

        public async Task<IResult> Handle(RevokeTokenQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var httpTokenClient = httpClientFactory.CreateClient("SimTokenClient");

                var httpContext = httpContextAccessor.HttpContext!;

                var tokenString = ValidateTokenHelper.ValidateAndExtractToken(httpContext, out IResult? validationResult);

                if (validationResult != null)
                {
                    return validationResult;
                }

                var cacheTokenKey = $"UserToken-{request.UserName}";

                var cachedUserToken = await cache.GetStringAsync(cacheTokenKey, token: cancellationToken);

                var cachedListToken = JsonSerializer.Deserialize<List<string>>(cachedUserToken!);

                foreach (var item in cachedListToken!)
                {
                    var formData = new Dictionary<string, string>
                                        {
                                            { "client_id", configuration["client_id"] },
                                            { "client_secret", configuration["client_secret"] },
                                            { "token", item }
                                        };

                    var content = new FormUrlEncodedContent(formData);

                    // Send a POST request with the HttpContent
                    var response = await httpTokenClient.PostAsync("revoke", content, cancellationToken);
                }

                var responseWithCookies = httpContextAccessor.HttpContext.Response;

                responseWithCookies.Cookies.Delete("stk");

                //string responseData = await response.Content.ReadAsStringAsync(cancellationToken);

                return Results.Ok(true);

            }
            catch (Exception ex)
            {
                logger.LogWarning("Logout: {Message}", ex.InnerException);
                return Results.Problem(ex.InnerException!.Message, "", (int)HttpStatusCode.InternalServerError);
            }
        }
    }

    public class RevokeTokenResponse
    {
        public int CategoryId { get; set; }
        public string? Name { get; set; }
    }
}