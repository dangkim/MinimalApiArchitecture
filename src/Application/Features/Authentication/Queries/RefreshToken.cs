using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MinimalApiArchitecture.Application.Helpers;
using MinimalApiArchitecture.Application.Model;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace MinimalApiArchitecture.Application.Features.Authentication.Queries;

public class RefreshToken : ICarterModule

{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("api/refreshToken", (IMediator mediator, RevokeTokenQuery query) =>
        {
            return mediator.Send(query);
        })
        .WithName(nameof(RefreshToken));
    }

    public class RevokeTokenQuery : IRequest<IResult>
    {
        public string? Email { get; set; }
    }

    public class RevokeTokenHandler(IDistributedCache cache, IHttpContextAccessor httpContextAccessor, ILogger<RevokeTokenHandler> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        : IRequestHandler<RevokeTokenQuery, IResult>
    {

        public async Task<IResult> Handle(RevokeTokenQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var cacheTokenKey = $"UserToken-{request.Email}";

                var cachedUserToken = await cache.GetStringAsync(cacheTokenKey, token: cancellationToken);

                var cacheOptions = new DistributedCacheEntryOptions()
                                                    .SetSlidingExpiration(TimeSpan.FromDays(366));

                var httpTokenClient = httpClientFactory.CreateClient("SimTokenClient");

                var httpContext = httpContextAccessor.HttpContext!;

                var tokenString = ValidateTokenHelper.ValidateAndExtractToken(httpContext, out IResult? validationResult);

                if (validationResult != null)
                {
                    return validationResult;
                }

                // RefreshToken
                var formDataRefreshToken = new Dictionary<string, string>
                                        {
                                            { "grant_type", "refresh_token" },
                                            { "client_id", configuration["client_id"] },
                                            { "client_secret", configuration["client_secret"] },
                                            { "refresh_token", tokenString }
                                        };

                var content = new FormUrlEncodedContent(formDataRefreshToken);

                using var responseTokenData = await httpTokenClient.PostAsync("token", content, cancellationToken);

                var responeRefreshToken = await responseTokenData.Content.ReadFromJsonAsync<ResponseTokenData>(cancellationToken: cancellationToken);

                // Remove old token
                var tokenList = string.IsNullOrEmpty(cachedUserToken)
                                            ? []
                                            : JsonSerializer.Deserialize<List<string>>(cachedUserToken) ?? [];

                if (!tokenList.Contains(tokenString))
                {
                    tokenList.Add(tokenString);
                }

                foreach (var token in tokenList)
                {
                    var formData = new Dictionary<string, string>
                                        {
                                            { "client_id", configuration["client_id"] },
                                            { "client_secret", configuration["client_secret"] },
                                            { "token", token }
                                        };

                    var contentRemoveToken = new FormUrlEncodedContent(formData);

                    await httpTokenClient.PostAsync("revoke", content, cancellationToken);
                }

                tokenList.Clear();
                tokenList.Add(responeRefreshToken.Access_token);
                var serializedTokenList = JsonSerializer.Serialize(tokenList);
                await cache.SetStringAsync(cacheTokenKey, serializedTokenList, cacheOptions, cancellationToken);

                return Results.Ok(true);

            }
            catch (Exception ex)
            {
                logger.LogWarning("Logout: {Message}", ex.InnerException);
                return Results.Problem(ex.InnerException!.Message, "", (int)HttpStatusCode.InternalServerError);
            }
        }
    }
}