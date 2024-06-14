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

namespace MinimalApiArchitecture.Application.Features.Authentication.Queries;

public class BuyActionNumber : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        app.MapGet("api/buystableproduct/{country}/{op}/{product}", (IMediator mediator, string country, string op, string product) =>
        {
            return mediator.Send(new BuyActionNumberQuery { Country = country, Op = op, Product = product });
        })
        .WithName(nameof(BuyActionNumber));
    }

    public class BuyActionNumberQuery : IRequest<IResult>
    {
        public string? Country { get; set; }
        public string? Op { get; set; }
        public string? Product { get; set; }
    }

    public class BuyActionNumberHandler(IHttpContextAccessor httpContextAccessor, ILogger<BuyActionNumberHandler> logger, IHttpClientFactory httpClientFactory, IConfiguration configuration)
        : IRequestHandler<BuyActionNumberQuery, IResult>
    {
        public async Task<IResult> Handle(BuyActionNumberQuery request, CancellationToken cancellationToken)
        {
            var httpClient = httpClientFactory.CreateClient("SimApiClient");

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

                    var url = string.Format("buyactivation/{0}/{1}/{2}", request.Country, request.Op, request.Product);

                    httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenString);

                    using var response = await httpClient.GetAsync(url, cancellationToken);

                    var responseData = await response.Content.ReadFromJsonAsync<object>(cancellationToken);

                    return Results.Ok(responseData);
                }
                catch (Exception ex)
                {
                    logger.LogWarning("BuyActionNumberHandler: {Message}", ex.InnerException);
                    return Results.Problem(ex.InnerException!.Message, "", (int)HttpStatusCode.InternalServerError);
                }
            }

        }

    }
}