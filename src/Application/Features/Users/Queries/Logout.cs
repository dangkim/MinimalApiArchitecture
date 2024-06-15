using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Identity.Client;
using MinimalApiArchitecture.Application.Helpers;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace MinimalApiArchitecture.Application.Features.Users.Queries;

public class Logout : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapPost("api/logout", (IMediator mediator, LogoutQuery query) =>
        {
            return mediator.Send(query);
        })
        .WithName(nameof(Logout));
    }

    public class LogoutQuery : IRequest<IResult>
    {
        public string? Email { get; set; }
        public long? UserId { get; set; }
    }

    public class LogoutHandler(IDistributedCache cache, IHttpContextAccessor httpContextAccessor, ILogger<LogoutHandler> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        : IRequestHandler<LogoutQuery, IResult>
    {
        public async Task<IResult> Handle(LogoutQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var cacheTokenKey = $"UserToken-{request.Email}";

                var cachedUserToken = await cache.GetStringAsync(cacheTokenKey, token: cancellationToken);

                var httpTokenClient = httpClientFactory.CreateClient("SimTokenClient");

                var httpContext = httpContextAccessor.HttpContext!;

                var tokenString = ValidateTokenHelper.ValidateAndExtractToken(httpContext, out IResult? validationResult);

                if (validationResult != null)
                {
                    return validationResult;
                }

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

                    var content = new FormUrlEncodedContent(formData);

                    await httpTokenClient.PostAsync("revoke", content, cancellationToken);
                }

                tokenList.Clear();
                var serializedTokenList = JsonSerializer.Serialize(tokenList);
                await cache.SetStringAsync(cacheTokenKey, serializedTokenList, cancellationToken);

                var responseWithCookies = httpContextAccessor.HttpContext.Response;

                responseWithCookies.Cookies.Delete("stk");

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