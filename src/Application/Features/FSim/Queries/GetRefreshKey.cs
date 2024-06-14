using Azure.Core;
using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
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

namespace MinimalApiArchitecture.Application.Features.Authentication.Queries;

public class GetRefreshKey : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/getrefreshkey", (IMediator mediator) =>
        {
            return mediator.Send(new GetRefreshKeyQuery { });
        })
        .WithName(nameof(GetRefreshKey));
    }

    public class GetRefreshKeyQuery : IRequest<IResult>
    {
    }

    public class GetRefreshKeyHandler(IHttpContextAccessor httpContextAccessor, ILogger<GetRefreshKeyHandler> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        : IRequestHandler<GetRefreshKeyQuery, IResult>
    {
        public async Task<IResult> Handle(GetRefreshKeyQuery request, CancellationToken cancellationToken)
        {
            // Use IHttpClientFactory to create an instance of HttpClient
            var httpClient = httpClientFactory.CreateClient("SimTokenClient");

            // Create an instance of HttpClient
            using (httpClient)
            {
                try
                {
                    var httpContext = httpContextAccessor.HttpContext!;

                    var tokenString = ValidateTokenHelper.ValidateAndExtractToken(httpContext, out IResult? validationResult);

                    if (validationResult != null)
                    {
                        return validationResult;
                    }

                    var formData = new Dictionary<string, string>
                                        {
                                            { "grant_type", "refresh_token" },
                                            { "client_id", configuration["client_id"] },
                                            { "client_secret", configuration["client_secret"] },
                                            { "refresh_token", tokenString }
                                        };

                    var content = new FormUrlEncodedContent(formData);

                    using var responseRefresh = await httpClient.PostAsync("token", content, cancellationToken);

                    // Check if the request was successful
                    if (responseRefresh.IsSuccessStatusCode)
                    {
                        // revoke old key
                        var requestBody = new
                        {
                            client_Id = configuration["client_id"],
                            token = tokenString
                        };

                        var requestBodyJson = JsonSerializer.Serialize(requestBody);

                        var httpContentRevoking = new StringContent(requestBodyJson, Encoding.UTF8, "application/json");

                        using var responseRevoking = await httpClient.PostAsync("revoke", httpContentRevoking, cancellationToken);
                        // end revoking, no return response

                        var responseRefreshData = await responseRefresh.Content.ReadAsStringAsync(cancellationToken);

                        return Results.Ok(responseRefreshData);
                    }
                    else
                    {
                        string responseData = await responseRefresh.Content.ReadAsStringAsync(cancellationToken);
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