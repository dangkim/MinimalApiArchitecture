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
using System.Net.Http.Headers;
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

    public class GetTokenQuery : IRequest<IResult>
    {
        public string UserName { get; set; }
        public string Password { get; set; }

    }

    public class GetTokenHandler(IDistributedCache cache, IHttpContextAccessor httpContextAccessor, ILogger<GetTokenHandler> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        : IRequestHandler<GetTokenQuery, IResult>
    {
        public async Task<IResult> Handle(GetTokenQuery request, CancellationToken cancellationToken)
        {
            var cacheTokenKey = $"UserToken-{request.UserName}";

            var cacheOptions = new DistributedCacheEntryOptions()
                                                    .SetSlidingExpiration(TimeSpan.FromDays(366));

            var cachedUserToken = await cache.GetStringAsync(cacheTokenKey, token: cancellationToken);

            byte[] bytes = Convert.FromBase64String(request.Password);
            string decryptedPassword = Encoding.UTF8.GetString(bytes);

            var httpTokenClient = httpClientFactory.CreateClient("SimTokenClient");

            var httpContext = httpContextAccessor.HttpContext!;

            var tokenString = ValidateTokenHelper.ValidateAndExtractToken(httpContext, out IResult? validationResult);

            if (string.IsNullOrEmpty(tokenString))
            {
                using (httpTokenClient)
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

                        using var response = await httpTokenClient.PostAsync("token", content, cancellationToken);

                        // Check if the request was successful
                        if (response.IsSuccessStatusCode)
                        {
                            var responseTokenData = await response.Content.ReadFromJsonAsync<ResponseTokenData>(cancellationToken: cancellationToken);

                            var responseWithCookies = httpContext.Response;

                            var cookieOptions = new CookieOptions
                            {
                                Expires = DateTimeOffset.UtcNow.AddDays(366),
                                Secure = true,
                                HttpOnly = true,
                                SameSite = SameSiteMode.None
                            };

                            responseWithCookies.Cookies.Append("stk", responseTokenData.Access_token, cookieOptions);

                            var tokenList = string.IsNullOrEmpty(cachedUserToken)
                                            ? []
                                            : JsonSerializer.Deserialize<List<string>>(cachedUserToken) ?? [];

                            if (!tokenList.Contains(responseTokenData.Access_token))
                            {
                                tokenList.Add(responseTokenData.Access_token);
                                var serializedTokenList = JsonSerializer.Serialize(tokenList);
                                await cache.SetStringAsync(cacheTokenKey, serializedTokenList, cacheOptions, token: cancellationToken);
                            }

                            return Results.Ok(responseTokenData);
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
                        logger.LogWarning("GetTokenHandler: {Message}", ex.InnerException);
                        return Results.Problem(ex.InnerException!.Message, "", (int)HttpStatusCode.InternalServerError);
                    }
                }
            }

            return Results.Ok(validationResult);
        }

    }

    public class UserProfileResponse
    {
        public decimal Balance { get; set; }
        public string? Email { get; set; }
        public long? UserId { get; set; }
    }

    public class ResponseTokenData
    {
        public string? Access_token { get; set; }
        public string? Token_type { get; set; }
        public long? Expires_in { get; set; }
    }

}