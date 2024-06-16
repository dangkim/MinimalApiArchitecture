using Carter;
using MediatR;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.SqlServer.Server;
using MinimalApiArchitecture.Application.Helpers;
using MinimalApiArchitecture.Application.Model;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MinimalApiArchitecture.Application.Features.Users.Queries;

public class GetBanks : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/getallbanks", (IMediator mediator) =>
        {
            return mediator.Send(new GetBanksQuery { });
        })
        .WithName(nameof(GetBanks));
    }

    public class GetBanksQuery : IRequest<IResult>
    {
    }

    public class GetBanksHandler(IHttpContextAccessor httpContextAccessor, ILogger<GetBanksHandler> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        : IRequestHandler<GetBanksQuery, IResult>
    {
        public async Task<IResult> Handle(GetBanksQuery request, CancellationToken cancellationToken)
        {
            try
            {
                var httpContext = httpContextAccessor.HttpContext!;

                var tokenString = ValidateTokenHelper.ValidateAndExtractToken(httpContext, out IResult? validationResult);

                if (validationResult != null)
                {
                    return validationResult;
                }

                var httpClient = httpClientFactory.CreateClient("SimGraphClient");

                using (httpClient)
                {
                    var payload = new
                    {
                        query = @"
                                query MyQuery {
                                       bandQRCode(status: PUBLISHED) {
                                        qRLink
                                        displayText
                                      }
                                    }"
                    };

                    using StringContent jsonContent = new(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");

                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenString);

                    using var response = await httpClient.PostAsync("", jsonContent, cancellationToken);

                    var responseData = await response.Content.ReadFromJsonAsync<Root>(cancellationToken);

                    return Results.Ok(responseData!.data.bandQRCode);
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning("GetBanks: {Message}", ex.InnerException);
                return Results.Problem(ex.InnerException!.Message, "", (int)HttpStatusCode.InternalServerError);
            }

        }

    }

    public class Link
    {
        public string? qRLink { get; set; }
        public string? displayText { get; set; }
    }

    public class Data
    {
        public List<Link> bandQRCode { get; set; }
    }

    public class Root
    {
        public Data data { get; set; }
    }
}