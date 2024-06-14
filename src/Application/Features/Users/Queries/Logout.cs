using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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
        app.MapGet("api/logout", (IMediator mediator) =>
        {
            return mediator.Send(new LogoutQuery { });
        })
        .WithName(nameof(Logout));
    }

    public class LogoutQuery : IRequest<IResult>
    {
    }

    public class LogoutHandler(IHttpContextAccessor httpContextAccessor, ILogger<LogoutHandler> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        : IRequestHandler<LogoutQuery, IResult>
    {
        public async Task<IResult> Handle(LogoutQuery request, CancellationToken cancellationToken)
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

                var formData = new Dictionary<string, string>
                                        {
                                            { "client_id", configuration["client_id"] },
                                            { "client_secret", configuration["client_secret"] },
                                            { "token", tokenString }
                                        };

                var content = new FormUrlEncodedContent(formData);

                // Send a POST request with the HttpContent
                HttpResponseMessage response = await httpTokenClient.PostAsync("revoke", content, cancellationToken);

                var responseWithCookies = httpContextAccessor.HttpContext.Response;

                responseWithCookies.Cookies.Delete("stk");

                string responseData = await response.Content.ReadAsStringAsync(cancellationToken);

                return Results.Ok(responseData);

            }
            catch (Exception ex)
            {
                logger.LogWarning("Logout: {Message}", ex.InnerException);
                return Results.Problem(ex.InnerException!.Message, "", (int)HttpStatusCode.InternalServerError);
            }

        }

    }
}