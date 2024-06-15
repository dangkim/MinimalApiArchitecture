using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MinimalApiArchitecture.Application.Infrastructure.Persistence;
using System.Net;
using System.Text.Json;
using System.Text;
using System.Net.Http.Json;
using MinimalApiArchitecture.Application.Model;

namespace MinimalApiArchitecture.Application.Features.Authentication.Queries;

public class ExternalLoginGoogle : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/externalLoginGoogle", (IMediator mediator, IHttpContextAccessor httpContextAccessor) =>
        {
            var httpContext = httpContextAccessor.HttpContext;
            return mediator.Send(new ExternalLoginGoogleQuery { Sign = httpContext.Request.Query["sign"] });
        })
        .WithName(nameof(ExternalLoginGoogle));
    }

    public class ExternalLoginGoogleQuery : IRequest<IResult>
    {
        public string? Sign { get; set; }
    }

    public class ExternalLoginGoogleRegisterHandler(IHttpContextAccessor httpContextAccessor, ApiDbContext context, ILogger<ExternalLoginGoogleRegisterHandler> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        : IRequestHandler<ExternalLoginGoogleQuery, IResult>
    {
        public async Task<IResult> Handle(ExternalLoginGoogleQuery request, CancellationToken cancellationToken)
        {
            // Use IHttpClientFactory to create an instance of HttpClient
            var httpClient = httpClientFactory.CreateClient("SimTokenClient");
            byte[] bytes = Convert.FromBase64String(request.Sign!);
            string userName = Encoding.UTF8.GetString(bytes);

            // Create an instance of HttpClient
            using (httpClient)
            {
                try
                {
                    var formData = new Dictionary<string, string>
                                        {
                                            { "grant_type", "password" },
                                            { "client_id", configuration["client_id"]! },
                                            { "client_secret", configuration["client_secret"]! },
                                            { "username", userName },
                                            { "password", userName+ "A@" }
                                        };

                    var content = new FormUrlEncodedContent(formData);

                    using var response = await httpClient.PostAsync("token", content, cancellationToken);

                    // Check if the request was successful
                    if (response.IsSuccessStatusCode)
                    {
                        var responseData = await response.Content.ReadFromJsonAsync<ResponseTokenData>(cancellationToken: cancellationToken);//.ReadAsStringAsync(cancellationToken);

                        var responseWithCookies = httpContextAccessor.HttpContext.Response;

                        var cookieOptions = new CookieOptions
                        {
                            Expires = DateTimeOffset.UtcNow.AddDays(366),
                            Secure = true,
                            HttpOnly = true
                        };

                        responseWithCookies.Cookies.Append("stk", responseData.Access_token, cookieOptions);

                        return Results.Redirect(configuration["simfrontendurl"]!);
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

    }
}